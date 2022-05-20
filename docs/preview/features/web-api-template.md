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

The OpenAPI documentation is available by-default. Be careful of exposing sensitive information with the OpenAPI documentation, only expose what's necessary and hide everything else.

### Health

A default health controller is available that exposes the configured health checks as an aggregated health report. 
For more information on application health, see [Microsoft's documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks).

The controller doesn't directly exposes Microsoft's `HealthReport` model but uses a custom `HealthReportJson` operation model which eliminates the exception details from the original report.
This way the application's health can be exposed in a safe manner without also exposing exception and assembly information to the user.