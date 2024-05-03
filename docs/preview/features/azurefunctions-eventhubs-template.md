---
title: "Azure EventHubs trigger template (Azure Functions)"
layout: default
---

# Azure EventHubs trigger template (Azure Functions)

## Create your first Arcus Azure Functions with Azure EventHubs trigger project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.AzureFunctions.EventHubs
```

When installed, the template can be created with shortname: `arcus-az-func-eventhubs`:

```shell
> dotnet new arcus-az-func-eventhubs --name Arcus.Demo.AzureFunctions.EventHubs
```

## Features

Creates a starter worker project with by default configured:

* Arcus secret store setup with Azure Key Vault secret provider (see more info [here](https://security.arcus-azure.net/features/secret-store/) on what this includes)
* [Serilog](https://serilog.net/) as logging mechanism with default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher), [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher), sinking to Application Insights.
* Dockerfile

### Configuration

And additional features available with options:
* `-es|--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the worker project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.
