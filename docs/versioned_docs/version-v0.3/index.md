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
* [Azure Service Bus Queue](features/servicebus-queue-worker-template)
* [Azure Service Bus Topic](features/servicebus-topic-worker-template)
* [Web API](features/web-api-template)

# Installation

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

### Web API

```shell
PM > dotnet new --install Arcus.Templates.WebApi
```

Read [here](features/web-api-template) for standard and configurable features.

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.templates/blob/master/LICENSE)*
