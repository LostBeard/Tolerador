using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using Tolerador.WebSiteExtensions;

namespace Tolerador.ExtensionContent
{
    public partial class NetflixContent : IDisposable
    {

        [Inject]
        BlazorJSRuntime JS { get; set; }

        [Inject]
        BrowserExtensionService BrowserExtensionService { get; set; }

        VideoWebSiteExtension? VideoWebSiteExtension = null;

        StorageArea SyncStorage { get; set; }

        int MuteToggleValue => VideoWebSiteExtension!.MuteAds ? 1 : 0;

        int SkipAdsToggleValue => VideoWebSiteExtension!.SkipAds ? 1 : 0;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine($"{GetType().Name}.OnInitialized");
            SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;

            VideoWebSiteExtension = new VideoWebSiteExtension(JS, BrowserExtensionService);
            VideoWebSiteExtension.OnWatchedNodesUpdated += VideoWebSiteExtension_OnWatchedNodesUpdated;
            VideoWebSiteExtension.WatchNodes = new List<WatchNode>
                {
                    // netflix.com/watch
                    new WatchNode("ad-indicator", ".watch-video--adsInfo-container"),
                    new WatchNode("video", "video"),
                    new WatchNode("play", "button[data-uia=\"control-play-pause-play\"]"),
                    new WatchNode("fullscreen", "button[data-uia=\"control-fullscreen-enter\"]"),
                    new WatchNode("mute", "button[data-uia*=\"control-volume-\"]"),

                    // mute buttons (depending on volume)
                    // button[data-uia="control-volume-off"]
                    // button[data-uia="control-volume-low"]
                    // button[data-uia="control-volume-medium"]
                    // button[data-uia="control-volume-high"]

                    //new WatchNode("skipAd", "", checkVisibility: true),
                };

            VideoWebSiteExtension.MuteAds = await SyncStorage.Get<bool>($"{GetType().Name}_MuteAds", true);
            VideoWebSiteExtension.SkipAds = await SyncStorage.Get<bool>($"{GetType().Name}_SkipAds", true);
        }
        async Task MuteAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.MuteAds = index == 1;
            await SyncStorage.Set($"{GetType().Name}_MuteAds", VideoWebSiteExtension.MuteAds);
            Console.WriteLine($"{GetType().Name}_MuteAds: {VideoWebSiteExtension.MuteAds}");
        }
        async Task SkipAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.SkipAds = index == 1;
            await SyncStorage.Set($"{GetType().Name}_SkipAds", VideoWebSiteExtension.SkipAds);
            Console.WriteLine($"{GetType().Name}_SkipAds: {VideoWebSiteExtension.SkipAds}");
        }

        public void Dispose()
        {
            Console.WriteLine($"{GetType().Name}.Dispose");
            if (VideoWebSiteExtension != null)
            {

                VideoWebSiteExtension.Dispose();
            }
        }
        //        public bool Muted
        //        {
        //            get
        //            {
        //                using var video = VideoWebSiteExtension?.GetWatchNodeEl<HTMLVideoElement>("video");
        //                return video != null && (video.Muted || video.Volume == 0d);
        //            }
        //            set
        //            {
        //#if DEBUG
        //                Console.WriteLine($"Muted being set: {value}");
        //#endif
        //                using var video = VideoWebSiteExtension?.GetWatchNodeEl<HTMLVideoElement>("video");
        //                if (video != null) video.Volume = value ? 0 : 1;
        //            }
        //        }
        double adVolume = 0;
        double videoVolume = 1;
        double variance = 0.05d;
        private void VideoWebSiteExtension_OnWatchedNodesUpdated(List<string> changedWatchNodes)
        {
            if (changedWatchNodes.Count == 0) return;
            var videoFound = VideoWebSiteExtension!.Found("video");
            if (!videoFound)
            {
                Console.WriteLine("video not found... ignoring other changes");
                return;
            }
            var playing = VideoWebSiteExtension.Playing;
            var skipAd = VideoWebSiteExtension.Found("skipAd");
            var ad = VideoWebSiteExtension.Found("ad-indicator") || skipAd;
            var volume = VideoWebSiteExtension.Volume;

            var atAdVolume = Math.Abs(volume - adVolume) < variance;

            JS.Log(new
            {
                videoFound = videoFound,
                playing = playing,
                ad = ad,
                skipAd = skipAd,
                volume = volume,
            });
            var requirePlaying = false;
            if (requirePlaying && !playing)
            {
                return;
            }

            if (ad && skipAd)
            {
                Console.WriteLine("- Skipping ad ??");
                if (VideoWebSiteExtension.SkipAds)
                {
                    Console.WriteLine("- Skipping ad");
                    VideoWebSiteExtension.SkipAd();
                }
            }
            if (ad && !atAdVolume)
            {
                Console.WriteLine("- Setting muted ??");
                if (VideoWebSiteExtension.MuteAds)
                {
                    Console.WriteLine("- Setting muted");
                    VideoWebSiteExtension.Volume = adVolume;
                    videoVolume = volume;
                }
            }
            if (!ad && atAdVolume)
            {
                Console.WriteLine("- Setting unmuted??");
                if (VideoWebSiteExtension.MuteAds)
                {
                    Console.WriteLine("- Setting unmuted");
                    VideoWebSiteExtension.Volume = videoVolume;
                }
            }
        }
    }
}
