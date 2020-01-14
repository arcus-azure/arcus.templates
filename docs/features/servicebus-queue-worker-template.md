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
* TCP [health check](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2) probe.
* Empty message pump on [Azure ServiceBus Queue](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues)
* Docker building file.
