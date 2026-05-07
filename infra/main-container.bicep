@description('Location for all resources')
param location string = resourceGroup().location

@description('Full API container image path with tag')
param apiImage string

@description('Full SPA container image path with tag')
param spaImage string

@description('Prefix for all app-scoped resources')
param resourcePrefix string

@description('Resource group containing shared resources (Key Vault, Cosmos DB, ACR)')
param sharedResourceGroup string

@description('Name of the existing Cosmos DB account')
param cosmosAccountName string

@description('Name of the existing Key Vault')
param keyVaultName string

@description('Name of the existing Container Registry')
param acrName string

@description('Custom domain used in CORS allow-list')
param customDomainName string

// ──────────────────────────────────────────────
// Existing resources in shared RG
// ──────────────────────────────────────────────

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
  scope: resourceGroup(sharedResourceGroup)
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
  scope: resourceGroup(sharedResourceGroup)
}

var cosmosConnectionString = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
var keyVaultUri = 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets'
var acrLoginServer = acr.properties.loginServer

// ──────────────────────────────────────────────
// Shared platform resources (owned by main.bicep)
// Log Analytics workspace and managed identity are deployed once by the
// canonical stack; this stack just references them. Cross-RG RBAC is granted
// once over there too — no module call needed here.
// ──────────────────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: '${resourcePrefix}-logs'
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${resourcePrefix}-identity'
}

// ──────────────────────────────────────────────
// Container App Environment
// ──────────────────────────────────────────────

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${resourcePrefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ──────────────────────────────────────────────
// API Container App (defined before SPA so FQDN is available)
// ──────────────────────────────────────────────

var baseSecrets = [
  {
    name: 'jwt-secret'
    keyVaultUrl: '${keyVaultUri}/JWT-SECRET'
    identity: managedIdentity.id
  }
  {
    name: 'users'
    keyVaultUrl: '${keyVaultUri}/USERS'
    identity: managedIdentity.id
  }
  {
    name: 'cosmos-connection-string'
    value: cosmosConnectionString
  }
]

var baseEnvVars = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'Jwt__Secret'
    secretRef: 'jwt-secret'
  }
  {
    name: 'Users'
    secretRef: 'users'
  }
  {
    name: 'CosmosDb__ConnectionString'
    secretRef: 'cosmos-connection-string'
  }
  {
    name: 'Cors__AllowedOrigins__0'
    value: 'https://${customDomainName}'
  }
  {
    name: 'Cors__AllowedOrigins__1'
    value: 'https://${resourcePrefix}-spa.${containerAppEnv.properties.defaultDomain}'
  }
]

resource apiApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: '${resourcePrefix}-api'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
      }
      registries: [
        {
          server: acrLoginServer
          identity: managedIdentity.id
        }
      ]
      secrets: baseSecrets
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: baseEnvVars
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        cooldownPeriod: 300
        rules: [
          {
            name: 'http-scale'
            http: {
              metadata: {
                concurrentRequests: '30'
              }
            }
          }
        ]
      }
    }
  }
}

// ──────────────────────────────────────────────
// SPA Container App
// ──────────────────────────────────────────────

resource spaApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: '${resourcePrefix}-spa'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      registries: [
        {
          server: acrLoginServer
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'spa'
          image: spaImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'API_BACKEND_URL'
              value: 'https://${apiApp.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
        cooldownPeriod: 300
        rules: [
          {
            name: 'http-scale'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

// ──────────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────────

output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output spaUrl string = 'https://${spaApp.properties.configuration.ingress.fqdn}'
