using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.WebWorkers;
using Tolerador;
using Tolerador.ServiceWorkers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBlazorJSRuntime();
builder.Services.AddWebWorkerService();

var JS = BlazorJSRuntime.JS;

var extensionMode = BrowserExtensionService.GetExtensionMode();
var extensionId = BrowserExtensionService.GetExtensionId();
var isRunningAsExtension = !string.IsNullOrEmpty(extensionId);
JS.Log("Blazor loaded", JS.GlobalThisTypeName, builder.HostEnvironment.BaseAddress);
JS.Log("Extension", isRunningAsExtension, extensionMode.ToString(), extensionId);

builder.Services.AddSingleton<BrowserExtensionService>();
// browser extension service workers are registered via the manifest.json file so set Register = None
// registering ExtensionServiceWorker here will tell Blazor to create a singleton of ExtensionServiceWorker
// and start it when running in a ServiceWorkerGlobalScope so it can handle service worker events
switch (extensionMode)
{
    case ExtensionMode.Background:
        // may be running in a background page (Firefox) or a background service (Chrome)
        builder.Services.RegisterServiceWorker<ExtensionServiceWorker>(new ServiceWorkerConfig { Register = ServiceWorkerStartupRegistration.None });
        break;
    case ExtensionMode.Content:
        builder.Services.AddSingleton<ContentBridgeService>();
        break;
}

// use the new id set is index.html. originally #app which could conflict
builder.RootComponents.Add<App>("#spawndev-extension");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();
await host.StartBackgroundServices();

#region DEBUG
// Fix 
// https://register.ubisoft.com/skullandbones-trial/en-US?platform=PC
// Program.cs failed: The URI 'https://register.ubisoft.com/skullandbones-trial/?platform=PC' is not contained by the base URI 'https://static-live.ubisoft.com//sitegen/PROD/FO/register/skullandbones-trial/'.    at Microsoft.AspNetCore.Components.NavigationManager.Validate(Uri , String )
JS.Set("_testWorker", new ActionCallback(async () =>
{
    var thisGlobalScope = JS.GlobalThisTypeName;
    Console.WriteLine($"thisGlobalScope: {thisGlobalScope}");
    var webWorkerService = host.Services.GetRequiredService<WebWorkerService>();
    var worker = await webWorkerService.GetWebWorker();
    var workerGlobalScope = await worker.Run(() => JS.GlobalThisTypeName);
    Console.WriteLine($"workerGlobalScope: {workerGlobalScope}");
}));

#endregion
await host.BlazorJSRunAsync();