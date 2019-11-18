using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Swagger;
#if SharedAccessKeyAuth
using Arcus.Security.Secrets.Core.Caching;
using Arcus.Security.Secrets.Core.Interfaces;
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

namespace Arcus.Templates.WebApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
#if SharedAccessKeyAuth
            #error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secrets/consume-from-key-vault
            services.AddSingleton<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(secretProvider: null));
#endif
#if CertificateAuth
            var certificateAuthenticationConfig = 
                new CertificateAuthenticationConfigBuilder()
                    .WithSubject(X509ValidationLocation.Configuration, "CertificateSubject")
                    .Build();
    
            services.AddScoped(serviceProvider => new CertificateAuthenticationValidator(certificateAuthenticationConfig));
#endif
            
            services.AddMvc(options => 
            {
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;
                
                RestrictToJsonContentType(options);
                AddEnumAsStringRepresentation(options);

#if SharedAccessKeyAuth
                #warning Please provide a valid request header name and secret name to the shared access filter
                options.Filters.Add(new SharedAccessKeyAuthenticationFilter("YOUR REQUEST HEADER NAME", "YOUR SECRET NAME"));
#endif
#if CertificateAuth
                options.Filters.Add(new CertificateAuthenticationFilter());
#endif
            });

            services.AddHealthChecks();
            
//#if DEBUG
            var openApiInformation = new Info
            {
                Title = "Arcus.Templates.WebApi",
                Version = "v1"
            };

            services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Arcus.Templates.WebApi.Open-Api.xml"));
            });
//#endif
        }

        private static void RestrictToJsonContentType(MvcOptions options)
        {
            var allButJsonInputFormatters = options.InputFormatters.Where(formatter => !(formatter is JsonInputFormatter));
            foreach (IInputFormatter inputFormatter in allButJsonInputFormatters)
            {
                options.InputFormatters.Remove(inputFormatter);
            }

            // Removing for text/plain, see https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-3.0#special-case-formatters
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
        }

        private static void AddEnumAsStringRepresentation(MvcOptions options)
        {
            var onlyJsonOutputFormatters = options.OutputFormatters.OfType<JsonOutputFormatter>();
            foreach (JsonOutputFormatter outputFormatter in onlyJsonOutputFormatters)
            {
                outputFormatter.PublicSerializerSettings.Converters.Add(new StringEnumConverter());
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();
#if CertificateAuth
            app.Use(LoadClientCertificateFromHeader);
#endif

            #warning Please configure application with HTTPS transport layer security

#if NoneAuth
            #warning Please configure application with authentication mechanism: https://webapi.arcus-azure.net/features/security/auth/shared-access-key
#endif
            app.UseMvc();

//[#if DEBUG]
            app.UseSwagger();
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", "Arcus.Templates.WebApi");
                swaggerUiOptions.DocumentTitle = "Arcus.Templates.WebApi";
            });
//[#endif]
        }
#if CertificateAuth
        // Remove this middleware method when the web API authentication NuGet package gets updated and the client certificate gets loaded in the CertificateAuthenticationFilter.
        // For more information when to remove this, see issue: https://github.com/arcus-azure/arcus.templates/issues/58.
        private static Task LoadClientCertificateFromHeader(HttpContext context, Func<Task> next)
        {
            if (context.Connection.ClientCertificate is null)
            {
                const string headerName = "X-ARR-ClientCert";

                try
                {
                    if (context.Request.Headers.TryGetValue(headerName, out StringValues headerValue))
                    {
                        byte[] rawData = Convert.FromBase64String(headerValue);
                        context.Connection.ClientCertificate = new X509Certificate2(rawData);
                    }
                }
                catch (Exception exception)
                {
                    ILogger logger = GetLoggerOrDefault(context.RequestServices);
                    logger.LogError(exception, "Cannot load client certificate from {headerName} header", headerName);
                }
            }

            return next.Invoke();
        }

        private static ILogger GetLoggerOrDefault(IServiceProvider services)
        {
            ILogger logger = 
                services.GetService<ILoggerFactory>()
                        ?.CreateLogger<Startup>();

            if (logger != null)
            {
                return logger;
            }

            return NullLogger.Instance;
        }
#endif
    }
}
