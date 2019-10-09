---
title: "Web API template"
layout: default
---

# Web API Project Template

## Create Your First Arcus Web API Project

First, install the template from NuGet:

```shell
> dotnet new -i Arcus.Templates.WebApi
```

When installed, the template can be created with shortname: `arcus-webapi`:

```shell
> dotnet new arcus-webapi -n Arcus.Demo.WebAPI
```


## Features

Creates a starter web API project with by default configured:
* [Exception middleware](https://webapi.arcus-azure.net/features/logging) to log unhandled exceptions thrown during request processing.
* Content negotiation that only supports `application/json`.
* Swagger docs generation and UI only available in PROD environment.
* [Health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) (_middleware only_) and health endpoint (_based on _[info gained via health checks](https://www.codit.eu/blog/documenting-asp-net-core-health-checks-with-openapi/)_).
* Docker building file.
* Default console logger.

## Configuration

And additional features available with options:
* `-A|--Authentication` (default `None`)
  * `SharedAccessKey`: adds [shared access key authentication](https://webapi.arcus-azure.net/features/security/auth/shared-access-key) mechanism to the API project
  * `None`: no authentication configured on the API project.
