@description('Location for all resources')
param location string = resourceGroup().location

@description('Resource group containing shared resources (Key Vault, Cosmos DB, ACR)')
param sharedResourceGroup string = 'REDACTED-SHARED-RG'

@description('Name of the existing Cosmos DB account')
param cosmosAccountName string = 'REDACTED-COSMOS'

@description('Name of the existing Key Vault')
param keyVaultName string = 'REDACTED-KV'

@description('Name of the existing Container Registry (unused by hosting; required by shared-rg-access module)')
param acrName string = 'REDACTED-ACR'

@description('Linux runtime stack identifier for the API App Service')
param dotnetLinuxFxVersion string = 'DOTNETCORE|10.0'

@description('Unique suffix for deployment names (auto-generated)')
param deploymentSuffix string = utcNow('yyyyMMddHHmmss')

// ──────────────────────────────────────────────
// Existing resources in REDACTED-SHARED-RG RG
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
  name: 'kam-score-logs-free'
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
  name: 'kam-score-identity-free'
  location: location
}

// ──────────────────────────────────────────────
// Cross-RG role assignments (Key Vault + ACR)
// ──────────────────────────────────────────────

module sharedAccess 'modules/shared-rg-access.bicep' = {
  name: 'shared-rg-access-free-${deploymentSuffix}'
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
  name: 'REDACTED-SPA-free'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    provider: 'None'
  }
}

// ──────────────────────────────────────────────
// App Service Plan — Linux Free (F1)
// ──────────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'kam-score-plan-free'
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

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'REDACTED-API-free'
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
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'Jwt__Secret'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}/JWT-SECRET/)'
        }
        {
          name: 'Users__Entries__0__Username'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}/ADMIN-USERNAME/)'
        }
        {
          name: 'Users__Entries__0__Password'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}/ADMIN-PASSWORD/)'
        }
        {
          name: 'Users__Entries__0__DisplayName'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}/ADMIN-DISPLAYNAME/)'
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
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
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
// Outputs
// ──────────────────────────────────────────────

output apiUrl string = 'https://${apiApp.properties.defaultHostName}'
output spaUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output logAnalyticsWorkspaceId string = logAnalytics.id
