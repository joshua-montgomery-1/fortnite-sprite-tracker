param location string
param containerImage string
param customDomainName string
param wwwCustomDomainName string
param bindCustomDomainCertificates bool

@secure()
param databaseConnectionString string

@secure()
param googleClientId string

@secure()
param googleClientSecret string

param budgetContactEmail string
param monthlyBudgetAmount int
param deploymentVersion string
param budgetStartDate string = utcNow('yyyy-MM-01T00:00:00Z')

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

resource apexManagedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2025-01-01' = {
  parent: environment
  name: 'cert-sprite-scout-apex'
  location: location
  properties: {
    subjectName: customDomainName
    domainControlValidation: 'HTTP'
  }
  dependsOn: [
    application
  ]
}

resource wwwManagedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2025-01-01' = {
  parent: environment
  name: 'cert-sprite-scout-www'
  location: location
  properties: {
    subjectName: wwwCustomDomainName
    domainControlValidation: 'CNAME'
  }
  dependsOn: [
    application
  ]
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
        customDomains: bindCustomDomainCertificates ? [
          {
            name: customDomainName
            bindingType: 'SniEnabled'
            certificateId: resourceId('Microsoft.App/managedEnvironments/managedCertificates', environmentName, 'cert-sprite-scout-apex')
          }
          {
            name: wwwCustomDomainName
            bindingType: 'SniEnabled'
            certificateId: resourceId('Microsoft.App/managedEnvironments/managedCertificates', environmentName, 'cert-sprite-scout-www')
          }
        ] : [
          {
            name: customDomainName
            bindingType: 'Disabled'
          }
          {
            name: wwwCustomDomainName
            bindingType: 'Disabled'
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
      ]
    }
    template: {
      containers: [
        {
          name: 'server'
          image: containerImage
          env: [
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
          ]
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
            cpu: json('0.5')
            memory: '1Gi'
          }
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
