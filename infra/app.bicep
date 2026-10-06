param location string
param containerImage string
param customDomainName string
param wwwCustomDomainName string

@secure()
param databaseConnectionString string

@secure()
param googleClientId string

@secure()
param googleClientSecret string

param authStorageAccountName string
param authCertificateShareName string

@secure()
param authStorageAccountKey string

@secure()
param authSigningPassword string

@secure()
param authEncryptionPassword string

param authClients array

param budgetContactEmail string
param monthlyBudgetAmount int
param deploymentVersion string
param budgetStartDate string = '2026-08-01T00:00:00Z'

var suffix = uniqueString(resourceGroup().id)
var environmentName = 'cae-sprite-scout-${suffix}'
var applicationName = 'ca-sprite-scout-${suffix}'

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: environmentName
  location: location
  properties: {
    // Omitting appLogsConfiguration selects "Don't save logs". Azure rejects
    // the literal string "none" even though the CLI uses that spelling.
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

resource authCertificateStorage 'Microsoft.App/managedEnvironments/storages@2025-01-01' = {
  parent: environment
  name: 'auth-certificates'
  properties: {
    azureFile: {
      accountName: authStorageAccountName
      accountKey: authStorageAccountKey
      shareName: authCertificateShareName
      accessMode: 'ReadOnly'
    }
  }
}

var authClientEnvironment = flatten(map(authClients, (client, index) => concat([
  {
    name: 'SpriteScoutAuth__Clients__${index}__ClientId'
    value: client.ClientId
  }
  {
    name: 'SpriteScoutAuth__Clients__${index}__DisplayName'
    value: client.DisplayName
  }
  {
    name: 'SpriteScoutAuth__Clients__${index}__ApplicationType'
    value: client.ApplicationType
  }
], map(client.RedirectUris, (redirectUri, redirectIndex) => {
  name: 'SpriteScoutAuth__Clients__${index}__RedirectUris__${redirectIndex}'
  value: redirectUri
}))))

// Managed certificates require the custom hostnames to exist before issuance,
// so they are bootstrapped once outside Bicep and renewed automatically by Azure.
resource apexManagedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2025-01-01' existing = {
  parent: environment
  name: 'cert-sprite-scout-apex'
}

resource wwwManagedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2025-01-01' existing = {
  parent: environment
  name: 'cert-sprite-scout-www'
}

resource application 'Microsoft.App/containerApps@2025-01-01' = {
  name: applicationName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        allowInsecure: false
        targetPort: 8080
        transport: 'auto'
        customDomains: [
          {
            name: customDomainName
            bindingType: 'SniEnabled'
            certificateId: apexManagedCertificate.id
          }
          {
            name: wwwCustomDomainName
            bindingType: 'SniEnabled'
            certificateId: wwwManagedCertificate.id
          }
        ]
      }
      maxInactiveRevisions: 1
      secrets: [
        {
          name: 'database-connection'
          value: databaseConnectionString
        }
        {
          name: 'google-client-id'
          value: googleClientId
        }
        {
          name: 'google-client-secret'
          value: googleClientSecret
        }
        {
          name: 'auth-signing-password'
          value: authSigningPassword
        }
        {
          name: 'auth-encryption-password'
          value: authEncryptionPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'server'
          image: containerImage
          volumeMounts: [
            {
              volumeName: 'auth-certificates'
              mountPath: '/auth-certificates'
            }
          ]
          env: concat([
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'DEPLOYMENT_VERSION'
              value: deploymentVersion
            }
            {
              name: 'ConnectionStrings__sprite-tracker'
              secretRef: 'database-connection'
            }
            {
              name: 'Authentication__Google__ClientId'
              secretRef: 'google-client-id'
            }
            {
              name: 'Authentication__Google__ClientSecret'
              secretRef: 'google-client-secret'
            }
            {
              name: 'SpriteScoutAuth__Issuer'
              value: 'https://${customDomainName}/identity/'
            }
            {
              name: 'SpriteScoutAuth__Resource'
              value: 'https://${customDomainName}/mcp'
            }
            {
              name: 'SpriteScoutAuth__Certificates__SigningPath'
              value: '/auth-certificates/signing.pfx'
            }
            {
              name: 'SpriteScoutAuth__Certificates__EncryptionPath'
              value: '/auth-certificates/encryption.pfx'
            }
            {
              name: 'SpriteScoutAuth__Certificates__SigningPassword'
              secretRef: 'auth-signing-password'
            }
            {
              name: 'SpriteScoutAuth__Certificates__EncryptionPassword'
              secretRef: 'auth-encryption-password'
            }
          ], authClientEnvironment)
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/alive'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 15
              periodSeconds: 30
              failureThreshold: 3
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      volumes: [
        {
          name: 'auth-certificates'
          storageType: 'AzureFile'
          storageName: authCertificateStorage.name
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
        rules: [
          {
            name: 'http-requests'
            http: {
              metadata: {
                concurrentRequests: '25'
              }
            }
          }
        ]
      }
    }
  }
}

resource budget 'Microsoft.Consumption/budgets@2024-08-01' = if (!empty(budgetContactEmail)) {
  name: 'sprite-scout-monthly-budget'
  properties: {
    amount: monthlyBudgetAmount
    category: 'Cost'
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: budgetStartDate
      endDate: dateTimeAdd(budgetStartDate, 'P10Y')
    }
    notifications: {
      Actual50Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 50
        thresholdType: 'Actual'
        contactEmails: [budgetContactEmail]
        contactGroups: []
        contactRoles: []
        locale: 'en-us'
      }
      Forecast100Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        thresholdType: 'Forecasted'
        contactEmails: [budgetContactEmail]
        contactGroups: []
        contactRoles: []
        locale: 'en-us'
      }
      Actual100Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        thresholdType: 'Actual'
        contactEmails: [budgetContactEmail]
        contactGroups: []
        contactRoles: []
        locale: 'en-us'
      }
    }
  }
}

output applicationUrl string = 'https://${application.properties.configuration.ingress.fqdn}'
