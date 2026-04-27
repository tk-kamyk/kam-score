@description('Location for all resources')
param location string = resourceGroup().location

@description('Full API container image path with tag')
param apiImage string

@description('Full SPA container image path with tag')
param spaImage string

@description('Resource group containing shared resources (Key Vault, Cosmos DB, ACR)')
param sharedResourceGroup string = 'kam-square'

@description('Name of the existing Cosmos DB account')
param cosmosAccountName string = 'kam-square-cosmos'

@description('Name of the existing Key Vault')
param keyVaultName string = 'kam-square-kv'

@description('Name of the existing Container Registry')
param acrName string = 'kamsquareacr'

@description('Unique suffix for deployment names (auto-generated)')
param deploymentSuffix string = utcNow('yyyyMMddHHmmss')

// ──────────────────────────────────────────────
// Existing resources in kam-square RG
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
// Log Analytics Workspace
// ──────────────────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'kam-score-logs'
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
  name: 'kam-score-identity'
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
  name: 'kam-score-api'
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
              value: 'https://score.kam-square.com'
            }
            {
              name: 'Cors__AllowedOrigins__1'
              value: 'https://kam-score-spa.${containerAppEnv.properties.defaultDomain}'
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
  dependsOn: [
    sharedAccess
  ]
}

// ──────────────────────────────────────────────
// SPA Container App
// ──────────────────────────────────────────────

resource spaApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: 'kam-score-spa'
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
        customDomains: [
          {
            name: 'score.kam-square.com'
            bindingType: 'Auto'
          }
        ]
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
  dependsOn: [
    sharedAccess
  ]
}

// ──────────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────────

output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output spaUrl string = 'https://${spaApp.properties.configuration.ingress.fqdn}'
