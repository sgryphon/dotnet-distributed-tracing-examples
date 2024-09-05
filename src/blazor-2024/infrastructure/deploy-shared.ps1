#!/usr/bin/env pwsh

<# .SYNOPSIS
  Deploy landing zone shared services into Azure.

.NOTES
  This creates shared services in your Azure subscription.

  This includes Azure KeyVault, Azure Monitor, and App Insights.

  Running these scripts requires the following to be installed:
  * PowerShell, https://github.com/PowerShell/PowerShell
  * Azure CLI, https://docs.microsoft.com/en-us/cli/azure/

  You also need to connect to Azure (log in), and set the desired subscription context.

  Follow standard naming conventions from Azure Cloud Adoption Framework, 
  with an additional organisation or subscription identifier (after app name) in global names 
  to make them unique.
  https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming

  Follow standard tagging conventions from  Azure Cloud Adoption Framework.
  https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging

.EXAMPLE

   az login
   az account set --subscription <subscription id>
   $VerbosePreference = 'Continue'
   ./deploy-shared.ps1
#>
[CmdletBinding()]
param (
    ## Deployment environment, e.g. Prod, Dev, QA, Stage, Test.
    [string]$Environment = $ENV:DEPLOY_ENVIRONMENT ?? 'Dev',
    ## The Azure region where the resource is deployed.
    [string]$Location = $ENV:DEPLOY_LOCATION ?? 'australiaeast',
    ## Identifier for the organisation (or subscription) to make global names unique.
    [string]$OrgId = $ENV:DEPLOY_ORGID ?? "0x$((az account show --query id --output tsv).Substring(0,4))"
)

<#
To run interactively, start with:

$VerbosePreference = 'Continue'

$Environment = $ENV:DEPLOY_ENVIRONMENT ?? 'Dev'
$Location = $ENV:DEPLOY_LOCATION ?? 'australiaeast'
$OrgId = $ENV:DEPLOY_ORGID ?? "0x$((az account show --query id --output tsv).Substring(0,4))"
#>

$ErrorActionPreference="Stop"

$SubscriptionId = $(az account show --query id --output tsv)
Write-Verbose "Deploying scripts for environment '$Environment' in subscription '$SubscriptionId'$($AddPublicIpv4 ? ' with IPv4' : '')"

# Following standard naming conventions from Azure Cloud Adoption Framework
# https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming
# With an additional organisation or subscription identifier (after app name) in global names to make them unique 

$rgName = "rg-shared-$Environment-001".ToLowerInvariant()

# Landing zone templates have Azure Monitor (but not app insights), KeyVault, and a diagnostics storage account

$logName = "log-shared-$Environment".ToLowerInvariant()
$appiName = "appi-shared-$Environment".ToLowerInvariant()
$kvName = "kv-shared-$OrgId-$Environment".ToLowerInvariant()
$stfuncName = "stfunc$OrgId$Environment".ToLowerInvariant()

# Following standard tagging conventions from  Azure Cloud Adoption Framework
# https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging

$TagDictionary = @{ DataClassification = 'Non-business'; Criticality = 'Low';
  BusinessUnit = 'IoT'; Env = $Environment }

# Create

Write-Host "Creating group $rgName"

# Convert dictionary to tags format used by Azure CLI create command
$tags = $TagDictionary.Keys | ForEach-Object { $key = $_; "$key=$($TagDictionary[$key])" }
$rg = az group create -g $rgName -l $location --tags $tags | ConvertFrom-Json

# Convert tags returned from JSON result to the format used by Azure CLI create command
#$rg = az group show --name $rgName | ConvertFrom-Json
#$rgTags = $rg.tags | Get-Member -MemberType NoteProperty | ForEach-Object { "$($_.Name)=$($rg.tags.$($_.Name))" }

Write-Verbose "Creating log analytics workspace $logName"

az monitor log-analytics workspace create `
  --resource-group $rgName `
  -l $rg.location `
  --workspace-name $logName `
  --tags $tags

Write-Verbose "Creating app insights (may take a while) $appiName"
az extension add -n application-insights

$ai = az monitor app-insights component create `
  --app $appiName `
  -g $rgName `
  --location $rg.location `
  --workspace $logName `
  --tags $tags | ConvertFrom-Json

# copy the key into another variable, to ensure the property is dereferenced when passing to
# the az command line
$aiKey = $ai.instrumentationKey
$aiConnectionString = $ai.connectionString

Write-Verbose "Creating key vault $kvName"

az keyvault create `
  --resource-group $rgName `
  -l $rg.location `
  --name $kvName `
  --tags $tags

Write-Verbose "Creating storage account"

az storage account create --name $stfuncName `
  --sku Standard_LRS `
  --resource-group $rgName `
  -l $rg.location `
  --tags $tags

# Output

Write-Verbose "Client App Instrumentation Key: $aiKey"
Write-Verbose "Client App Connection String: $aiConnectionString"

Write-Verbose "Deployment Complete"
