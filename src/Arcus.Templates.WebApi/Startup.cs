using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
#if Serilog
using Serilog;
#endif
#if ExcludeOpenApi
#else
using Microsoft.OpenApi.Models;
#endif
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
#if ExcludeCorrelation
#else
using Arcus.WebApi.Correlation;
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
            services.AddControllers(options => 
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
#if ExcludeCorrelation
#else
            services.AddCorrelation();
#endif

#if ExcludeOpenApi
#else
//[#if DEBUG]
            var openApiInformation = new OpenApiInfo
            {
                Title = "Arcus.Templates.WebApi",
                Version = "v1"
            };

            services.AddSwaggerGen(swaggerGenerationOptions =>
            {
                swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Arcus.Templates.WebApi.Open-Api.xml"));
            });
//[#endif]
#endif
        }

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

        private static void AddEnumAsStringRepresentation(MvcOptions options)
        {
            var onlyJsonOutputFormatters = options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>();
            foreach (SystemTextJsonOutputFormatter outputFormatter in onlyJsonOutputFormatters)
            {
                outputFormatter.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();
#if ExcludeCorrelation
#else
            app.UseCorrelation();
#endif
            app.UseRouting();

#if Serilog
            app.UseSerilogRequestLogging();
#endif

            #warning Please configure application with HTTPS transport layer security
#if NoneAuth

            #warning Please configure application with authentication mechanism: https://webapi.arcus-azure.net/features/security/auth/shared-access-key
#endif

#if ExcludeOpenApi
#else
//[#if DEBUG]
            app.UseSwagger();
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
