using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if Serilog
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights;
#endif
#if (ExcludeOpenApi == false)
using Microsoft.OpenApi.Models;
using Arcus.Templates.WebApi.ExampleProviders;
using Swashbuckle.AspNetCore.Filters;
#endif
#if SharedAccessKeyAuth
using Arcus.Security.Core.Caching;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
#endif
#if CertificateAuth
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
#endif
#if JwtAuth
using System.Text;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
#endif

namespace Arcus.Templates.WebApi
{
    public class Program
    {
#if Serilog
        #warning Make sure that the appsettings.json is updated with your Azure Application Insights instrumentation key.
        private const string ApplicationInsightsInstrumentationKeyName = "Telemetry:ApplicationInsights:InstrumentationKey";
        
#endif
#if SharedAccessKeyAuth
        private const string SharedAccessKeyHeaderName = "X-API-Key";
        
#endif
        public static int Main(string[] args)
        {
#if Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            
            try
            {
                CreateWebApplication(args)
                    .Run();
                
                return 0;
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
#else
            CreateWebApplication(args)
                .Run();
            
            return 0;
#endif
        }
        
        public static WebApplication CreateWebApplication(string[] args)
        {
            IConfiguration configuration = CreateConfiguration(args);
            WebApplicationBuilder builder = CreateWebApplicationBuilder(args, configuration);
            
            WebApplication app = builder.Build();
            ConfigureApp(app);
            
            return app;
        }
        
        private static IConfiguration CreateConfiguration(string[] args)
        {
            IConfigurationRoot configuration =
                new ConfigurationBuilder()
                    .AddCommandLine(args)
#if AppSettings
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#endif
                    .AddEnvironmentVariables()
                    .Build();
            
            return configuration;
        }
        
        private static WebApplicationBuilder CreateWebApplicationBuilder(string[] args, IConfiguration configuration)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Configuration.AddConfiguration(configuration);
            ConfigureHost(builder, configuration);
            ConfigureServices(builder, configuration);
            
            return builder;
        }

        private static void ConfigureHost(WebApplicationBuilder builder, IConfiguration configuration)
        {
            string httpEndpointUrl = "http://+:" + configuration["ARCUS_HTTP_PORT"];
            builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false)
                           .UseUrls(httpEndpointUrl);
            
            builder.Host.ConfigureSecretStore((context, config, stores) =>
            {
//[#if DEBUG]
                stores.AddConfiguration(config);
//[#endif]
                
                //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
            });
#if Serilog
            builder.Host.UseSerilog((context, serviceProvider, config) => CreateLoggerConfiguration(context, serviceProvider, config));
#endif
#if Console
            builder.Host.ConfigureLogging(logging => logging.AddConsole());
#endif
        }
        
        private static void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration)
        {
#if CertificateAuth
            var certificateAuthenticationConfig = 
                new CertificateAuthenticationConfigBuilder()
                    .WithSubject(X509ValidationLocation.Configuration, "CertificateSubject")
                    .Build();
            
            builder.Services.AddScoped(serviceProvider => new CertificateAuthenticationValidator(certificateAuthenticationConfig));
            
#endif
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
            builder.Services.AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;
                
                RestrictToJsonContentType(options);
                ConfigureJsonFormatters(options);
                
#if SharedAccessKeyAuth
#warning Please provide a valid request header name and secret name to the shared access filter
                options.Filters.Add(new SharedAccessKeyAuthenticationFilter(headerName: SharedAccessKeyHeaderName, queryParameterName: null, secretName: "<your-secret-name>"));
#endif
#if CertificateAuth
                options.Filters.Add(new CertificateAuthenticationFilter());
#endif
#if JwtAuth
                AuthorizationPolicy policy = 
                    new AuthorizationPolicyBuilder()
                        .RequireRole("Admin")
                        .RequireAuthenticatedUser()
                        .Build();
                
                options.Filters.Add(new AuthorizeFilter(policy));
#endif
            });
#if JwtAuth
#error Use previously registered secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secrets/consume-from-key-vault
            ISecretProvider secretProvider = null;
            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                string key = secretProvider.GetRawSecretAsync("JwtSigningKey").GetAwaiter().GetResult();
                
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration.GetValue<string>("Jwt:Issuer"),
                    ValidateAudience = true,
                    ValidAudience = configuration.GetValue<string>("Jwt:Audience")
                };
            });
#endif
            builder.Services.AddHealthChecks();
#if (ExcludeCorrelation == false)
            builder.Services.AddHttpCorrelation();
#endif
#if (ExcludeOpenApi == false)
            
//[#if DEBUG]
            var openApiInformation = new OpenApiInfo
            {
                Title = "Arcus.Templates.WebApi",
                Version = "v1"
            };
            
            builder.Services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Arcus.Templates.WebApi.Open-Api.xml"));
                
                swaggerGenerationOptions.ExampleFilters();
#if (ExcludeCorrelation == false)
                swaggerGenerationOptions.OperationFilter<AddHeaderOperationFilter>("X-Transaction-Id", "Transaction ID is used to correlate multiple operation calls. A new transaction ID will be generated if not specified.", false);
                swaggerGenerationOptions.OperationFilter<AddResponseHeadersFilter>();
#endif
#if SharedAccessKeyAuth

                swaggerGenerationOptions.AddSecurityDefinition("shared-access-key", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = SharedAccessKeyHeaderName,
                    Description = "Authentication scheme based on shared access key"
                });
                swaggerGenerationOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { 
                        new OpenApiSecurityScheme
                        {
                            Description = "Globally authentication scheme based on shared access key",
                            Reference = new OpenApiReference
                            {
                                Id = "shared-access-key",
                                Type = ReferenceType.SecurityScheme
                            }
                        }, new List<string>() }
                });
#endif
#if CertificateAuth

                swaggerGenerationOptions.AddSecurityDefinition("certificate", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-ARR-ClientCert",
                    Description = "Authentication scheme based on client certificate"
                });
                swaggerGenerationOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Description = "Globally authentication scheme based on client certificate",
                            Reference = new OpenApiReference
                            {
                                Id = "certificate",
                                Type = ReferenceType.SecurityScheme
                            }
                        }, new List<string>()
                    }
                });
#endif
#if JwtAuth

                swaggerGenerationOptions.AddSecurityDefinition("jwt", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Description = "Authentication scheme based on JWT"
                });
                swaggerGenerationOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Description = "Globally authentication scheme based on JWT",
                            Reference = new OpenApiReference
                            {
                                Id = "jwt",
                                Type = ReferenceType.SecurityScheme
                            }
                        }, new List<string>()
                    }
                });
#endif
            });
            
            builder.Services.AddSwaggerExamplesFromAssemblyOf<HealthReportResponseExampleProvider>();
//[#endif]
#endif
        }
        
#if Serilog

        private static LoggerConfiguration CreateLoggerConfiguration(
            HostBuilderContext context, 
            IServiceProvider serviceProvider, 
            LoggerConfiguration config)
        {
            var instrumentationKey = context.Configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);
            
            return config
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithVersion()
                .Enrich.WithComponentName("API")
#if (ExcludeCorrelation == false)
                .Enrich.WithHttpCorrelationInfo(serviceProvider)
#endif
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsights(instrumentationKey);
        }
#endif
        
        private static void RestrictToJsonContentType(MvcOptions options)
        {
            var allButJsonInputFormatters = options.InputFormatters.Where(formatter => !(formatter is SystemTextJsonInputFormatter));
            foreach (IInputFormatter inputFormatter in allButJsonInputFormatters)
            {
                options.InputFormatters.Remove(inputFormatter);
            }
            
            // Removing for text/plain, see https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-3.0#special-case-formatters
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
        }

        private static void ConfigureJsonFormatters(MvcOptions options)
        {
            var onlyJsonInputFormatters = options.InputFormatters.OfType<SystemTextJsonInputFormatter>();
            foreach (SystemTextJsonInputFormatter inputFormatter in onlyJsonInputFormatters)
            {
                inputFormatter.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                inputFormatter.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
            
            var onlyJsonOutputFormatters = options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>();
            foreach (SystemTextJsonOutputFormatter outputFormatter in onlyJsonOutputFormatters)
            {
                outputFormatter.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                outputFormatter.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        }
        
        public static void ConfigureApp(WebApplication app)
        {
#if (ExcludeCorrelation == false)
            app.UseHttpCorrelation();
#endif
            app.UseRouting();
            app.UseRequestTracking();
            app.UseExceptionHandling();
            
#if JwtAuth
            #warning Please configure application with HTTPS transport layer security and set 'useSSL' in the Docker 'launchSettings.json' back to 'true'
            app.UseAuthentication();
#endif
#if NoneAuth
            #warning Please configure application with authentication mechanism: https://webapi.arcus-azure.net/features/security/auth/shared-access-key
#endif

#if (ExcludeOpenApi == false)
//[#if DEBUG]
            app.UseSwagger(swaggerOptions =>
            {
                swaggerOptions.RouteTemplate = "api/{documentName}/docs.json";
            });
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("/api/v1/docs.json", "Arcus.Templates.WebApi");
                swaggerUiOptions.RoutePrefix = "api/docs";
                swaggerUiOptions.DocumentTitle = "Arcus.Templates.WebApi";
            });
//[#endif]
#endif
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
