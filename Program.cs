using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Tewr.Blazor.FileReader;

namespace WebComicReader
{
    public static class Program
    {
        public static IJSRuntime Runtime;

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
        public static async Task<string> GetHash()
        {
            return await Runtime.InvokeAsync<string>("getHash");
        }

        public static async Task NavigateToHome(this NavigationManager Nav)
        {
            Nav.NavigateTo(await GetBaseDirectory());
        }
        public static async Task Info(string Message, params object[] Format)
        {
            await Runtime.InvokeVoidAsync("console.info", string.Format(Message, Format));
        }
        public static async Task Warning(string Message, params object[] Format)
        {
            await Runtime.InvokeVoidAsync("console.warn", string.Format(Message, Format));
        }
        public static async Task Error(string Message, params object[] Format)
        {
            await Runtime.InvokeVoidAsync("console.error", string.Format(Message, Format));
        }
        public static async Task<long> GetContentLength(string URL)
        {
            return long.Parse(await Runtime.InvokeAsync<string>("getContentLength", URL));
        }
        public static int Percentage(long Current, long Total)
        {
            return (int)Math.Round((double)(100 * Current) / Total);
        }
        public static async Task DoEvents()
        {
            await Task.Delay(10);
        }

        public static async Task<MemoryStream> ToMemoryStream(this Stream Input)
        {
            if (Input.CanSeek)
                Input.Position = 0;
            else if (Input.Position > 0)
                throw new Exception("The stream position must be in the first byte");

            if (Input is MemoryStream)
                return (MemoryStream)Input;

            var Buffer = new MemoryStream();

            try
            {
                await Input.CopyToAsync(Buffer);
            }
            catch
            {
                Input.CopyTo(Buffer);
            }

            Buffer.Position = 0;
            return Buffer;
        }
        public static async Task<MemoryStream> GetWithProgressAsync(this HttpClient Client, string Url, Action<long, long> onProgress, long Length = -1, int BufferSize = 1024 * 100)
        {
            var Request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri(Url) };
            Request.SetBrowserRequestMode(BrowserRequestMode.NoCors);
            Request.SetBrowserResponseStreamingEnabled(true);
            using var Response = await Client.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead);
            //using var Response = await Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
            Response.EnsureSuccessStatusCode();

            if (Length == -1)
                Length = await GetContentLength(Url);

            using var Input = await Response.Content.ReadAsStreamAsync();
            var Output = new MemoryStream();
            var Readed = 0;
            var Buffer = new byte[BufferSize];
            do
            {

                Readed = await Input.ReadAsync(Buffer, 0, Buffer.Length);
                Output.Write(Buffer, 0, Readed);
                onProgress?.Invoke(Output.Length, Length);
                await Task.Delay(1);
            }
            while (Readed > 0);

            Output.Position = 0;
            return Output;
        }
    }
}
