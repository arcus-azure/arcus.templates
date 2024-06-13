---
title: "Azure Service Bus Topic Trigger template (Azure Functions)"
layout: default
---

# Azure Service Bus Topic Trigger template (Azure Functions)

## Create your first Arcus Azure Functions with Azure Service Bus Topic Trigger project

First, install the template from NuGet:

```shell
> dotnet new install Arcus.Templates.AzureFunctions.ServiceBus.Topic
```

When installed, the template can be created with shortname: `arcus-az-func-servicebus-topic`:

```shell
> dotnet new arcus-az-func-servicebus-topic --name Arcus.Demo.AzureFunctions.ServiceBus.Topic
```

## Features

Creates a starter worker project with by default configured:

* Arcus secret store setup with Azure Key Vault secret provider (see more info [here](https://security.arcus-azure.net/features/secret-store/) on what this includes)
* [Serilog](https://serilog.net/) as logging mechanism with default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher), [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher), sinking to Application Insights.
* Dockerfile

### Configuration

And additional features available with options:
* `--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the Azure Functions project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.