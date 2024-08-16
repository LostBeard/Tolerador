using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.WebWorkers;
using System.Text.RegularExpressions;

namespace Tolerador.ServiceWorkers
{
    public class BackgroundWorker : ServiceWorkerEventHandler
    {
        BrowserExtensionService BrowserExtensionService;
        List<Task>? InitWaitFor = new List<Task>();

        DeclarativeNetRequest? DeclarativeNetRequest => BrowserExtensionService.Browser?.DeclarativeNetRequest;
        public BackgroundWorker(BlazorJSRuntime js, BrowserExtensionService browserExtensionService) : base(js)
        {
            BrowserExtensionService = browserExtensionService;
            if (BrowserExtensionService.ExtensionMode == ExtensionMode.Background)
            {
                BrowserExtensionService.Browser!.Runtime!.OnMessage += Runtime_OnMessage;
            }
        }
        public void InitAsyncWaitFor(Task task)
        {
            if (InitWaitFor == null) throw new Exception("InitWaitFor is null. BackgroundWorker.OnInitializedAsync has already completed.");
            InitWaitFor.Add(task);
        }
        protected override async Task OnInitializedAsync()
        {
            Log("ExtensionServiceWorker InitAsync >>");
            // little delay to let other auto-starting services run
            if (BrowserExtensionService.ExtensionMode == ExtensionMode.Background)
            {
                var rules = await DeclarativeNetRequest!.GetDynamicRules();
                JS.Log("Rules::", rules);
            }
            await Task.Delay(50);
            await Task.WhenAll(InitWaitFor!);
            InitWaitFor = null;
            Log("ExtensionServiceWorker InitAsync <<");
        }
        bool Runtime_OnMessage(JSObject data, MessageSender sender, Function? sendResponse)
        {
            JS.Log("bg...Runtime_OnMessage *****");
            var cspViolation = data.JSRef!.Get<CSPViolation?>("cspViolation");
            if (cspViolation != null)
            {
                _ = PatchCSP(cspViolation, sender);
            }
            return false;
        }
        async Task PatchCSP(CSPViolation cspViolation, MessageSender sender)
        {
            var originalPolicy = cspViolation.OriginalPolicy;
            var updatedPolicy = originalPolicy;
            if (originalPolicy.IndexOf("wasm-unsafe-eval") == -1)
            {
                updatedPolicy = originalPolicy.Replace("script-src ", "script-src 'wasm-unsafe-eval' ");
            }
            else
            {
                // rule already has 'wasm-unsafe-eval'
                // if this happens, there is another problem
                return;
            }
            var url = new Uri(cspViolation.DocumentURI);
            // separate paths on the same domain MAY (uncommon) have different csp rules
            // the query string and hash shouldn't have any effect on csp rules
            // check if the extension actualyl wants to patch this csp or not
            // could check user settings.
            var shouldPatchCSP = await ShouldPatchCSPCheck(url);
            if (!shouldPatchCSP)
            {
                // ignore the csp issue
                return;
            }
            var pageUrl = url.GetLeftPart(UriPartial.Path);
            var pageUrlEscaped = Regex.Escape(pageUrl);
            var ruleId = await GetFreeSessionRuleId();
            var cspRule = new Rule
            {
                Id = ruleId,
                Action = new RuleAction
                {
                    Type = RuleActionType.ModifyHeaders,
                    ResponseHeaders = new ModifyHeaderInfo[] 
                    {
                        new ModifyHeaderInfo 
                        {
                            Header = "content-security-policy",
                            Operation = HeaderOperation.Set,
                            Value = updatedPolicy,
                        }
                    },
                },
                Condition = new RuleCondition
                {
                    RegexFilter = $@"^{pageUrlEscaped}(\?.*)?(#.*)?$",
                    ResourceTypes = new EnumString<ResourceType>[] { ResourceType.MainFrame, ResourceType.SubFrame, ResourceType.XMLHttpRequest },
                }
            };
            JS.Log("Adding rule", pageUrl, cspRule);
            // save rule
            await DeclarativeNetRequest!.UpdateSessionRules(new UpdateRuleOptions
            {
                AddRules = new Rule[] { cspRule },
            });
            // reload tab so the new rule can take effect
            await BrowserExtensionService.Browser!.Tabs!.Reload(sender.Tab!.Id);
        }
        async Task<bool> ShouldPatchCSPCheck(Uri url)
        {
            return true;
        }
        async Task<int> GetFreeSessionRuleId()
        {
            using var rules = await DeclarativeNetRequest!.GetSessionRules();
            var ruleIds = rules.ToArray().Select(o => o.Id).ToArray();
            var id = Random.Shared.Next(0, int.MaxValue);
            while (ruleIds.Contains(id))
            {
                id = Random.Shared.Next(0, int.MaxValue);
            }
            return id;
        }
        protected override async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
        {
            Response ret;
            try
            {
                ret = await JS.Fetch(e.Request);
            }
            catch
            {
                ret = Response.Error();
            }
            return ret;
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
