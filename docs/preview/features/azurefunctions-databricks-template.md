---
title: "Databricks Job Metrics template (Azure Functions)"
layout: default
---

# Azure Functions Databricks project template

## Create your first Arcus Azure Functions Databricks project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.AzureFunctions.Databricks
```

When installed, the template can be created with shortname: `arcus-az-databricks`:

```shell
> dotnet new arcus-az-databricks --name Arcus.Demo.AzureFunctions.Databricks
```

## Features

Creates a starter worker project with by default configured:

* Azure Function timer that for each cycle the finished Databricks job runs reports as metrics ([official docs](https://background-jobs.arcus-azure.net/features/databricks/gain-insights))
