---
title: "Databricks Job Metrics template (Azure Functions)"
layout: default
---

# Databricks Job Metrics template (Azure Functions)

## Create your first Arcus Azure Functions Databricks project

First, install the template from NuGet:

```shell
> dotnet new --install Arcus.Templates.AzureFunctions.Databricks.JobMetrics
```

When installed, the template can be created with shortname: `arcus-az-databricks-jobmetrics`:

```shell
> dotnet new arcus-az-databricks-jobmetrics --name Arcus.Demo.AzureFunctions.Databricks.JobMetrics
```

## Features

Creates a starter worker project with by default configured:

* Azure Function timer that for each cycle the finished Databricks job runs reports as metrics ([official docs](https://background-jobs.arcus-azure.net/features/databricks/gain-insights))
* Dockerfile

### Configuration

And additional features available with options:
* `-es|--exclude-serilog`: Exclude the [Serilog](https://serilog.net/) logging infrastructure in the Azure Functions project which includes default enrichers ([version](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) and [application](https://observability.arcus-azure.net/features/telemetry-enrichment#application-enricher)), and sinking to Application Insights.