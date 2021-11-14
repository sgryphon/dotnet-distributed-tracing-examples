$suffix = (ConvertFrom-Json "$(az account show)").id.Substring(0,4)
az group create --name demo-tracing-rg --location australiaeast
az servicebus namespace create --resource-group demo-tracing-rg --name demo-trace-$suffix --sku Standard
az servicebus queue create --resource-group demo-tracing-rg --namespace-name demo-trace-$suffix --name demo-queue
az servicebus namespace authorization-rule keys list --resource-group demo-tracing-rg --namespace-name demo-trace-$suffix --name RootManageSharedAccessKey --query primaryConnectionString -o tsv

$connectionString = (az servicebus namespace authorization-rule keys list --resource-group demo-tracing-rg --namespace-name demo-trace-$suffix --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)
$connectionString
