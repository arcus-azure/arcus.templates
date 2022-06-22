FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0.301-bullseye-slim AS build
WORKDIR /src
COPY ["Arcus.Templates.AzureFunctions.Databricks.JobMetrics.csproj", ""]

COPY . .
WORKDIR "/src/."
RUN dotnet build "Arcus.Templates.AzureFunctions.Databricks.JobMetrics.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Arcus.Templates.AzureFunctions.Databricks.JobMetrics.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV AzureWebJobsScriptRoot=/app \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true