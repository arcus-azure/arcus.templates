---
title: "Azure Service Bus Topic worker template"
layout: default
---

# Azure Service Bus Topic Worker Project Template

⚠ Was previously called `arcus-servicebus-topic` but is now moved to a [general worker messaging project template](./worker-messaging-template.md) with a `--messaging ServiceBusTopic` project option.

## Create Your First Arcus Azure Service Bus Topic Worker Project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.ServiceBus.Topic
```

When installed, the template can be created with shortname: `arcus-servicebus-topic`:

```shell
> dotnet new arcus-servicebus-topic --name Arcus.Demo.ServiceBus.Topic 
```


## Features

Creates a starter worker project with by default configured:

* TCP health check probe ([official docs](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) & [Arcus docs](https://messaging.arcus-azure.net/features/tcp-health-probe)).
* Empty message pump on Azure Service Bus Topic ([official docs](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions))
* Dockerfile.

### Configuration

And additional features available with options:
* `-es|--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the worker project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.
