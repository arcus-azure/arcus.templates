FROM mcr.microsoft.com/dotnet/runtime:8.0.8-jammy-chiseled AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src
COPY ["Arcus.Templates.EventHubs.csproj", ""]

COPY . .
WORKDIR "/src/."
RUN dotnet build "Arcus.Templates.EventHubs.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Arcus.Templates.EventHubs.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Arcus.Templates.EventHubs.dll"]
