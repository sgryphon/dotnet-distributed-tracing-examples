#!/usr/bin/env pwsh

[CmdletBinding()]
param (
      [string]$OrgId = "0x$($(az account show --query id --output tsv).Substring(0,4))",
      [string]$Environment = 'Dev',
      [string]$Location = 'australiaeast',
      [string]$Sku = $ENV:DEPLOY_SERVICEBUS_SKU ?? 'Standard'
)

$appName = 'tracedemo'
$rgName = "rg-$appName-$Environment-001".ToLowerInvariant()
$sbName = "sb-$appName-$OrgId-$Environment".ToLowerInvariant()
$sbqName = "sbq-demo".ToLowerInvariant()

# Convert tags returned from JSON result to the format used by Azure CLI create command
$rg = az group show --name $rgName | ConvertFrom-Json
$rgTags = $rg.tags | Get-Member -MemberType NoteProperty | ForEach-Object { "$($_.Name)=$($rg.tags.$($_.Name))" }

Write-Host "Creating service bus $sbName with queue $sbqName"

az servicebus namespace create --name $sbName --resource-group $rgName -l $rg.location --sku $Sku --tags $rgTags
az servicebus queue create --name $sbqName --namespace-name $sbName --resource-group $rgName

# Output

Write-Host "Connection string"

az servicebus namespace authorization-rule keys list --namespace-name $sbName --resource-group $rgName --name RootManageSharedAccessKey --query primaryConnectionString -o tsv
