// https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/
// https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/bicep-functions
@allowed([
  'development'
  'production'
])
param environment string = 'development'
param prefix string = 'nfderpidl'
param location string = resourceGroup().location

var appUniqueString = uniqueString(resourceGroup().name)
// Storage account names can't have hypens
var storagePrefix = replace(prefix, '-', '')

var functionAppName = '${prefix}func${appUniqueString}'
var functionPlanName = '${prefix}funcplan'
var functionStorageName = take('${storagePrefix}storage${appUniqueString}', 24)
var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${funcappstorage.name};AccountKey=${listKeys(funcappstorage.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
var tags = {
  'AppName': 'derpidl-functions'
  'Environment': environment
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: functionPlanName
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

// Storage account for functions app
resource funcappstorage 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: functionStorageName
  kind: 'StorageV2'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource funcappsettings 'Microsoft.Web/sites/config@2021-03-01' = {
  name: 'appsettings'
  parent: funcapp
  properties: {
    AzureWebJobsStorage: storageAccountConnectionString
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageAccountConnectionString
    WEBSITE_CONTENTSHARE: '${toLower(functionAppName)}876f'
  }
}

resource funcapp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  kind: 'functionapp'
  location: location
  tags: tags
  properties: {
    httpsOnly: true
    serverFarmId: hostingPlan.id
  }
}

output functionAppName string = funcapp.name
output functionAppUrl string = 'https://${funcapp.properties.defaultHostName}'
