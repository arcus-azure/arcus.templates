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
* [Exception middleware](https://webapi.arcus-azure.net/features/logging) to log unhandled exceptions thrown during request processing.
* Content negotiation that only supports `application/json`.
* Swagger docs generation and UI (only available locally).
* Provides basic health endpoint with [ASP.NET Core health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) with [OpenAPI support](https://www.codit.eu/blog/documenting-asp-net-core-health-checks-with-openapi/).
* Docker building file.
* Default console logger.

## Configuration

And additional features available with options:
* `-au|--authentication` (default `None`)
  * `SharedAccessKey`: adds [shared access key authentication](https://webapi.arcus-azure.net/features/security/auth/shared-access-key) mechanism to the API project
  * `Certificate`: adds [client certificate authentication](https://webapi.arcus-azure.net/features/security/auth/certificate) mechanism to the API project
  * `None`: no authentication configured on the API project.
* `-ec|--exclude-correlation` (default `false`): excludes the [capability to correlate](https://webapi.arcus-azure.net/features/correlation) between HTTP requests/responses.
* `-ia|--include-appsettings` (default `false`): includes a `appsettings.json` file to the web API project.
* `-lo|--logging` (default `Default`)
  * `Default`: no extra logging mechanism except for the default console logging will be added to the web API project.
  * `Serilog`: adds Serilog as logging mechanism with request logging to the web API project.

## Security
As part of this template the following HTTP header(s) are removed for security sake:
* `Server` header * Provides information concerning the Web API runtime
