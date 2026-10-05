// FluentFunctions infrastructure: a .NET isolated Function app on Flex Consumption, built from
// Azure Verified Modules. No connection strings or keys: the app reaches every dependency with a
// user-assigned managed identity, and local/shared-key auth is switched off wherever Azure allows.
//
// Keep in step with infra/terraform: see docs/iac-parity.md.

targetScope = 'resourceGroup'

@description('Short application name used in resource names. Lowercase letters and digits.')
@minLength(2)
@maxLength(12)
param appName string = 'fnappname'

@description('Deployment environment.')
@allowed(['dev', 'test', 'prod'])
param environmentName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Maximum instances Flex Consumption may scale out to.')
@minValue(1)
@maxValue(1000)
param maximumInstanceCount int = 100

@description('Memory per instance in MB.')
@allowed([512, 2048, 4096])
param instanceMemoryMB int = 2048

@description('Log Analytics retention in days.')
param logRetentionDays int = 30

@description('Tags applied to every resource.')
param tags object = {}

var resourceSuffix = '${appName}-${environmentName}'
// Storage and Key Vault names are global and short; a hash of the resource group keeps them unique.
var uniqueSuffix = take(uniqueString(resourceGroup().id), 6)
var allTags = union(tags, { application: appName, environment: environmentName, 'managed-by': 'bicep' })
var deploymentContainerName = 'app-package'

// Built-in role definition ids.
var roles = {
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  storageQueueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  monitoringMetricsPublisher: '3913510d-42f4-4e42-8a64-420c390055eb'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  serviceBusDataReceiver: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
  serviceBusDataSender: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
}

module identity 'br/public:avm/res/managed-identity/user-assigned-identity:0.6.0' = {
  params: {
    name: 'id-${resourceSuffix}'
    location: location
    tags: allTags
  }
}

module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.16.1' = {
  params: {
    name: 'log-${resourceSuffix}'
    location: location
    tags: allTags
    dataRetention: logRetentionDays
  }
}

module appInsights 'br/public:avm/res/insights/component:0.8.0' = {
  params: {
    name: 'appi-${resourceSuffix}'
    location: location
    tags: allTags
    workspaceResourceId: logAnalytics.outputs.resourceId
    disableLocalAuth: true
    roleAssignments: [
      {
        principalId: identity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: roles.monitoringMetricsPublisher
      }
    ]
  }
}

module storage 'br/public:avm/res/storage/storage-account:0.33.1' = {
  params: {
    name: take('st${appName}${environmentName}${uniqueSuffix}', 24)
    location: location
    tags: allTags
    skuName: 'Standard_LRS'
    kind: 'StorageV2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    blobServices: {
      containers: [
        { name: deploymentContainerName }
      ]
    }
    roleAssignments: [
      for role in [roles.storageBlobDataOwner, roles.storageQueueDataContributor, roles.storageTableDataContributor]: {
        principalId: identity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: role
      }
    ]
  }
}

module keyVault 'br/public:avm/res/key-vault/vault:0.14.2' = {
  params: {
    name: take('kv-${appName}-${environmentName}-${uniqueSuffix}', 24)
    location: location
    tags: allTags
    enableRbacAuthorization: true
    enablePurgeProtection: environmentName == 'prod'
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
    roleAssignments: [
      {
        principalId: identity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: roles.keyVaultSecretsUser
      }
    ]
  }
}
//#if (UseServiceBus)

module serviceBus 'br/public:avm/res/service-bus/namespace:0.17.1' = {
  params: {
    name: 'sbns-${resourceSuffix}-${uniqueSuffix}'
    location: location
    tags: allTags
    skuObject: {
      name: 'Standard'
    }
    disableLocalAuth: true
    queues: [
      {
        name: 'orders'
        maxDeliveryCount: 10
        lockDuration: 'PT1M'
        deadLetteringOnMessageExpiration: true
      }
    ]
    roleAssignments: [
      for role in [roles.serviceBusDataReceiver, roles.serviceBusDataSender]: {
        principalId: identity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: role
      }
    ]
  }
}
//#endif

module plan 'br/public:avm/res/web/serverfarm:0.7.0' = {
  params: {
    name: 'asp-${resourceSuffix}'
    location: location
    tags: allTags
    skuName: 'FC1'
    reserved: true
    zoneRedundant: false
  }
}

module functionApp 'br/public:avm/res/web/site:0.24.0' = {
  params: {
    name: 'func-${resourceSuffix}-${uniqueSuffix}'
    location: location
    tags: union(allTags, { 'azd-service-name': 'functions' })
    kind: 'functionapp,linux'
    serverFarmResourceId: plan.outputs.resourceId
    httpsOnly: true
    managedIdentities: {
      userAssignedResourceIds: [identity.outputs.resourceId]
    }
    keyVaultAccessIdentityResourceId: identity.outputs.resourceId
    basicPublishingCredentialsPolicies: [
      { name: 'ftp', allow: false }
      { name: 'scm', allow: false }
    ]
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.outputs.primaryBlobEndpoint}${deploymentContainerName}'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: identity.outputs.resourceId
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
    }
    configs: [
      {
        name: 'appsettings'
        properties: {
          // Identity-based host storage: no AzureWebJobsStorage connection string.
          AzureWebJobsStorage__accountName: storage.outputs.name
          AzureWebJobsStorage__credential: 'managedidentity'
          AzureWebJobsStorage__clientId: identity.outputs.clientId
          // Used by the app's own Azure SDK clients and the OpenTelemetry exporter.
          AZURE_CLIENT_ID: identity.outputs.clientId
          APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.outputs.connectionString
          APPLICATIONINSIGHTS_AUTHENTICATION_STRING: 'ClientId=${identity.outputs.clientId};Authorization=AAD'
          KeyVault__Uri: keyVault.outputs.uri
//#if (UseServiceBus)
          ServiceBusConnection__fullyQualifiedNamespace: '${serviceBus.outputs.name}.servicebus.windows.net'
          ServiceBusConnection__credential: 'managedidentity'
          ServiceBusConnection__clientId: identity.outputs.clientId
          ServiceBus__QueueName: 'orders'
//#endif
//#if (UseOrders)
          Orders__MaxLines: '50'
          Orders__MaxQuantityPerLine: '100'
          Orders__MaxAge: '1.00:00:00'
//#endif
//#if (UseTimer)
          Cleanup__Schedule: '0 0 2 * * *'
          Cleanup__RetentionDays: '30'
          Cleanup__BatchSize: '500'
//#endif
        }
      }
    ]
  }
}

output functionAppName string = functionApp.outputs.name
output functionAppHostName string = functionApp.outputs.defaultHostname
output identityClientId string = identity.outputs.clientId
output keyVaultUri string = keyVault.outputs.uri
