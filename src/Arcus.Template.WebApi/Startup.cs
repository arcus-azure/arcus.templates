using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
#if Auth
using Arcus.Security.Secrets.Core.Caching;
using Arcus.Security.Secrets.Core.Interfaces;
#endif
#if SharedAccessKeyAuth
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
#endif

namespace Arcus.Template.WebApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
#if Auth
            #warning "Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secrets/consume-from-key-vault"
            services.AddScoped<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(secretProvider: null));
#endif

#if SharedAccessKeyAuth
            #warning "Please provide a valid request header name and secret name to the shared access filter"
            services.AddMvc(options => options.Filters.Add(new SharedAccessKeyAuthenticationFilter("YOUR REQUEST HEADER NAME", "YOUR SECRET NAME")));
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();

            #warning "Please configure application with HTTPS transport layer security"

#if NoneAuth
            #warning "Please configure application with authentication mechanism: https://webapi.arcus-azure.net/features/security/auth/shared-access-key"
#endif
            app.UseMvc();
        }
    }
}
