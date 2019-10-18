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
#if Auth
using Arcus.Security.Secrets.Core.Caching;
using Arcus.Security.Secrets.Core.Interfaces;
#endif
#if SharedAccessKeyAuth
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
#endif

namespace Arcus.Templates.WebApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
#if Auth
            #warning Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secrets/consume-from-key-vault
            var cachedSecretProvider = new CachedSecretProvider(secretProvider: null);
            services.AddScoped<ICachedSecretProvider>(serviceProvider => cachedSecretProvider);
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

            #warning Please configure application with HTTPS transport layer security

#if NoneAuth
            #warning Please configure application with authentication mechanism: https://webapi.arcus-azure.net/features/security/auth/shared-access-key
#endif
            app.UseMvc();

//#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", "Arcus.Templates.WebApi");
                swaggerUiOptions.DocumentTitle = "Arcus.Templates.WebApi";
            });
//#endif
        }
    }
}
