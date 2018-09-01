using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTest
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<AppModel>();
        }

        public void Configure(IBlazorApplicationBuilder app)
        {
            app.UseNObservable();
            app.AddComponent<App>("app");
        }
    }
}
