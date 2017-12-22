#!/bin/bash

export ASPNETCORE_URLS="http://*:$PORT"

set -e
run_cmd="dotnet run --server.urls=https://localhost:$PORT --no-launch-profile"

until dotnet restore && dotnet build; do
>&2 echo "No project to restore/build..."
sleep 1
done

#until dotnet ef database update; do
#>&2 echo "Database is starting up..."
#sleep 1
#done

>&2 echo "Ready - starting kestrel"
exec $run_cmd