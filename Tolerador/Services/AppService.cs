using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.SpeerNet;
using Tolerador.ServiceWorkers;

namespace Tolerador.Services
{
    public class AppService : IAsyncBackgroundService
    {
        /// <summary>
        /// Returns when the service is Ready
        /// </summary>
        public Task Ready => _Ready ??= InitAsync();
        private Task? _Ready = null;
        BlazorJSRuntime JS;
        BrowserExtensionService BrowserExtensionService;
        BackgroundWorker BackgroundWorker;
        SpeerNetService SpeerNetService;
        public AppService(BlazorJSRuntime js, BrowserExtensionService browserExtensionService, BackgroundWorker backgroundWorker, SpeerNetService speerNetService)
        {
            JS = js;
            BrowserExtensionService = browserExtensionService;
            BackgroundWorker = backgroundWorker;
            SpeerNetService = speerNetService;
            JS.Log("AppService()");
        }
        async Task InitAsync()
        {
            await SpeerNetService.Ready;
            await BackgroundWorker.Ready;
            JS.Log("AppService.InitAsync()");
            switch (BrowserExtensionService.ExtensionMode)
            {
                case ExtensionMode.Content:
                    InitContentMode();
                    break;
                case ExtensionMode.Background:
                    InitSpeerNetService();
                    await InitBackgroundMode();
                    break;
                case ExtensionMode.None:
                    // probably running in a website
                    if (JS.IsServiceWorkerGlobalScope)
                    {
                        InitSpeerNetService();
                    }
                    break;
                case ExtensionMode.Popup:
                case ExtensionMode.Options:
                case ExtensionMode.Installed:
                    // WebWorkerService.Instances can be used to communicate with the BackgroundWorker instance
                    // In Chrome, calling runtime.connect() from the Options/Popup/Installed window instance never fired on the background worker.
                    // Worked in Firefox
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// This will be called when any of below are true:
        /// - Running in an extension background page (Firefox) or background ServiceWorker (Chrome). (ExtensionMode.Background)
        /// - Not running in an extension and running in a ServiceWorker
        /// </summary>
        void InitSpeerNetService()
        {
            // load 1 or more nodes
#if DEBUG && false
            SpeerNetService.AddSpeerNetNode("ws://localhost:9000/_sio/");
#else
            SpeerNetService.AddSpeerNetNode("wss://pi.spawndev.com:44365/_sio/");
#endif
        }
        Port? BackgroundPort { get; set; }
        List<Port> ContentPorts = new List<Port>();
        void InitContentMode()
        {
            var runtime = BrowserExtensionService.Runtime;
            JS.Log("InitContentMode", runtime);
            if (runtime == null) return;
            runtime.OnMessage += BackgroundWorker_OnMessage;
            BackgroundPort = runtime.Connect(new ConnectInfo { Name = "content-port" });
            BackgroundPort.OnMessage += BackgroundPort_OnMessage;
            BackgroundPort.OnDisconnect += BackgroundPort_OnDisconnect;
            BackgroundPort.PostMessage("Hello background from content!");
        }
        bool BackgroundWorker_OnMessage(JSObject message, MessageSender sender, Function? callback)
        {
            var responseRequested = callback != null;
            JS.Log("BackgroundWorker_OnMessage", message, sender, callback);
            JS.Log("responseRequested:", responseRequested);
            return false;
        }
        async Task InitBackgroundMode()
        {
            var runtime = BrowserExtensionService.Runtime;
            JS.Log("InitBackgroundMode1", runtime);
            if (runtime == null) return;
            runtime.OnConnect += Runtime_OnConnect;
            using var tabs = BrowserExtensionService.Browser!.Tabs!;
            var allTabs = await tabs.Query(new TabQueryInfo { });
            JS.Log("allTabs1", allTabs);
            JS.Set("allTabs1", allTabs);
            var succ = 0;
            var fail = 0;
            foreach (var tab in allTabs)
            {
                try
                {
                    await tabs.SendMessage(tab.Id, "ping");
                    succ++;
                }
                catch
                {
                    fail++;
                }
            }
            JS.Log("ping:", fail, succ);
        }
        /// <summary>
        /// This event handler is fired when an extension context calls connect<br/>
        /// We are listening for Blazor in extension content context
        /// </summary>
        /// <param name="port"></param>
        void Runtime_OnConnect(Port port)
        {
            JS.Log("Runtime_OnConnect", port);
            switch (port.Name)
            {
                case "content-port":
                    ContentPorts.Add(port);
                    port.OnMessage += ContentPort_OnMessage;
                    port.OnDisconnect += ContentPort_OnDisconnect;
                    port.PostMessage("Hello content!");
                    break;
            }
        }
        void ContentPort_OnMessage(JSObject message)
        {
            JS.Log("ContentPort_OnMessage", message);
        }
        void ContentPort_OnDisconnect(Port port)
        {
            JS.Log("ContentPort_OnDisconnect", port);
        }
        void BackgroundPort_OnMessage(JSObject message)
        {
            JS.Log("BackgroundPort_OnMessage", message);
        }
        void BackgroundPort_OnDisconnect(Port port)
        {
            JS.Log("BackgroundPort_OnDisconnect", port);
        }
    }
}
