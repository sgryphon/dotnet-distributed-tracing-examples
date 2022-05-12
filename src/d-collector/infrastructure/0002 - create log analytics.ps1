#!/usr/bin/env pwsh

[CmdletBinding()]
param (
      [string]$OrgId = "0x$($(az account show --query id --output tsv).Substring(0,4))",
      [string]$Environment = 'Dev',
      [string]$Location = 'australiaeast'
)

$appName = 'tracedemo'
$rgName = "rg-$appName-$Environment-001".ToLowerInvariant()
$logName = "log-$appName-$Environment".ToLowerInvariant()
$appiName = "appi-$appName-$Environment".ToLowerInvariant()

# Convert tags returned from JSON result to the format used by Azure CLI create command
$rg = az group show --name $rgName | ConvertFrom-Json
$rgTags = $rg.tags | Get-Member -MemberType NoteProperty | ForEach-Object { "$($_.Name)=$($rg.tags.$($_.Name))" }

Write-Verbose "Creating log analytics workspace $rgName"

az monitor log-analytics workspace create `
  --resource-group $rgName `
  -l $rg.location `
  --workspace-name $logName `
  --tags $rgTags

Write-Verbose "Creating app insights (may take a while) $appiName"
az extension add -n application-insights

$ai = az monitor app-insights component create `
  --app $appiName `
  -g $rgName `
  --location $rg.location `
  --workspace $logName `
  --tags $rgTags | ConvertFrom-Json

# copy the key into another variable, to ensure the property is dereferenced when passing to
# the az command line
$aiKey = $ai.instrumentationKey
$aiConnectionString = $ai.connectionString

Write-Verbose "Client App Instrumentation Key: $aiKey"
Write-Verbose "Client App Connection String: $aiConnectionString"
