namespace dtlnorUtilSite
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Blazored.LocalStorage;
    using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.JSInterop;
    using Toolbelt.Blazor.Extensions.DependencyInjection;

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
