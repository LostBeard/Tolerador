using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.WebWorkers;
using Timer = System.Timers.Timer;

namespace Tolerador.ServiceWorkers
{
    public class BackgroundWorker : ServiceWorkerEventHandler
    {
#if DEBUG
        Timer tmr = new Timer();
#endif
        BrowserExtensionService BrowserExtensionService;
        public BackgroundWorker(BlazorJSRuntime js, BrowserExtensionService browserExtensionService) : base(js)
        {
            BrowserExtensionService = browserExtensionService;
#if DEBUG
            tmr.Elapsed += Tmr_Elapsed;
            tmr.Interval = 5000;
            tmr.Start();
#endif
        }
        List<Task>? InitWaitFor = new List<Task>();
        public void InitAsyncWaitFor(Task task)
        {
            if (InitWaitFor == null) throw new Exception("InitWaitFor is null. BackgroundWorker.OnInitializedAsync has already completed.");
        }
        private void Tmr_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // simple alive indicator for debugging
            Log($"Tick: {JS.InstanceId}");
        }
        protected override async Task OnInitializedAsync()
        {
            Log("ExtensionServiceWorker InitAsync >>");
            // little delay to let other auto-starting services run
            await Task.Delay(50);
            await Task.WhenAll(InitWaitFor!);
            InitWaitFor = null;
            Log("ExtensionServiceWorker InitAsync <<");
        }
        void Log(params object[] args)
        {
            JS.Log(new object?[] { $"ServiceWorkerEventHandler > {JS.InstanceId}" }.Concat(args).ToArray());
        }
        protected override async Task ServiceWorker_OnInstallAsync(ExtendableEvent e)
        {
            Log($"self.SkipWaiting()");
            await ServiceWorkerThis!.SkipWaiting();
        }
        protected override async Task ServiceWorker_OnActivateAsync(ExtendableEvent e)
        {
            Log($"clients.Claim()");
            using var clients = ServiceWorkerThis!.Clients;
            await clients.Claim();
        }
    }
}
