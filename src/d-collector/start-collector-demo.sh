#!/bin/bash

version=$(dotnet gitversion /output json /showvariable InformationalVersion)
az_instrumentation_key=$(az monitor app-insights component show -a appi-tracedemo-dev -g rg-tracedemo-dev-001 -o tsv --query instrumentationKey)

tmux new-session -d 'ASPNETCORE_URLS="http://localhost:8002" npm run start --prefix Demo.WebApp/ClientApp'
tmux split-window -h "dotnet run --project Demo.WebApp -p:InformationalVersion=$version -- --urls http://*:8002 --environment Development"
tmux split-window -f "dotnet run --project Demo.Service -p:InformationalVersion=$version -- --urls https://*:44301 --environment Development"
tmux split-window -h "dotnet run --project Demo.Worker -p:InformationalVersion=$version -- --environment Development"
tmux split-window -fhp 67 "docker run -it --rm -e AZ_INSTRUMENTATION_KEY=$az_instrumentation_key --network demo_default -p 4317:4317 -v $PWD/otel-collector-config.yaml:/etc/otelcol-contrib/config.yaml otel/opentelemetry-collector-contrib:0.50.0"

tmux attach-session -d

