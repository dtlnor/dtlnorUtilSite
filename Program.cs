namespace dtlnorUtilSite
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Toolbelt.Blazor.Extensions.DependencyInjection;
    using System.Globalization;
    using Microsoft.JSInterop;
    using Blazored.LocalStorage;

    public class Program
    {

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services
                .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                .AddI18nText(options =>
                {
                    // options.PersistanceLevel = PersistanceLevel.Session;
                })
                .AddBlazoredLocalStorage();

            await builder.Build().RunAsync();
        }
    }
}
