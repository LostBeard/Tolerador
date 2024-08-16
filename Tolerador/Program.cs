using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.SpeerNet;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.BlazorJS.WebWorkers;
using Tolerador;
using Tolerador.Services;
using Tolerador.ServiceWorkers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBlazorJSRuntime(out var JS);
builder.Services.AddWebWorkerService();

var extensionMode = BrowserExtensionService.GetExtensionMode();
var extensionId = BrowserExtensionService.GetExtensionId();
var isRunningAsExtension = !string.IsNullOrEmpty(extensionId);
JS.Log("Blazor loaded", JS.GlobalThisTypeName, builder.HostEnvironment.BaseAddress);
JS.Log("Extension", isRunningAsExtension, extensionMode.ToString(), extensionId);

// SpawnDev.BlazorJS[Toolbox]
builder.Services.AddSingleton<MediaDevicesService>();
// SpeerNet
builder.Services.AddSingleton<DeviceIdentityService>();
builder.Services.AddSingleton<SpeerNetService>();
builder.Services.AddSingleton<DevicePairingService>();

builder.Services.AddSingleton<BrowserExtensionService>();


// may be running in a background page (Firefox) or a background service (Chrome)
// Register is set to none because the ServiceWorker is registered via the manifest and here we are telling WebWorkerService what class to handle ServiceWorkerEvents
// GlobalScope is set to all because is Firefox the background script runs in a window, and in Chrome the background script runs in a ServiceWorker
// ExtensionServiceWorker can essentially ignore

//if (extensionMode == ExtensionMode.Background)
//{
//    // only used for extension background
//    builder.Services.RegisterServiceWorker<BackgroundWorker>(GlobalScope.All, new ServiceWorkerConfig { Register = ServiceWorkerStartupRegistration.None });
//}
//else
//{
//    // when not in an extension BackgroundWorker is added as a regular singleton (it will auto-start though due being an IAsyncBackgroundService)
//    builder.Services.AddSingleton<BackgroundWorker>();
//}
if (extensionMode == ExtensionMode.None)
{
    builder.Services.RegisterServiceWorker<BackgroundWorker>(GlobalScope.All);
}
else
{
    builder.Services.RegisterServiceWorker<BackgroundWorker>(GlobalScope.All, new ServiceWorkerConfig { Register = ServiceWorkerStartupRegistration.None });
}


// browser extension service workers are registered via the manifest.json file so set Register = None
// registering ExtensionServiceWorker here will tell Blazor to create a singleton of ExtensionServiceWorker
// and start it when running in a ServiceWorkerGlobalScope so it can handle service worker events
switch (extensionMode)
{
    case ExtensionMode.Background:
        //// may be running in a background page (Firefox) or a background service (Chrome)
        //// Register is set to none because the ServiceWorker is registered via the manifest and here we are telling WebWorkerService what class to handle ServiceWorkerEvents
        //// GlobalScope is set to all because is Firefox the background script runs in a window, and in Chrome the background script runs in a ServiceWorker
        //// ExtensionServiceWorker can essentially ignore
        //builder.Services.RegisterServiceWorker<BackgroundWorker>(GlobalScope.All, new ServiceWorkerConfig { Register = ServiceWorkerStartupRegistration.None });
        break;
    case ExtensionMode.Content:
        builder.Services.AddSingleton<ContentBridgeService>();
        break;
}
builder.Services.AddSingleton<AppService>();

// use the new id set in index.html. originally #app which could have a conflict with the page it is loaded into
builder.RootComponents.Add<App>("#spawndev-extension");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();
await host.StartBackgroundServices();

var WebWorkerService = host.Services.GetRequiredService<WebWorkerService>();
WebWorkerService.OnInstancesChanged += WebWorkerService_OnInstancesChanged;
void ListInstances()
{
    JS.Log($"ListInstances: {WebWorkerService.Instances.Count}");
    foreach (var instance in WebWorkerService.Instances)
    {
        var isThis = instance.Info.InstanceId == JS.InstanceId;
        JS.Log($"Id: {(isThis ? "[THIS]" : "")} {instance.Info.InstanceId} {instance.Info.Scope} {instance.Info.Url}");
    }
}
void WebWorkerService_OnInstancesChanged()
{
    ListInstances();
}
ListInstances();

#region DEBUG
// Fix 
// https://register.ubisoft.com/skullandbones-trial/en-US?platform=PC
// Program.cs failed: The URI 'https://register.ubisoft.com/skullandbones-trial/?platform=PC' is not contained by the base URI 'https://static-live.ubisoft.com//sitegen/PROD/FO/register/skullandbones-trial/'.    at Microsoft.AspNetCore.Components.NavigationManager.Validate(Uri , String )
JS.Set("_testWorker", Callback.Create(async () =>
{
    var thisGlobalScope = JS.GlobalThisTypeName;
    Console.WriteLine($"thisGlobalScope: {thisGlobalScope}");
    var webWorkerService = host.Services.GetRequiredService<WebWorkerService>();
    var worker = await webWorkerService.GetWebWorker();
    if (worker == null)
    {
        JS.Log("Failed to create a worker");
        return;
    }
    var workerGlobalScope = await worker.Run(() => JS.GlobalThisTypeName);
    Console.WriteLine($"workerGlobalScope: {workerGlobalScope}");
}));

#endregion
await host.BlazorJSRunAsync();