# @file-module:docker
# Build from the solution root: docker build -f src/AppName.Api/Dockerfile -t appname-api .
ARG VERSION=0.0.0-dev

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION
ARG TARGETARCH
WORKDIR /source
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/AppName.Api/AppName.Api.csproj -c Release -a $TARGETARCH -o /app -p:Version=$VERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl is for the HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# /app/data holds the files that must survive a new container: the JWT key file and the local database.
RUN mkdir -p /app/data && chown app:app /app/data
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1
USER app
ENTRYPOINT ["dotnet", "AppName.Api.dll"]
