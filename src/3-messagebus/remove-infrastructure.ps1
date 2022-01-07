#!/usr/bin/env pwsh

<# .SYNOPSIS
  Remove the Azure infrastructure. #>
[CmdletBinding()]
param (
    ## Deployment environment, e.g. Prod, Dev, QA, Stage, Test.
    [string]$Environment = 'Dev'
)

$ErrorActionPreference="Stop"

$SubscriptionId = $(az account show --query id --output tsv)
Write-Verbose "Removing from context subscription ID $SubscriptionId"

$appName = 'tracedemo'

$rgName = "rg-$appName-$Environment-001".ToLowerInvariant()

az group delete --name $rgName
