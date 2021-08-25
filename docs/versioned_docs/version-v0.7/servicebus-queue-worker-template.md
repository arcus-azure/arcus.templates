---
title: "Azure Service Bus Queue worker template"
layout: default
---

# Azure Service Bus Queue Worker Project Template

## Create Your First Arcus Azure Service Bus Queue Worker Project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.ServiceBus.Queue
```

When installed, the template can be created with shortname: `arcus-servicebus-queue`:

```shell
> dotnet new arcus-servicebus-queue --name Arcus.Demo.ServiceBus.Queue 
```


## Features

Creates a starter worker project with by default configured:

* TCP health check probe ([official docs](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) & [Arcus docs](https://messaging.arcus-azure.net/features/tcp-health-probe)).
* Empty message pump on Azure Service Bus Queue ([official docs](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues))
* Dockerfile.

### Configuration

And additional features available with options:
* `-es|--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the worker project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.
