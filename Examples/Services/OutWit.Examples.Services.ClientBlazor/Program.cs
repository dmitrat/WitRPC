using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Interfaces;
using OutWit.Examples.Services.ClientBlazor;
using OutWit.Examples.Services.ClientBlazor.Encryption;
using OutWit.Examples.Services.ClientBlazor;

#region Default


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<EncryptorClientWeb>();

builder.Services.AddMudServices();


#endregion

await builder.Build().RunAsync();
