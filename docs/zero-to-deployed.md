# Zero to deployed

From nothing to a Function app running in dev, test and prod. Time each step during the pilot; the total is the time-to-first-deploy metric.

## 1. Install the template (1 min)

Add the internal feed once, then install:

```bash
dotnet nuget add source "<internal feed v3 URL>" --name internal   # once per machine
dotnet new install FluentFunctions.Templates
```

## 2. Generate the app (1 min)

```bash
dotnet new fluentfn --name Contoso.Orders --trigger http --trigger servicebus --iac bicep
cd Contoso.Orders
git init && git add . && git commit -m "Generate from fluentfn"
```

Run `dotnet new fluentfn --help` for every option.

## 3. Build and run locally (5 min)

```bash
dotnet test
azurite --silent &
cd src/Contoso.Orders.Functions && func start
```

The generated `README.md` has a `curl` to try the HTTP trigger. If a setting is wrong, `func start` stops and names it.

## 4. Replace the samples (as long as it takes)

The orders and clean-up samples show the shape, not your domain. Replace the records and handlers in `*.Core`, keep the adapters thin, and keep the tests passing. The rules are in the app's `docs/architecture.md`.

## 5. Set up Azure DevOps (20 min, once per app)

1. Push the repo to Azure Repos (or GitHub, connected to Azure DevOps).
2. For each of `dev`, `test`, `prod`:
   - a service connection `sc-<app>-<env>` (Azure Resource Manager, workload identity federation), with Owner, or Contributor plus Role Based Access Control Administrator, on the target subscription or resource group;
   - an environment `<app>-<env>` with approvals on `test` and `prod`.
3. Terraform only: create the state storage account and `tfstate-<env>` containers, and set the two `tfState*` variables in `pipelines/azure-pipelines.yml`.
4. Create a pipeline from `pipelines/azure-pipelines.yml`.

`<app>` is the `appName` variable at the top of the pipeline.

## 6. Deploy (15 min)

Open a PR: the pipeline builds, tests and runs `what-if` / `terraform plan` against all three environments. Merge it: it deploys infrastructure and code to dev, then test and prod after their approvals.

The first deployment of each environment can log storage or Service Bus authorization errors for a few minutes while the new role assignments apply. They clear on their own.

## 7. Check it (5 min)

- Application Insights > Transaction search shows the HTTP request and the Service Bus processing in one trace.
- The Function app's **Environment variables** blade has no connection strings or keys.
