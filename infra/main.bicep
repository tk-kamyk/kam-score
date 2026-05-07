@description('Location for all resources')
param location string = resourceGroup().location

@description('Prefix for all app-scoped resources (e.g. logs, identity, api, spa)')
param resourcePrefix string

@description('Resource group containing shared resources (Key Vault, Cosmos DB, ACR)')
param sharedResourceGroup string

@description('Name of the existing Cosmos DB account')
param cosmosAccountName string

@description('Name of the existing Key Vault')
param keyVaultName string

@description('Name of the existing Container Registry (unused by hosting; required by shared-rg-access module)')
param acrName string

@description('Custom domain for the Static Web App (e.g. score.example.com)')
param customDomainName string

@description('Linux runtime stack identifier for the API App Service')
param dotnetLinuxFxVersion string = 'DOTNETCORE|10.0'

@description('Unique suffix for deployment names (auto-generated)')
param deploymentSuffix string = utcNow('yyyyMMddHHmmss')

// ──────────────────────────────────────────────
// Existing resources in shared RG
// ──────────────────────────────────────────────

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
  scope: resourceGroup(sharedResourceGroup)
}

var cosmosConnectionString = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
var keyVaultUri = 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets'

// ──────────────────────────────────────────────
// Log Analytics Workspace
// ──────────────────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${resourcePrefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: json('0.1')
    }
  }
}

// ──────────────────────────────────────────────
// User-Assigned Managed Identity
// ──────────────────────────────────────────────

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${resourcePrefix}-identity'
  location: location
}

// ──────────────────────────────────────────────
// Cross-RG role assignments (Key Vault + ACR)
// ──────────────────────────────────────────────

module sharedAccess 'modules/shared-rg-access.bicep' = {
  name: 'shared-rg-access-${deploymentSuffix}'
  scope: resourceGroup(sharedResourceGroup)
  params: {
    principalId: managedIdentity.properties.principalId
    keyVaultName: keyVaultName
    acrName: acrName
  }
}

// ──────────────────────────────────────────────
// Static Web App (SPA) — declared before the API so its hostname
// can be auto-wired into the API CORS allow-list.
// ──────────────────────────────────────────────

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${resourcePrefix}-spa'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    provider: 'None'
  }
}

// Custom domain binding. Requires the CNAME record to already point
// at the SWA's default hostname before validation succeeds.
resource swaCustomDomain 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticWebApp
  name: customDomainName
  properties: {
    validationMethod: 'cname-delegation'
  }
}

// ──────────────────────────────────────────────
// App Service Plan — Linux Free (F1)
// ──────────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${resourcePrefix}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true
  }
}

// ──────────────────────────────────────────────
// API Web App (deploy-as-code; F1 cannot host custom containers)
// ──────────────────────────────────────────────

var baseAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'Jwt__Secret'
    value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}/JWT-SECRET/)'
  }
  {
    name: 'Users'
    value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}/USERS/)'
  }
  {
    name: 'CosmosDb__ConnectionString'
    value: cosmosConnectionString
  }
  {
    name: 'Cors__AllowedOrigins__0'
    value: 'https://${staticWebApp.properties.defaultHostname}'
  }
  {
    name: 'Cors__AllowedOrigins__1'
    value: 'https://${customDomainName}'
  }
  {
    name: 'WEBSITE_RUN_FROM_PACKAGE'
    value: '1'
  }
]

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${resourcePrefix}-api'
  location: location
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: managedIdentity.id
    siteConfig: {
      linuxFxVersion: dotnetLinuxFxVersion
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      appSettings: baseAppSettings
    }
  }
  dependsOn: [
    sharedAccess
  ]
}

// Deployment uses managed identity + run-from-package; SCM/FTP basic auth
// would only widen the attack surface, so disable both explicitly.
resource apiScmBasicAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: apiApp
  name: 'scm'
  properties: {
    allow: false
  }
}

resource apiFtpBasicAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: apiApp
  name: 'ftp'
  properties: {
    allow: false
  }
}

// ──────────────────────────────────────────────
// Keep-warm pinger (Consumption Logic App)
//
// F1 App Service has no Always On, so the API cold-starts after ~20 min of
// inactivity. This Logic App pings /api/health every 15 minutes to keep the
// app warm. 2 built-in executions per run × 2,880 runs/month = 5,760/month;
// the Consumption free tier covers 4,000/month, so the overage is a few
// cents per month. Endpoint is rate-limited (60 req/min/IP) so this caller
// cannot drain the bucket for real users.
// ──────────────────────────────────────────────

resource keepWarm 'Microsoft.Logic/workflows@2019-05-01' = {
  name: '${resourcePrefix}-keepwarm'
  location: location
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      triggers: {
        Recurrence: {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Minute'
            interval: 15
          }
        }
      }
      actions: {
        Ping_API_Health: {
          type: 'Http'
          inputs: {
            method: 'GET'
            uri: 'https://${apiApp.properties.defaultHostName}/api/health/'
          }
        }
      }
    }
  }
}

// ──────────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────────

output apiUrl string = 'https://${apiApp.properties.defaultHostName}'
output spaUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output logAnalyticsWorkspaceId string = logAnalytics.id
output keepWarmWorkflowId string = keepWarm.id
