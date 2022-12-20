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
> dotnet new arcus-az-func-http --name Arcus.Demo.AzureFunctions.Http
```

## Features

Creates a starter worker project with by default configured:

* Arcus secret store setup with Azure Key Vault secret provider (see more info [here](https://security.arcus-azure.net/features/secret-store/) on what this includes)
* [Serilog](https://serilog.net/) as logging mechanism with default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher), [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher), and [correlation](https://webapi.arcus-azure.net/features/telemetry) when appropriate), sinking to Application Insights.

* Example Azure Function HTTP 'order' trigger with:
    * HTTP correlation (see more info [here](https://webapi.arcus-azure.net/features/correlation) on what this includes)
    * Request content and header to restrict to JSON content and JSON parsing with data model annotations validation
    * General exception handling that results in 500 Internal Server Error
    * OpenAPI docs generation and UI (see more info [here](https://github.com/Azure/azure-functions-openapi-extension))
    * Dockerfile

### Configuration

And additional features available with options:

* `--functions-worker`: Configures the type of Azure Functions worker type the project should target.
  * `isolated` (default): Uses the isolated Azure Functions worker type which runs on a different process as the Azure Function
  * `inProcess`: Uses the in-process Azure Functions worker type which runs on the same process as run Azure Function
  For more information on the difference between the two, see [Microsoft's documentation](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide).
* `--include-healthchecks` (default `false`): include a default Health Azure Function and health check services from the project
* `--exclude-openapi` (default `false`): exclude the [Azure Functions OpenAPI docs generation and UI](https://github.com/Azure/azure-functions-openapi-extension) from the project.
* `--exclude-serilog` (default `false`): exclude the [Serilog](https://serilog.net/) logging infrastructure in the Azure Functions project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.

### Security

As part of this template the following HTTP header value(s) are removed for security sake:
* `Server` header * Provides information concerning the web runtime
