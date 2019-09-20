FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Arcus.Template.WebApi/Arcus.Template.WebApi.csproj", "Arcus.Template.WebApi/"]
RUN dotnet restore "Arcus.Template.WebApi/Arcus.Template.WebApi.csproj"
COPY . .
WORKDIR "/src/Arcus.Template.WebApi"
RUN dotnet build "Arcus.Template.WebApi.csproj" -c Release
RUN dotnet new -i "bin/Release/Arcus.Template.WebApi.*.nupkg"
RUN dotnet new arcus-webapi -o /output -n MyProject
WORKDIR "output"
RUN dotnet build "MyProject.csproj" -c Release -o /app
RUN ls output

FROM build AS publish
RUN dotnet publish "MyProject.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MyProject.dll"]