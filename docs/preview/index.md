---
title: "Home"
layout: default
---

# Using our templates

Arcus Templates provides various project templates:
* [Web API](features/web-api-template)
* [Azure Service Bus Queue](features/servicebus-queue-worker-template)
* [Azure Service Bus Topic](features/servicebus-topic-worker-template)
* [Databricks Job Metrics (Azure Functions)](features/azurefunctions-databricks-jobmetrics-template)
* [HTTP Trigger](features/azurefunctions-http-template)

[Using Arcus project templates in Visual Studio](features/using-arcus-templates-in-visualstudio)

# Installation

### Web API

```shell
PM > dotnet new --install Arcus.Templates.WebApi
```

Read [here](features/web-api-template) for standard and configurable features.

### Azure Service Bus Queue

```shell
PM > dotnet new --install Arcus.Templates.ServiceBus.Queue
```

Read [here](features/servicebus-queue-worker-template) for standard and configurable features.

### Azure Service Bus Topic

```shell
PM > dotnet new --install Arcus.Templates.ServiceBus.Topic
```

Read [here](features/servicebus-topic-worker-template) for standard and configurable features.

### Databricks Job Metrics (Azure Functions)

```shell
PM > dotnet new --install Arcus.Templates.AzureFunctions.Databricks.JobMetrics
```

Read [here](features/azurefunctions-databricks-jobmetrics-template) for standard and configurable features.

### HTTP Trigger (Azure Functions)

```shell
PM > dotnet new --install Arcus.Templates.AzureFunctions.Http
```

Read [here](features/azurefunctions-http-template) for standard and configuratble features.

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.templates/blob/master/LICENSE)*
