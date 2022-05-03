#!/bin/bash

tmux new-session -d 'dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development'
tmux split-window -h 'dotnet run --project Demo.Service --urls "https://*:44301" --environment Development'
tmux attach-session -d
