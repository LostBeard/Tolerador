using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;
using Timer = System.Timers.Timer;

namespace Tolerador.ServiceWorkers
{
    public class ExtensionServiceWorker : ServiceWorkerEventHandler
    {
#if DEBUG
        Timer tmr = new Timer();
#endif
        public ExtensionServiceWorker(BlazorJSRuntime js) : base(js)
        {            
#if DEBUG
            tmr.Elapsed += Tmr_Elapsed;
            tmr.Interval = 5000;
            tmr.Start();
#endif
        }
        private void Tmr_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // simple alive indicator for debugging
            Log($"Tick: {InstanceId}");
        }
        void Log(params object[] args)
        {
            JS.Log(new object?[] { $"ServiceWorkerEventHandler > {InstanceId}" }.Concat(args).ToArray());
        }
    }
}
