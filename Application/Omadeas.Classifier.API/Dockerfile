# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# syntax=docker/dockerfile:1.4

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for health checks (required by AWS App Runner)
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Application/Omadeas.Classifier.API/. Omadeas.Classifier.API/
COPY Application/Omadeas.Classifier.Core/. Omadeas.Classifier.Core/
COPY Application/Omadeas.Classifier.Infrastructure/. Omadeas.Classifier.Infrastructure/

# Mount the secret and configure NuGet source
RUN --mount=type=secret,id=github_token \
    export GITHUB_TOKEN=$(cat /run/secrets/github_token) && \
    dotnet nuget add source https://nuget.pkg.github.com/twincoretechorg/index.json \
      --name github \
      --username OWNER \
      --password "$GITHUB_TOKEN" \
      --store-password-in-clear-text && \
      dotnet restore "./Omadeas.Classifier.API/Omadeas.Classifier.API.csproj"

COPY . .
WORKDIR "/src/Omadeas.Classifier.API"
RUN dotnet build "./Omadeas.Classifier.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Omadeas.Classifier.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Omadeas.Classifier.API.dll"]
