using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching.Configuration;
#if Correlation
using Arcus.WebApi.Logging.Core.Correlation;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
#if Console
using Microsoft.Extensions.Logging; 
#endif
#if Serilog_AppInsights
using Serilog;
using Serilog.Configuration;
using Serilog.Extensions.Hosting;
using Serilog.Events;
#endif
#if OpenApi
using Microsoft.OpenApi.Models;
using Arcus.Templates.WebApi.ExampleProviders;
using Swashbuckle.AspNetCore.Filters;
#endif
#if Auth
using Microsoft.AspNetCore.Mvc.Filters;
#endif
#if CertificateAuth
using Arcus.WebApi.Security.Authentication.Certificates;
#endif
#if JwtAuth
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
#endif

namespace Arcus.Templates.WebApi
{
    public class Program
    {
#if Serilog_AppInsights
        #warning Make sure that the Azure Application Insights connection string key is available as a secret.
        private const string ApplicationInsightsConnectionStringKeyName = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        
#endif
#if SharedAccessKeyAuth
        private const string SharedAccessKeyHeaderName = "X-API-Key";
        
#endif
        public static async Task<int> Main(string[] args)
        {
#if Serilog_AppInsights
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            
            try
            {
                WebApplication app = CreateWebApplication(args);
                await ConfigureSerilogAsync(app);
                await app.RunAsync();
                
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
            WebApplication app = CreateWebApplication(args);
            await app.RunAsync();
            
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
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            
            builder.Configuration.AddConfiguration(configuration);
            ConfigureServices(builder, configuration);
            ConfigureHost(builder, configuration);
            
            return builder;
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
                options.OnlyAllowJsonFormatting();
                options.ConfigureJsonFormatting(json =>
                {
                    json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    json.Converters.Add(new JsonStringEnumConverter());
                });

#if SharedAccessKeyAuth
                #warning Please provide a valid request header name and secret name to the shared access filter
                options.AddSharedAccessKeyAuthenticationFilterOnHeader(SharedAccessKeyHeaderName, "<your-secret-name>");
#endif
#if CertificateAuth
                options.AddCertificateAuthenticationFilter();
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
            builder.Services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer((jwt, serviceProvider) =>
            {
                var secretProvider = serviceProvider.GetRequiredService<ISecretProvider>();
                string key = secretProvider.GetRawSecretAsync("JwtSigningKey").GetAwaiter().GetResult();
                
                jwt.SaveToken = true;
                jwt.TokenValidationParameters = new TokenValidationParameters
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
#if Correlation
            builder.Services.AddHttpCorrelation();
#endif
#if OpenApi
            
            ConfigureOpenApi(builder);
#endif
        }
#if OpenApi
        
        private static void ConfigureOpenApi(WebApplicationBuilder builder)
        {
            #warning Be careful of exposing sensitive information with the OpenAPI document, only expose what's necessary and hide everything else.
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
#if Correlation
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
                        }, new List<string>()
                    }
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
                
                swaggerGenerationOptions.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Description = "Authentication scheme based on JWT"
                });
                swaggerGenerationOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Scheme = "bearer",
                            Description = "Globally authentication scheme based on JWT",
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        }, new List<string>()
                    }
                });
#endif
            });
            
            builder.Services.AddSwaggerExamplesFromAssemblyOf<HealthReportResponseExampleProvider>();
        }
        
#endif
        private static void ConfigureHost(WebApplicationBuilder builder, IConfiguration configuration)
        {
#if AppSettings
            string httpEndpointUrl = "http://+:" + configuration.GetValue<int>("ARCUS_HTTP_PORT"); 
#else
            string httpEndpointUrl = "http://+:" + configuration.GetValue("ARCUS_HTTP_PORT", 5000); 
#endif
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
#if Serilog_AppInsights
            builder.Host.UseSerilog(Log.Logger);
#endif
#if Console
            builder.Host.ConfigureLogging(logging => logging.AddConsole());
#endif
        }

#if Serilog_AppInsights
        
        private static async Task ConfigureSerilogAsync(WebApplication app)
        {
            var secretProvider = app.Services.GetRequiredService<ISecretProvider>();
            string connectionString = await secretProvider.GetRawSecretAsync(ApplicationInsightsConnectionStringKeyName);
            
            var reloadLogger = (ReloadableLogger) Log.Logger;
            reloadLogger.Reload(config =>
            {
                config.ReadFrom.Configuration(app.Configuration)
                      .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                      .Enrich.FromLogContext()
                      .Enrich.WithVersion()
                      .Enrich.WithComponentName("API")
#if Correlation
                      .Enrich.WithHttpCorrelationInfo(app.Services)
#endif
                      .WriteTo.Console();
            
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    config.WriteTo.AzureApplicationInsightsWithConnectionString(connectionString);
                }
                
                return config;
            });
        }
        
#endif
        private static void ConfigureApp(IApplicationBuilder app)
        {
#if Correlation
            app.UseHttpCorrelation();
#endif
            app.UseRouting();
            app.UseRequestTracking(options => options.OmittedRoutes.Add("/"));
            app.UseExceptionHandling();
            
#if JwtAuth
            #warning Please configure application with HTTPS transport layer security and set 'useSSL' in the Docker 'launchSettings.json' back to 'true'
            app.UseAuthentication();
#endif
#if NoneAuth
            #warning Please configure application with authentication mechanism: https://webapi.arcus-azure.net/features/security/auth/shared-access-key
#endif

#if OpenApi
            app.UseSwagger(swaggerOptions =>
            {
                swaggerOptions.RouteTemplate = "api/{documentName}/docs.json";
            });
//[#if DEBUG]
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
