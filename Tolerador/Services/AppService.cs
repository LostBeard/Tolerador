using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.JSObjects;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
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
        public AppService(BlazorJSRuntime js, BrowserExtensionService browserExtensionService, BackgroundWorker backgroundWorker)
        {
            JS = js;
            BrowserExtensionService = browserExtensionService;
            BackgroundWorker = backgroundWorker;
            JS.Log("AppService()");
        }
        async Task InitAsync()
        {
            JS.Log("AppService.InitAsync()");
            switch (BrowserExtensionService.ExtensionMode)
            {
                case ExtensionMode.Content:
                    await InitContentMode();
                    break;
                case ExtensionMode.Background:
                    await InitBackgroundMode();
                    break;
                case ExtensionMode.None:
                    // probably running in a website
                    break;
                case ExtensionMode.Popup:
                case ExtensionMode.Options:
                case ExtensionMode.Installed:
                    // WebWorkerService.Instances can be used to communicate with the BackgroundWorker instance
                    // In Chrome, calling connect from the Options window instance never fired on the background worker.
                    // Worked in Firefox
                    //await InitExtensionMode();
                    break;
                default:
                    break;
            }
        }
        Port? BackgroundPort { get; set; }
        List<Port> ContentPorts = new List<Port>();
        //List<Port> ExtensionPorts = new List<Port>();
        //async Task InitExtensionMode()
        //{
        //    var runtime = BrowserExtensionService.BrowserRuntime;
        //    JS.Log("InitExtensionMode", runtime);
        //    if (runtime == null) return;
        //    BackgroundPort = runtime.Connect(new ConnectInfo { Name = "extension-port" });
        //    BackgroundPort.OnMessage += BackgroundPort_OnMessage;
        //    BackgroundPort.OnDisconnect += BackgroundPort_OnDisconnect;
        //    BackgroundPort.PostMessage("Hello background from extension!");
        //}
        async Task InitContentMode()
        {
            var runtime = BrowserExtensionService.ChromeRuntime;
            JS.Log("InitContentMode", runtime);
            if (runtime == null) return;
            BackgroundPort = runtime.Connect(new ConnectInfo { Name = "content-port" });
            BackgroundPort.OnMessage += BackgroundPort_OnMessage;
            BackgroundPort.OnDisconnect += BackgroundPort_OnDisconnect;
            BackgroundPort.PostMessage("Hello background from content!");
            runtime.OnMessage += BackgroundWorker_OnMessage;
        }
        void BackgroundWorker_OnMessage(JSObject message, MessageSender sender, Function? callback)
        {
            JS.Log("BackgroundWorker_OnMessage", message, sender, callback);
        }
        async Task InitBackgroundMode()
        {
            var runtime = BrowserExtensionService.ChromeRuntime;
            JS.Log("InitBackgroundMode1", runtime);
            if (runtime == null) return;
            runtime.OnConnect += Runtime_OnConnect;
            using var tabs = BrowserExtensionService.Chrome!.Tabs!;
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
                case "extension-port":
                    //ExtensionPorts.Add(port);
                    //port.OnMessage += ExtensionPort_OnMessage;
                    //port.OnDisconnect += ExtensionPort_OnDisconnect;
                    //port.PostMessage("Hello extension!");
                    break;
            }
        }
        void ExtensionPort_OnMessage(JSObject message)
        {
            JS.Log("ExtensionPort_OnMessage", message);
        }
        void ExtensionPort_OnDisconnect(Port port)
        {
            JS.Log("ExtensionPort_OnDisconnect", port);
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
