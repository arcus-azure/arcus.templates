FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0.203-bullseye-slim AS build
WORKDIR /src
COPY ["Arcus.Templates.AzureFunctions.Http.csproj", ""]

COPY . .
WORKDIR "/src/."
RUN dotnet build "Arcus.Templates.AzureFunctions.Http.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Arcus.Templates.AzureFunctions.Http.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV AzureWebJobsScriptRoot=/app \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
