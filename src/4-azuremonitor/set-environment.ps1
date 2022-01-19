#!/usr/bin/env pwsh

[CmdletBinding()]
param (
    ## Deployment environment, e.g. Prod, Dev, QA, Stage, Test.
    [string]$Environment = $ENV:DEPLOY_ENVIRONMENT ?? 'Dev',
    ## Identifier for the organisation (or subscription) to make global names unique.
    [string]$OrgId = $ENV:DEPLOY_ORGID ?? "0x$((az account show --query id --output tsv).Substring(0,4))"
)

$appName = 'tracedemo'
$rgName = "rg-$appName-$Environment-001".ToLowerInvariant()
$sbName = "sb-$appName-$OrgId-$Environment".ToLowerInvariant()
$appiName = "appi-$appName-$Environment".ToLowerInvariant()

$sbConnectionString = (az servicebus namespace authorization-rule keys list --namespace-name $sbName --resource-group $rgName --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)
$aiConnectionString = (az monitor app-insights component show --app $appiName -g $rgName  --query connectionString -o tsv)

$ENV:DOTNET_ENVIRONMENT = $Environment
$ENV:ConnectionStrings__ServiceBus = $sbConnectionString
$ENV:ApplicationInsights__ConnectionString = $aiConnectionString
