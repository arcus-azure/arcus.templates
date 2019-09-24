FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.7-alpine3.9 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Arcus.Template.WebApi/Arcus.Template.WebApi.csproj", "Arcus.Template.WebApi/"]
RUN dotnet restore "Arcus.Template.WebApi/Arcus.Template.WebApi.csproj"
COPY . .
WORKDIR "/src/Arcus.Template.WebApi"
RUN dotnet build "Arcus.Template.WebApi.csproj" -c Release

FROM build AS publish
RUN dotnet publish "Arcus.Template.WebApi.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Arcus.Template.WebApi.dll"]