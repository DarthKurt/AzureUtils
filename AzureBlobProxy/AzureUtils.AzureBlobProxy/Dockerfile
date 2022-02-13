##############
### build  ###
##############
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /src

COPY . .

RUN chmod -R 755 ./build.sh \
        && ./build.sh

###############
### Runtime ###
###############
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64 as runtime

RUN \
        addgroup -S dotnet -g 5046 && \
        adduser -u 5046 \
                -G dotnet \
                --disabled-password \
                --home /app \
                --gecos '' \
                dotnetuser

RUN \
    # upgrade libs to remove potential vulnerabilities
    apk upgrade musl \
    # cleanup
    && apk del --purge \
    && rm -rf /var/cache/apk/* \
                /tmp/* \
                /tmp/.[!.]*

COPY --from=build /publish /app

USER dotnetuser
WORKDIR /app

ENTRYPOINT ["./AzureUtils.AzureBlobProxy"]
