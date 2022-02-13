#!/bin/sh

# Restore
dotnet restore \
    --runtime linux-musl-x64 \
    ./AzureUtils.AzureBlobProxy.csproj

# Publish only for linux runtime as we know where to deploy
dotnet publish --no-restore --nologo \
        --self-contained true \
        -r linux-musl-x64 \
        -p:PublishTrimmed=false \
        -p:PublishSingleFile=true \
        -c Release \
        -o /publish \
        ./AzureUtils.AzureBlobProxy.csproj

# Permissions:
# all files
chmod -R 644 /publish/*

# entry point
chmod 755 /publish/AzureUtils.AzureBlobProxy
