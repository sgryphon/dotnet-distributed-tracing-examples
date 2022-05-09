#!/bin/bash

version=$(dotnet gitversion /output json /showvariable InformationalVersion)
tmux new-session -d 'ASPNETCORE_URLS="http://localhost:8002" npm run start --prefix Demo.WebApp/ClientApp'
tmux split-window -h "dotnet run --project Demo.WebApp -p:InformationalVersion=$version -- --urls http://*:8002 --environment Development"
tmux split-window -f "dotnet run --project Demo.Service -p:InformationalVersion=$version -- --urls https://*:44301 --environment Development"
tmux split-window -h "dotnet run --project Demo.Worker -p:InformationalVersion=$version -- --environment Development"
tmux attach-session -d
