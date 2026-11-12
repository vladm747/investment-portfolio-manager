# Hosting the math service on Azure

The Python service (`PortfolioManager/PortfolioManager.MathModule`) runs as an **Azure Container App** in a private
network. The API (App Service `portfoliomanagerapi`) reaches it through regional VNet integration; nothing else can.

```
browser ──► portfoliomanagerapi (App Service) ──► snet-api ─┐  virtual network, two subnets, nothing else attached
                                                            ├──► internal Container Apps environment ──► math service
                              private DNS zone (the environment's default domain) resolves to the environment's private IP
```

| Setting | Where | Value |
|---|---|---|
| `MathModule__BaseUrl` | App Service settings of the API | `https://<app>.<default domain>`, the `mathModuleBaseUrl` output of `main.bicep` |
| `MathModule__ApiKey` | App Service settings of the API | the shared key |
| `MATH_API_KEY` | secret of the Container App (set by `main.bicep` from `mathApiKey`) | the same key |

## Why Container Apps and not App Service for Containers

- The API's plan is a **Windows** plan (S1: `reserved: false` in `PortfolioManager.API/Properties/ServiceDependencies`),
  and a Windows plan cannot run a Linux container. App Service for Containers would mean a second, Linux plan that
  bills all month, and its access restrictions still need the same VNet to say "only the API".
- A Container Apps environment can be **internal** (no public IP, no public DNS record), which is the "only the API
  can reach it" requirement as a property of the resource, not a list of addresses to maintain. Restricting by the API's
  outbound IPs would not do: App Service outbound addresses are shared with other customers' apps.
- Revisions give a free rollback: a new image that fails its `/health` probe never replaces the working one.

## What protects the service

1. **No route in from the internet.** The environment is `internal`: no public IP, and its domain exists only in the
   private DNS zone linked to this one virtual network.
2. **Only the API is on that network.** The virtual network holds two subnets, the API's and the environment's, and no
   VM, VPN or peering. If you ever peer or connect it to another network, add a network security group on
   `snet-containerapps` that allows only `snet-api` (and the subnet itself) first.
3. **API key.** Every endpoint except `/health` needs `X-API-Key`. The container starts with `REQUIRE_API_KEY=true`, so it
   refuses to start without a `MATH_API_KEY` of at least 32 characters: a forgotten secret stops the deployment, it does
   not leave the service open. `/docs` and `/openapi.json` are off (`ENABLE_DOCS=false`).
4. **No registry password.** The app pulls its image with a managed identity that has the `AcrPull` role; the registry's
   admin user is off.

## First deployment

Run these from the repository root. Use Git Bash or WSL for the commands as written. Resource group and API names are
the ones in `PortfolioManager.API/Properties/ServiceDependencies`; check them with `az webapp list`.

```bash
RG=PortfolioManagerResourceGroup
API=portfoliomanagerapi
```

**0. Prerequisites.**

```bash
az login
az extension add --name containerapp --upgrade
for p in Microsoft.App Microsoft.OperationalInsights Microsoft.ContainerRegistry Microsoft.ManagedIdentity Microsoft.Network; do az provider register --namespace $p; done

# The template needs the region of the API's plan, and the plan must be Basic or higher for VNet integration.
az webapp show --resource-group $RG --name $API --query "{location:location, plan:serverFarmId}" --output json
az appservice plan show --ids <plan id from above> --query "{sku:sku.name, linux:reserved}" --output json
```

If the region is not `germanywestcentral`, pass `location=<region>` to the deployment below. Deploying `main.bicep`
creates roles (`AcrPull`), so the account needs `Owner` or `User Access Administrator` on the resource group.

**1. Make the key** and keep it in a shell variable (it is not written anywhere in the repository):

```bash
MATH_KEY=$(openssl rand -base64 24)   # 32 characters
```

**2. Network, registry, environment, DNS** (no image yet, so the app itself is not created):

```bash
az deployment group create --resource-group $RG --template-file infra/main.bicep --parameters mathApiKey="$MATH_KEY"
```

**3. Build and push the first image.** Docker must be running. (A new version later: see "Operating it".)

```bash
ACR=$(az deployment group show --resource-group $RG --name main --query properties.outputs.registryName.value --output tsv)
az acr login --name $ACR
docker build --tag $ACR.azurecr.io/portfolio-math-module:1 PortfolioManager/PortfolioManager.MathModule
docker push $ACR.azurecr.io/portfolio-math-module:1
```

**4. Create the app** by deploying again with the image:

```bash
az deployment group create --resource-group $RG --template-file infra/main.bicep \
  --parameters mathApiKey="$MATH_KEY" containerImage=$ACR.azurecr.io/portfolio-math-module:1
az deployment group show --resource-group $RG --name main --query properties.outputs --output json
```

The outputs give `mathModuleBaseUrl`, `vnetName` and `apiSubnetName` for the next step. `mathApiKey` is a required
parameter, so every deployment of `main.bicep` takes it (the key can be read back from the API's settings if the shell
variable is gone).

**5. Connect the API.** This changes the existing site, which `main.bicep` deliberately does not manage (deploying a site
from a template replaces its app settings). `appsettings set` only adds or changes the named settings; the site restarts.

```bash
az webapp vnet-integration add --resource-group $RG --name $API --vnet portfoliomanager-vnet --subnet snet-api
az webapp config appsettings set --resource-group $RG --name $API \
  --settings "MathModule__BaseUrl=<mathModuleBaseUrl>" "MathModule__ApiKey=$MATH_KEY"
```

Leave "route all traffic" (`WEBSITE_VNET_ROUTE_ALL`) off. The service has a private `10.x` address, which goes through the
network by default; the API's traffic to the database and everything else keeps leaving the way it does today. The
service's own calls to Yahoo Finance leave through the environment's platform-managed public IP.

**6. Check it.**

- From your computer, `nslookup <fqdn of the app>` must **not** resolve: the name exists only inside the network.
- On `https://portfoliomanagerapi.scm.azurewebsites.net` (Debug console, CMD): `nameresolver <fqdn>` shows a `10.40.0.x`
  address and `tcpping <fqdn>:443` succeeds.
- In the client, add a position (a real ticker, for example `MSFT`): that calls `GET /v1/stocks/{symbol}`. Then open the
  portfolio's chart and the optimisation page. Those are the three things that did not work on Azure before.
- `az containerapp logs show --resource-group $RG --name portfoliomanager-math --follow` shows the requests.
  A 502 from the API with `Math module call ... was rejected with 401` in the API log means the two keys differ.

Deploy the math service first and the API after it (the API refuses to start without `MathModule__BaseUrl`). The release that
adds this also changes the API routes and the database schema: read the backup note in `PortfolioManager/README.md` first.

## Operating it

- **Deploy a new image.** Nothing in GitHub publishes to Azure (the CI workflow only builds the image to check it). Push a
  new tag and point the app at it; the old revision keeps serving until the new one passes its `/health` probe:

  ```bash
  TAG=2   # any tag not used before
  az acr login --name $ACR
  docker build --tag $ACR.azurecr.io/portfolio-math-module:$TAG PortfolioManager/PortfolioManager.MathModule
  docker push $ACR.azurecr.io/portfolio-math-module:$TAG
  az containerapp update --resource-group $RG --name portfoliomanager-math --image $ACR.azurecr.io/portfolio-math-module:$TAG
  ```

  A later deployment of `main.bicep` must pass this image as `containerImage`, or it would roll the app back to the old one.
- **Rotate the key.** Generate a new one, then change both sides together: `az deployment group create ... mathApiKey=<new>
  containerImage=<current image>` (this makes a new revision) and the API's `MathModule__ApiKey`. Between the two steps
  the API answers 502 for market data.
- **Roll back.** `az containerapp revision list --resource-group $RG --name portfoliomanager-math`, then
  `az containerapp revision activate` on the previous one (single-revision mode: activating one deactivates the other).
- **Cost.** The registry (Basic), the always-on replica (0.5 vCPU, 1 GiB) and the log workspace (capped at 0.5 GB a day)
  are the items that bill; the virtual network, subnets and the DNS zone are free or cents. Check the Azure pricing
  calculator for Germany West Central. `minReplicas=0` lets the replica scale to zero and costs a cold start on the first
  request after idle; the API's 90 s timeout and retries cover it, the user waits.
- **Remove it.** Delete the new resources one by one: the container app, environment, registry, identity, log
  workspace, private DNS zone and virtual network (remove the API's VNet integration first:
  `az webapp vnet-integration remove`). Do **not** delete the resource group: it also holds the API and the database.

## Things this was written without being able to check

`main.bicep` was written against Microsoft's documentation, not deployed. The points that decide whether
it works on the first try, in the order they would fail:

1. `external: true` on the ingress. In an internal environment that means "reachable from the virtual network"; with
   `false` only other apps of the environment could call it, and the API would time out.
2. The private DNS zone. The zone is named after the environment's default domain and holds a wildcard record to its
   static IP; if the API cannot resolve the name, check the zone's link to the virtual network.
3. TLS. The API calls `https://<fqdn>`, which the environment serves with its own certificate for the default domain.
4. `Microsoft.App` and the other providers must be registered in the subscription, and the region must allow them.
5. Outbound access. The service downloads prices from Yahoo Finance through the environment's default egress. If adding a
   ticker answers 502 while `/health` is fine, look at `az containerapp logs show` before suspecting the key.
