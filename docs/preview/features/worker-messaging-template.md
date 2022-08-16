---
title: "Worker messaging template"
layout: default
---

# Worker Messaging Project Template

## Create Your First Arcus Worker Messaging Project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.Worker.Messaging
```

When installed, the template can be created with shortname: `arcus-worker-messaging`:

```shell
> dotnet new arcus-worker-messaging --messaging ServiceBusTopic --name Arcus.Demo.ServiceBus.Topic
```

We support several messaging systems to create your worker messaging project:
* `--messaging ServiceBusTopic` (default)
* `--messaging ServiceBusQueue`

## Features

Creates a starter worker project with by default configured:

* TCP health check probe ([official docs](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) & [Arcus docs](https://messaging.arcus-azure.net/features/tcp-health-probe)).
* Message pump on the configured messaging system
* Dockerfile.

### Configuration

And additional features available with options:
* `-es|--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the worker project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.
