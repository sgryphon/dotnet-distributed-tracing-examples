#!/usr/bin/env pwsh

[CmdletBinding()]
param (
      [string]$OrgId = "0x$($(az account show --query id --output tsv).Substring(0,4))",
      [string]$Environment = 'Dev',
      [string]$Location = 'australiaeast'
)

$SubscriptionId = $(az account show --query id --output tsv)
Write-Verbose "Using context subscription ID $SubscriptionId"

# Following standard naming conventions from Azure Cloud Adoption Framework
# https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming
# With an additional organisation or subscription identifier (after app name) in global names to make them unique 

$appName = 'tracedemo'
$rgName = "rg-$appName-$Environment-001".ToLowerInvariant()

# Following standard tagging conventions from  Azure Cloud Adoption Framework
# https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging

$TagDictionary = @{ WorkloadName = 'tracingdemo'; DataClassification = 'Non-business'; Criticality = 'Low';
  BusinessUnit = 'Demo'; ApplicationName = $appName; Env = $Environment }

# Create

Write-Host "Creating group $rgName"

# Convert dictionary to tags format used by Azure CLI create command
$tags = $TagDictionary.Keys | ForEach-Object { $key = $_; "$key=$($TagDictionary[$key])" }
az group create -g $rgName -l $location --tags $tags
