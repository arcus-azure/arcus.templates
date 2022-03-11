---
title: "Arcus Templates"
layout: default
permalink: /
redirect_from:
- /index.html
slug: /
sidebar_label: Welcome
sidebar_position: 1
---

# Using our templates

Arcus Templates provides various project templates:
* [Azure Service Bus Queue](./servicebus-queue-worker-template.md)
* [Azure Service Bus Topic](./servicebus-topic-worker-template.md)
* [Databricks Job Metrics (Azure Functions)](./azurefunctions-databricks-jobmetrics-template.md)
* [HTTP Trigger (Azure Functions)](./azurefunctions-http-templates.md)
* [Web API](./web-api-template.md)

# Installation

### Azure Service Bus Queue

```shell
PM > dotnet new --install Arcus.Templates.ServiceBus.Queue
```

Read [here](./servicebus-queue-worker-template.md) for standard and configurable features.

### Azure Service Bus Topic

```shell
PM > dotnet new --install Arcus.Templates.ServiceBus.Topic
```

Read [here](./servicebus-topic-worker-template.md) for standard and configurable features.

### Databricks Job Metrics (Azure Functions)

```shell
PM > dotnet new --install Arcus.Templates.AzureFunctions.Databricks.JobMetrics
```

Read [here](./azurefunctions-databricks-jobmetrics-template.md) for standard and configurable features.

### HTTP Trigger (Azure Functions)

```shell
PM > dotnet new --install Arcus.Templates.AzureFunctions.Http
```

Read [here](./azurefunctions-http-templates.md) for standard and configurable features.

### Web API

```shell
PM > dotnet new --install Arcus.Templates.WebApi
```

Read [here](./web-api-template.md) for standard and configurable features.

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.templates/blob/master/LICENSE)*
