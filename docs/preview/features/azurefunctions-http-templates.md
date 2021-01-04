---
title: "HTTP Trigger template (Azure Functions)"
layout: default
---

# HTTP Trigger template (Azure Functions)

## Create your first Arcus Azure Functions with HTTP Trigger project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.AzureFunctions.Http
```

When installed, the template can be created with shortname: `arcus-az-func-http`:

```shell
> dotnet new arcus-az-http --name Arcus.Demo.AzureFunctions.Http
```

## Features

Creates a starter worker project with by default configured:

* Azure Function HTTP trigger with:
    * HTTP correlation (see more info [here](https://webapi.arcus-azure.net/features/correlation) on what this includes)
    * Request content and header to restrict to JSON content and JSON parsing with data model annotations validation
    * Arcus secret store setup with Azure Key Vault secret provider (see more info [here](https://security.arcus-azure.net/features/secret-store/) on what this includes)
    * [Serilog](https://serilog.net/) as logging mechanism with default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher), [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher), and [correlation](https://webapi.arcus-azure.net/features/telemetry) when appropriate), sinking to Application Insights.
    * General exception handling that results in 500 Internal Server Error

### Security

As part of this template the following HTTP header value(s) are removed for security sake:
* `Server` header * Provides information concerning the web runtime
