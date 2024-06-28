# Integration tests template

## Create your first Arcus integration tests project
First, install the template from NuGet:

```shell
> dotnet new install Arcus.Templates.IntegrationTests
```

When installed, the template can be created with shortname: `arcus-integration-tests`:

```shell
> dotnet new arcus-integration-tests --name Arcus.Demo.Tests.Integration
```

## Features
Creates an empty integration tests project with the following installed and setup:

### Files
* * Integration base class with:
  * Test configuration that allows for test classes to find configuration values directly (see [docs](https://testing.arcus-azure.net/features/core))
  * Test logger, specific for your test framework, to write diagnostic messages during the test run (see [docs](https://testing.arcus-azure.net/features/logging))
* `appsettings.json` and `appsettings.local.json` files to store and inject required configuration values (retrievable with the test configuration, see [docs](https://testing.arcus-azure.net/features/core)])

### Packages
* Default installed `Arcus.Testing.Core` package that provides common test infrastructure building blocks (see [docs](https://testing.arcus-azure.net/features/core))

## Configuration
And additional features available with options:
* `--test-framework` (default `xUnit`)
  * `xUnit`: adds xUnit as the test framework to the integration tests project
  * `NUnit`: adds NUnit as the test framework to the integration tests project
  * `MSTest`: adds MSTest as the test framework to the integration tests project
* `--include-assert-package` (default: `false`): includes the `Arcus.Testing.Assert` package to the integration tests project (see [docs](https://testing.arcus-azure.net/features/assertion))