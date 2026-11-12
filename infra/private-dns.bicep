// Private DNS for the Container Apps environment. A separate module because the zone is named after the
// environment's default domain, a value Azure only knows once the environment exists; a resource name must be known
// when the deployment starts, while a module parameter may be filled in later.

@description('Default domain of the Container Apps environment, e.g. happyhill-1a2b3c4d.germanywestcentral.azurecontainerapps.io')
param zoneName string

@description('Private IP address of the environment (its internal load balancer).')
param staticIp string

@description('Virtual network that resolves the zone: the one the API is integrated with.')
param vnetId string

resource zone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: zoneName
  location: 'global'
}

// Every app of the environment is <app>.<default domain>, so one wildcard record covers them all.
resource wildcard 'Microsoft.Network/privateDnsZones/A@2020-06-01' = {
  parent: zone
  name: '*'
  properties: {
    ttl: 3600
    aRecords: [
      { ipv4Address: staticIp }
    ]
  }
}

resource root 'Microsoft.Network/privateDnsZones/A@2020-06-01' = {
  parent: zone
  name: '@'
  properties: {
    ttl: 3600
    aRecords: [
      { ipv4Address: staticIp }
    ]
  }
}

resource link 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: zone
  name: 'link-to-math-vnet'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: { id: vnetId }
  }
}
