using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using System.Reflection;
using System.Text.RegularExpressions;
using Tolerador.Services;

namespace Tolerador.Layout
{
    public partial class ContentOverlay
    {
        [Parameter]
        [EditorRequired]
        public Assembly AppAssembly { get; set; }

        StorageArea SyncStorage { get; set; }

        bool HideContent { get; set; } = true;

        [Parameter]
        public IEnumerable<Assembly> AdditionalAssemblies { get; set; }

        [Inject]
        VideoExtension VideoExtension { get; set; }

        [Inject] 
        BrowserExtensionService BrowserExtensionService { get; set; }

        public Dictionary<string, object> ContentParameters { get; set; } = new Dictionary<string, object>();

        Type? ContentType { get; set; }

        DynamicComponent? dynamicComponent = null;

        bool BeenInit = false;
        public ContentOverlayRouteInfo? ContentOverlayRouteInfo { get; private set; }
        protected override void OnInitialized()
        {
            if (!BeenInit)
            {
                SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;
                BeenInit = true;
                CacheRoutes();
                ContentOverlayUpdate();
                BrowserExtensionService.OnLocationChanged += BrowserExtensionService_OnLocationChanged;

                VideoExtension.OnVideoCountChanged += VideoExtension_OnVideoCountChanged;
                VideoExtension.OnVideoStateChanged += VideoExtension_OnVideoStateChanged;
            }
        }
        private void VideoExtension_OnVideoStateChanged()
        {
            StateHasChanged();
        }
        private void VideoExtension_OnVideoCountChanged()
        {
            StateHasChanged();
        }
        protected override async Task OnInitializedAsync()
        {
            HideContent = await SyncStorage.Get<bool>($"{GetType().Name}_{nameof(HideContent)}", true);
        }
        async Task Clicked(MouseEventArgs mouseEventArgs)
        {
            HideContent = !HideContent;
            await SyncStorage.Set($"{GetType().Name}_{nameof(HideContent)}", HideContent);
        }
        private void BrowserExtensionService_OnLocationChanged(Uri obj)
        {
            ContentOverlayUpdate();
        }
        void ContentOverlayUpdate()
        {
            var routeInfo = GetBestContentComponentRoute(BrowserExtensionService.Location);
            ContentOverlayRouteInfo = routeInfo;
            var contentType = routeInfo?.ContentComponentType;
            if (ContentType != contentType)
            {
                ContentType = contentType;
                Console.WriteLine($"ContentType changed: {ContentType?.Name ?? "NONE"}");
                StateHasChanged();
            }
        }
        public List<ContentRouteInfo> ContentRouteInfos { get; } = new List<ContentRouteInfo>();
        void CacheRoutes()
        {
            var routes = new List<ContentOverlayRouteInfo>();
            var assemblies = new List<Assembly> { AppAssembly };
            if (AdditionalAssemblies != null) assemblies.AddRange(AdditionalAssemblies);
            foreach (var assembly in assemblies)
            {
                var componentTypes = assembly.ExportedTypes.Where(o => o.IsSubclassOf(typeof(ComponentBase))).ToList();
                foreach (var componentType in componentTypes)
                {
                    var attrs = componentType.GetCustomAttributes<ContentLocationAttribute>();
                    attrs = attrs.OrderByDescending(o => o.Weight).ThenByDescending(o => o.LocationRegexPattern.Length).ToList();
                    if (attrs.Count() == 0) continue;
                    var contentRouteInfo = new ContentRouteInfo
                    {
                        Assembly = assembly,
                        ComponentType = componentType,
                        ContentLocations = (List<ContentLocationAttribute>)attrs,
                    };
                    ContentRouteInfos.Add(contentRouteInfo);
                }
            }
        }
        ContentOverlayRouteInfo? GetBestContentComponentRoute(string location)
        {
            var routes = new List<ContentOverlayRouteInfo>();
            foreach (var contentRouteInfo in ContentRouteInfos)
            {
                foreach (var attr in contentRouteInfo.ContentLocations)
                {
                    var m = Regex.Match(location, attr.LocationRegexPattern);
                    if (m.Success)
                    {
                        routes.Add(new ContentOverlayRouteInfo(location, contentRouteInfo.ComponentType, m, attr));
                        break;
                    }
                }
            }
            // sort by weight first, and then pattern length; then take the first
            var ret = routes.OrderByDescending(o => o.ContentLocationAttribute.Weight).ThenByDescending(o => o.ContentLocationAttribute.LocationRegexPattern.Length).FirstOrDefault();
            return ret;
        }
        //ContentOverlayRouteInfo? GetBestContentComponentRoute(string location)
        //{
        //    var routes = new List<ContentOverlayRouteInfo>();
        //    var assemblies = new List<Assembly> { AppAssembly };
        //    if (AdditionalAssemblies != null) assemblies.AddRange(AdditionalAssemblies);
        //    foreach (var assembly in assemblies)
        //    {
        //        var componentTypes = assembly.ExportedTypes.Where(o => o.IsSubclassOf(typeof(ComponentBase))).ToList();
        //        foreach (var componentType in componentTypes)
        //        {
        //            var attrs = componentType.GetCustomAttributes<ContentLocationAttribute>();
        //            attrs = attrs.OrderByDescending(o => o.Weight).ThenByDescending(o => o.LocationRegexPattern.Length).ToList();
        //            foreach (var attr in attrs)
        //            {
        //                var locationPattern = attr.LocationRegexPattern;
        //                var m = Regex.Match(location, locationPattern);
        //                if (m.Success)
        //                {
        //                    routes.Add(new ContentOverlayRouteInfo(location, componentType, m, attr));
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    var ret = routes.OrderByDescending(o => o.ContentLocationAttribute.Weight).ThenByDescending(o => o.ContentLocationAttribute.LocationRegexPattern.Length).FirstOrDefault();
        //    return ret;
        //}
    }
}
