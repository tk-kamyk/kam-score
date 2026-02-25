@description('Principal ID of the managed identity to grant access')
param principalId string

@description('Name of the existing Key Vault')
param keyVaultName string

@description('Name of the existing Container Registry')
param acrName string

// Role definition IDs
var keyVaultSecretsUserRole = '4633458b-17de-408a-b874-0445c86b69e6'
var acrPullRole = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

// Reference existing resources in this resource group (kam-square)
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// Grant Key Vault Secrets User role to managed identity
resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, keyVaultSecretsUserRole)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRole)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant AcrPull role to managed identity
resource acrRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, principalId, acrPullRole)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRole)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
