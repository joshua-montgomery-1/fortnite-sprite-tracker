targetScope = 'subscription'

@description('Azure region near the Supabase project.')
param location string = 'eastus2'

@description('Resource group dedicated to Sprite Scout so its budget is isolated.')
param resourceGroupName string = 'rg-sprite-scout-prod'

@description('Public OCI image, such as ghcr.io/owner/fortnite-sprite-tracker:sha.')
param containerImage string

@description('Apex hostname bound to the production Container App. Its A and asuid TXT records must exist before deployment.')
param customDomainName string = 'spritescout.com'

@description('WWW hostname bound to the production Container App. Its CNAME and asuid.www TXT records must exist before deployment.')
param wwwCustomDomainName string = 'www.spritescout.com'

@secure()
param databaseConnectionString string

@secure()
param googleClientId string

@secure()
param googleClientSecret string

@description('Optional address for Azure budget alerts. Leave empty to omit the budget resource.')
param budgetContactEmail string = ''

@minValue(1)
@maxValue(20)
param monthlyBudgetAmount int = 5

@description('Changes on every deployment so Container Apps creates a revision and re-pulls mutable image tags.')
param deploymentVersion string = utcNow('yyyyMMddHHmmss')

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    application: 'sprite-scout'
    environment: 'production'
    costProfile: 'free-first'
  }
}

module application 'app.bicep' = {
  name: 'sprite-scout-application'
  scope: resourceGroup
  params: {
    location: location
    containerImage: containerImage
    customDomainName: customDomainName
    wwwCustomDomainName: wwwCustomDomainName
    databaseConnectionString: databaseConnectionString
    googleClientId: googleClientId
    googleClientSecret: googleClientSecret
    budgetContactEmail: budgetContactEmail
    monthlyBudgetAmount: monthlyBudgetAmount
    deploymentVersion: deploymentVersion
  }
}

output applicationUrl string = application.outputs.applicationUrl
