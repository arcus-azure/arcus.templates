# Arcus Templates
[![Build Status](https://dev.azure.com/codit/Arcus/_apis/build/status/Commit%20builds/CI%20-%20Arcus.Templates?branchName=master)](https://dev.azure.com/codit/Arcus/_build/latest?definitionId=765&branchName=master)
[![NuGet Badge](https://buildstats.info/nuget/Arcus.Templates.WebApi?includePreReleases=true)](https://www.nuget.org/packages/Arcus.Templates.WebApi/)

.NET Core templates to simplify the new projects and applying best practices.

![Arcus](https://raw.githubusercontent.com/arcus-azure/arcus/master/media/arcus.png)

# Installation

## Web API Template
We provide a template to build Web APIs. ([docs](https://templates.arcus-azure.net/features/web-api-template))

First, install the template:
```shell
> dotnet new --install Arcus.Templates.WebApi
```

When installed, the template can be created with shortname: `arcus-webapi`:
```shell
> dotnet new arcus-webapi --name Arcus.Demo.WebAPI
```

## Azure Service Bus Queue Worker Template
We provide a template to build worker projects with a Azure Service Bus message pump. ([docs](https://templates.arcus-azure.net/features/servicebus-queue-worker-template))

First, install the template:
```shell
> dotnet new --install Arcus.Templates.SerivceBus.Queue
```

When installed, the template can be created with shortname: `arcus-servicebus-queue`:
```shell
> dotnet new arcus-servicebus-queue --name Arcus.Demo.ServiceBus.Queue
```

## Azure Service Bus Topic Worker Template
We provide a template to build worker projects with a Azure Service Bus message pump. ([docs](https://templates.arcus-azure.net/features/servicebus-topic-worker-template))

First, install the template:
```shell
> dotnet new --install Arcus.Templates.ServiceBus.Topic
```

When installed, the template can be created with shortname: `arcus-servicebus-topic`:
```shell
> dotnet new arcus-servicebus-topic --name Arcus.Demo.ServiceBus.Topic
```

# Documentation

All documentation can be found on [here](https://templates.arcus-azure.net/).

## Versioning

We are using versioned documentation so that people can navigate to the docs for a specific version.

To support this, we use a folder with our docs for that specific version by calling it `v0.3`.

When introducing a new version, simply copy the folder of the previous version and call it `v0.5` where 0.5 is the new version of our packages.

Once the new version is released, we have to change that version to the new "latest" version. To do that, you must:

1. Change all docs in previous version so that they no longer have URL redirects
2. Introduce redirects in the new version of the docs (as documented on [GitHub](https://github.com/jekyll/jekyll-redirect-from#usage))

# License Information
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.
