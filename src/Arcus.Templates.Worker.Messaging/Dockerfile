FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14 AS build
WORKDIR /src
COPY ["Arcus.Templates.Worker.Messaging.csproj", ""]

COPY . .
WORKDIR "/src/."
RUN dotnet build "Arcus.Templates.Worker.Messaging.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Arcus.Templates.Worker.Messaging.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Arcus.Templates.Worker.Messaging.dll"]
