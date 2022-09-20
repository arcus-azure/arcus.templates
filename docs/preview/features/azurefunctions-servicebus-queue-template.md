---
title: "Azure Service Bus Queue Trigger template (Azure Functions)"
layout: default
---

# Azure Service Bus Queue Trigger template (Azure Functions)

## Create your first Arcus Azure Functions with Azure Service Bus Queue Trigger project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.AzureFunctions.ServiceBus.Queue
```

When installed, the template can be created with shortname: `arcus-az-func-servicebus-queue`:

```shell
> dotnet new arcus-az-func-servicebus-queue --name Arcus.Demo.AzureFunctions.ServiceBus.Queue
```

## Features

Creates a starter worker project with by default configured:

* Arcus secret store setup with Azure Key Vault secret provider (see more info [here](https://security.arcus-azure.net/features/secret-store/) on what this includes)
* [Serilog](https://serilog.net/) as logging mechanism with default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher), [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher), sinking to Application Insights.
* Dockerfile

### Configuration

And additional features available with options:
* `--function-worker`: Configures the type of Azure Functions worker type the project should target.
  * `inProcess` (default): Uses the in-process Azure Functions worker type which runs on the same process as run Azure Function
  * `isolated`: Uses the isolated Azure Functions worker type which runs on a different process as the Azure Function
  For more information on the difference between the two, see [Microsoft's documentation](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide).
* `--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the Azure Functions project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.