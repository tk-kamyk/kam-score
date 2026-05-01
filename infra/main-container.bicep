@description('Location for all resources')
param location string = resourceGroup().location

@description('Full API container image path with tag')
param apiImage string

@description('Full SPA container image path with tag')
param spaImage string

@description('Resource group containing shared resources (Key Vault, Cosmos DB, ACR)')
param sharedResourceGroup string = 'REDACTED-SHARED-RG'

@description('Name of the existing Cosmos DB account')
param cosmosAccountName string = 'REDACTED-COSMOS'

@description('Name of the existing Key Vault')
param keyVaultName string = 'REDACTED-KV'

@description('Name of the existing Container Registry')
param acrName string = 'REDACTED-ACR'

// ──────────────────────────────────────────────
// Existing resources in REDACTED-SHARED-RG RG
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
  name: 'kam-score-logs'
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: 'kam-score-identity'
}

// ──────────────────────────────────────────────
// Container App Environment
// ──────────────────────────────────────────────

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'kam-score-env'
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

resource apiApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: 'REDACTED-API'
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
      secrets: [
        {
          name: 'jwt-secret'
          keyVaultUrl: '${keyVaultUri}/JWT-SECRET'
          identity: managedIdentity.id
        }
        {
          name: 'admin-username'
          keyVaultUrl: '${keyVaultUri}/ADMIN-USERNAME'
          identity: managedIdentity.id
        }
        {
          name: 'admin-password'
          keyVaultUrl: '${keyVaultUri}/ADMIN-PASSWORD'
          identity: managedIdentity.id
        }
        {
          name: 'admin-displayname'
          keyVaultUrl: '${keyVaultUri}/ADMIN-DISPLAYNAME'
          identity: managedIdentity.id
        }
        {
          name: 'admin-role'
          keyVaultUrl: '${keyVaultUri}/ADMIN-ROLE'
          identity: managedIdentity.id
        }
        {
          name: 'dtu-username'
          keyVaultUrl: '${keyVaultUri}/DTU-USERNAME'
          identity: managedIdentity.id
        }
        {
          name: 'dtu-password'
          keyVaultUrl: '${keyVaultUri}/DTU-PASSWORD'
          identity: managedIdentity.id
        }
        {
          name: 'dtu-displayname'
          keyVaultUrl: '${keyVaultUri}/DTU-DISPLAYNAME'
          identity: managedIdentity.id
        }
        {
          name: 'dtu-role'
          keyVaultUrl: '${keyVaultUri}/DTU-ROLE'
          identity: managedIdentity.id
        }
        {
          name: 'ksv-username'
          keyVaultUrl: '${keyVaultUri}/KSV-USERNAME'
          identity: managedIdentity.id
        }
        {
          name: 'ksv-password'
          keyVaultUrl: '${keyVaultUri}/KSV-PASSWORD'
          identity: managedIdentity.id
        }
        {
          name: 'ksv-displayname'
          keyVaultUrl: '${keyVaultUri}/KSV-DISPLAYNAME'
          identity: managedIdentity.id
        }
        {
          name: 'ksv-role'
          keyVaultUrl: '${keyVaultUri}/KSV-ROLE'
          identity: managedIdentity.id
        }
        {
          name: 'cph-username'
          keyVaultUrl: '${keyVaultUri}/CPH-USERNAME'
          identity: managedIdentity.id
        }
        {
          name: 'cph-password'
          keyVaultUrl: '${keyVaultUri}/CPH-PASSWORD'
          identity: managedIdentity.id
        }
        {
          name: 'cph-displayname'
          keyVaultUrl: '${keyVaultUri}/CPH-DISPLAYNAME'
          identity: managedIdentity.id
        }
        {
          name: 'cph-role'
          keyVaultUrl: '${keyVaultUri}/CPH-ROLE'
          identity: managedIdentity.id
        }
        {
          name: 'cosmos-connection-string'
          value: cosmosConnectionString
        }
      ]
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
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Users__Entries__0__Username'
              secretRef: 'admin-username'
            }
            {
              name: 'Users__Entries__0__Password'
              secretRef: 'admin-password'
            }
            {
              name: 'Users__Entries__0__DisplayName'
              secretRef: 'admin-displayname'
            }
            {
              name: 'Users__Entries__0__Role'
              secretRef: 'admin-role'
            }
            {
              name: 'Users__Entries__1__Username'
              secretRef: 'dtu-username'
            }
            {
              name: 'Users__Entries__1__Password'
              secretRef: 'dtu-password'
            }
            {
              name: 'Users__Entries__1__DisplayName'
              secretRef: 'dtu-displayname'
            }
            {
              name: 'Users__Entries__1__Role'
              secretRef: 'dtu-role'
            }
            {
              name: 'Users__Entries__2__Username'
              secretRef: 'ksv-username'
            }
            {
              name: 'Users__Entries__2__Password'
              secretRef: 'ksv-password'
            }
            {
              name: 'Users__Entries__2__DisplayName'
              secretRef: 'ksv-displayname'
            }
            {
              name: 'Users__Entries__2__Role'
              secretRef: 'ksv-role'
            }
            {
              name: 'Users__Entries__3__Username'
              secretRef: 'cph-username'
            }
            {
              name: 'Users__Entries__3__Password'
              secretRef: 'cph-password'
            }
            {
              name: 'Users__Entries__3__DisplayName'
              secretRef: 'cph-displayname'
            }
            {
              name: 'Users__Entries__3__Role'
              secretRef: 'cph-role'
            }
            {
              name: 'CosmosDb__ConnectionString'
              secretRef: 'cosmos-connection-string'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: 'https://score.REDACTED-SHARED-RG.com'
            }
            {
              name: 'Cors__AllowedOrigins__1'
              value: 'https://REDACTED-SPA.${containerAppEnv.properties.defaultDomain}'
            }
          ]
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
  name: 'REDACTED-SPA'
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
