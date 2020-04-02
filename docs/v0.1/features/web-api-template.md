---
title: "Web API template"
layout: default
---

# Web API Project Template

## Create Your First Arcus Web API Project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.WebApi
```

When installed, the template can be created with shortname: `arcus-webapi`:

```shell
> dotnet new arcus-webapi --name Arcus.Demo.WebAPI
```


## Features

Creates a starter web API project with by default configured:

![](https://img.shields.io/badge/Available%20starting-v0.1-green?link=https://github.com/arcus-azure/arcus.templates/releases/tag/v0.1.0)

* [Exception middleware](https://webapi.arcus-azure.net/features/logging) to log unhandled exceptions thrown during request processing.
* Content negotiation that only supports `application/json`.
* OpenAPI docs generation and UI (only available locally).

### Configuration

And additional features available with options:

* `-A|--Authentication` (default `None`)
  * `SharedAccessKey`: adds [shared access key authentication](https://webapi.arcus-azure.net/features/
  * `None`: no authentication configured on the API project.
