#!/usr/bin/env pwsh

<# .SYNOPSIS
  Deploy the Azure infrastructure. #>
  [CmdletBinding()]
  param (
      ## Identifier for the organisation or subscription to make global names unique.
      [string]$OrgId = "0x$($(az account show --query id --output tsv).Substring(0,4))",
      ## Deployment environment, e.g. Prod, Dev, QA, Stage, Test.
      [string]$Environment = 'Dev',
      ## The Azure region where the resource is deployed.
      [string]$Location = 'australiaeast'
  )

# Pre-requisites:

# Running these scripts requires the following to be installed:
#  * PowerShell, https://github.com/PowerShell/PowerShell
#  * Azure CLI, https://docs.microsoft.com/en-us/cli/azure/
#
# To run:
#   az extension add --name azure-iot
#   az login
#   az account set --subscription <subscription id>
#   $VerbosePreference = 'Continue'
#   ./deploy-infrastructure.ps1

$ErrorActionPreference="Stop"

$SubscriptionId = $(az account show --query id --output tsv)
Write-Verbose "Using context subscription ID $SubscriptionId"

# Following standard naming conventions from Azure Cloud Adoption Framework
# https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming
# With an additional organisation or subscription identifier (after app name) in global names to make them unique 

$appName = 'tracedemo'

$rgName = "rg-$appName-$Environment-001".ToLowerInvariant()
$sbName = "sb-$appName-$OrgId-$Environment".ToLowerInvariant()
$sbqName = "sbq-demo".ToLowerInvariant()

# Following standard tagging conventions from  Azure Cloud Adoption Framework
# https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging

$TagDictionary = @{ WorkloadName = 'tracingdemo'; DataClassification = 'Non-business'; Criticality = 'Low';
  BusinessUnit = 'Demo'; ApplicationName = $appName; Env = $Environment }

# Create

Write-Host "Creating group $rgName"

# Convert dictionary to tags format used by Azure CLI create command
$tags = $TagDictionary.Keys | ForEach-Object { $key = $_; "$key=$($TagDictionary[$key])" }
az group create -g $rgName -l $location --tags $tags

Write-Host "Creating service bus $sbName with queue $sbqName"

# Convert tags returned from JSON result to the format used by Azure CLI create command
$rg = az group show --name $rgName | ConvertFrom-Json
$rgTags = $rg.tags | Get-Member -MemberType NoteProperty | ForEach-Object { "$($_.Name)=$($rg.tags.$($_.Name))" }

az servicebus namespace create --name $sbName --resource-group $rgName -l $rg.location --sku Standard --tags $rgTags
az servicebus queue create --name $sbqName --namespace-name $sbName --resource-group $rgName

# Output

az servicebus namespace authorization-rule keys list --namespace-name $sbName --resource-group $rgName --name RootManageSharedAccessKey --query primaryConnectionString -o tsv
$connectionString
