using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Tewr.Blazor.FileReader;

namespace WebComicReader
{
    public static class Program
    {
        public static IJSRuntime Runtime;
        public static ILogger Logger;

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddFileReaderService(opt => opt.UseWasmSharedBuffer = true);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
        public static async Task<string> GetBaseDirectory()
        {
            return await Runtime.InvokeAsync<string>("getBaseDirectory");
        }

        public static async Task NavigateToHome(this NavigationManager Nav)
        {
            Nav.NavigateTo(await GetBaseDirectory());
        }
    }
}
