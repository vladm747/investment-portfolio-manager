// Hosting for the Python math service on Azure Container Apps, reachable only from the API.
//
//   API (App Service, regional VNet integration)
//     -> snet-api -- virtual network -- snet-containerapps
//          -> internal Container Apps environment (no public endpoint) -> math service
//
// The environment is "internal": it has no public IP and its default domain exists only in the private DNS zone
// below, which is linked to this one virtual network. The only thing attached to the network is the API's subnet,
// so the API is the only caller that can reach the service at all; the API key (MATH_API_KEY) is the second lock.
//
// This file creates new resources only. The API site (portfoliomanagerapi) already exists and is not managed here:
// attaching it to the subnet and setting MathModule__BaseUrl / MathModule__ApiKey is done with `az` (infra/README.md),
// because deploying a site resource from a template would replace its app settings.
//
// Deploy in two steps (the container app cannot be created before its image exists):
//   1. without containerImage: registry, network, environment, DNS  2. with containerImage: the app itself.

targetScope = 'resourceGroup'

@description('Region. Must be the region of the API App Service plan: regional VNet integration needs the same region.')
param location string = resourceGroup().location

@description('Prefix of the names of the new resources.')
param prefix string = 'portfoliomanager'

@description('Container registry name: 5-50 lowercase letters and digits, unique across Azure.')
param registryName string = 'pmmath${uniqueString(resourceGroup().id)}'

@description('Address space of the new virtual network. It must not overlap a network you peer with it later.')
param vnetAddressPrefix string = '10.40.0.0/24'

@description('Subnet the API is integrated with (App Service recommends /26; /28 is the minimum).')
param apiSubnetPrefix string = '10.40.0.0/26'

@description('Subnet of the Container Apps environment (/27 is the minimum for a workload-profiles environment).')
param containerAppsSubnetPrefix string = '10.40.0.64/26'

@description('Image to run, <registry>.azurecr.io/portfolio-math-module:<tag>. Empty on the first deployment: the registry has no image yet, so the app is not created.')
param containerImage string = ''

@description('MATH_API_KEY: the shared key the API sends in X-API-Key. At least 32 characters (the service refuses to start otherwise). Must equal MathModule__ApiKey of the API.')
@secure()
param mathApiKey string

@description('Annual risk-free rate as a fraction (0.04 = 4 %), used by the optimiser and by Sharpe and Sortino.')
param riskFreeRate string = '0'

@description('1 keeps the service warm. 0 saves money but the first request after idle waits for a cold start (image pull, pandas and scipy import).')
@minValue(0)
@maxValue(1)
param minReplicas int = 1

var vnetName = '${prefix}-vnet'
var apiSubnetName = 'snet-api'
var containerAppsSubnetName = 'snet-containerapps'
var appName = '${prefix}-math'
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var servicePort = 8011

// ---- network ----------------------------------------------------------------------------------------------------

resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [vnetAddressPrefix]
    }
    subnets: [
      {
        name: apiSubnetName
        properties: {
          addressPrefix: apiSubnetPrefix
          delegations: [
            {
              name: 'app-service'
              properties: { serviceName: 'Microsoft.Web/serverFarms' }
            }
          ]
        }
      }
      {
        name: containerAppsSubnetName
        properties: {
          addressPrefix: containerAppsSubnetPrefix
          delegations: [
            {
              name: 'container-apps'
              properties: { serviceName: 'Microsoft.App/environments' }
            }
          ]
        }
      }
    ]
  }
}

resource containerAppsSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' existing = {
  parent: vnet
  name: containerAppsSubnetName
}

// ---- logs, registry, identity -----------------------------------------------------------------------------------

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: json('0.5') } // a runaway log cannot run up the bill
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false // pulls and pushes use Azure identities, there is no password to leak
  }
}

// The app pulls its image with this identity, so no registry password exists anywhere.
resource pullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-math-pull'
  location: location
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: registry
  name: guid(registry.id, pullIdentity.id, acrPullRoleId)
  properties: {
    principalId: pullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
  }
}

// ---- environment ------------------------------------------------------------------------------------------------

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${prefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: containerAppsSubnet.id
      internal: true // no public IP: only the virtual network can reach the apps in it
    }
    workloadProfiles: [
      { name: 'Consumption', workloadProfileType: 'Consumption' }
    ]
    zoneRedundant: false
  }
}

module dns 'private-dns.bicep' = {
  name: 'math-private-dns'
  params: {
    zoneName: environment.properties.defaultDomain
    staticIp: environment.properties.staticIp
    vnetId: vnet.id
  }
}

// ---- the service ------------------------------------------------------------------------------------------------

resource mathService 'Microsoft.App/containerApps@2024-03-01' = if (!empty(containerImage)) {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${pullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single' // a bad image never replaces a working revision: the old one serves until the new one is healthy
      ingress: {
        // In an internal environment "external: true" means "reachable from the virtual network" (the portal calls it
        // "Limited to VNet"). "external: false" would mean "reachable only from other apps of this environment",
        // which excludes the API. Nothing is reachable from the internet either way.
        external: true
        targetPort: servicePort
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: pullIdentity.id
        }
      ]
      secrets: [
        { name: 'math-api-key', value: mathApiKey }
      ]
    }
    template: {
      containers: [
        {
          name: 'math'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'MATH_API_KEY', secretRef: 'math-api-key' }
            // Fail closed: the service refuses to start without a key of 32+ characters.
            { name: 'REQUIRE_API_KEY', value: 'true' }
            // No /docs or /openapi.json on a hosted service.
            { name: 'ENABLE_DOCS', value: 'false' }
            { name: 'RISK_FREE_RATE', value: riskFreeRate }
          ]
          // Probes call /health directly, which needs no key and never calls Yahoo Finance.
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/health', port: servicePort }
              periodSeconds: 5
              failureThreshold: 24
            }
            {
              type: 'Liveness'
              httpGet: { path: '/health', port: servicePort }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health', port: servicePort }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        // One replica on purpose: the price caches live in the process, and a second replica would only
        // double the requests to Yahoo Finance, which rate-limits.
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    acrPull
    dns
  ]
}

// ---- what the next steps need -----------------------------------------------------------------------------------

output registryName string = registry.name
output registryLoginServer string = registry.properties.loginServer
output vnetName string = vnet.name
output apiSubnetName string = apiSubnetName
output containerAppName string = appName
output environmentDefaultDomain string = environment.properties.defaultDomain
output environmentStaticIp string = environment.properties.staticIp

@description('Value for MathModule__BaseUrl of the API. Empty until the app exists.')
output mathModuleBaseUrl string = empty(containerImage) ? '' : 'https://${mathService!.properties.configuration.ingress.fqdn}'
