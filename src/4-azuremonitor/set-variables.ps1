function Set-Variables
{
  [CmdletBinding()]
  param([parameter(Mandatory)][string]$EnvironmentName)

  if ($EnvironmentName -eq "demo")
  {
    $global:Location = 'australiaeast'
    $global:ResourceGroup='demo-tracing-rg'
    $global:ServiceBusNamespacePrefix='demo-trace-'
    $global:QueueName='demo-queue'
    $global:LogAnalyticsWorkspaceName='trace-demo-logs'
    $global:AppInsightsName='trace-demo-app-insights'
  }
  else
  {
    Write-Error "Unrecognized environment name, cannot infer other variable values"
  }
}
