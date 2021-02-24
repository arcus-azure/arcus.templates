---
title: "Web API template"
layout: default
---

# Web API Project Template

## Create Your First Arcus Web API Project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.WebApi
```

When installed, the template can be created with shortname: `arcus-webapi`:

```shell
> dotnet new arcus-webapi --name Arcus.Demo.WebAPI
```


## Features

Creates a starter web API project with by default configured:

![](https://img.shields.io/badge/Latest%20version-v0.3-green?link=https://github.com/arcus-azure/arcus.templates/releases/tag/v0.3.0)

* Provides basic health endpoint with [ASP.NET Core health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) with [OpenAPI support](https://www.codit.eu/blog/documenting-asp-net-core-health-checks-with-openapi/).
* Docker building file.
* Default console logger.

### Configuration

And additional features available with options:

* `-au|--authentication` (default `None`)
  * `SharedAccessKey`: adds [shared access key authentication](https://webapi.arcus-azure.net/features/security/auth/shared-access-key) mechanism to the API project
  * `Certificate`: adds [client certificate authentication](https://webapi.arcus-azure.net/features/security/auth/certificate) mechanism to the API project
  * `JWT`: adds JWT (JSON Web Token) authentication mechanism to the API project
  * `None`: no authentication configured on the API project.
* `-ia|--include-appsettings` (default `false`): includes a `appsettings.json` file to the web API project.
* `-ec|--exclude-correlation` (default `false`): excludes the [capability to correlate](https://webapi.arcus-azure.net/features/correlation) between HTTP requests/responses from the API project.
* `-eo|--exclude-openApi` (default `false`): exclude the [ASP.NET OpenAPI docs generation and UI](https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-3.1&tabs=visual-studio) from API project.
* `-lo|--logging` (default `Serilog`)
  * `Console`: no extra logging mechanism except for the default console logging will be added to the web API project.
  * `Serilog`: adds [Serilog](https://serilog.net/) as logging mechanism with request logging, default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher), [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher), and [correlation](https://webapi.arcus-azure.net/features/telemetry) when appropriate), sinking to Application Insights to the web API project.

### Security

As part of this template the following HTTP header(s) are removed for security sake:
* `Server` header * Provides information concerning the Web API runtime

## Older Versions

* [v0.5.0](../v0.5/web-api-template)
* [v0.4.0](../v0.4/web-api-template)
* [v0.3.0](../v0.3/web-api-template)
* [v0.2.0](../v0.2/web-api-template)
* [v0.1.0](../v0.1/web-api-template)