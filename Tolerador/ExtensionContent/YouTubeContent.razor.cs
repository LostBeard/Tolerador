using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.JSObjects;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using Tolerador.WebSiteExtensions;

namespace Tolerador.ExtensionContent
{
    public partial class YouTubeContent : IDisposable
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
            Console.WriteLine("YouTubeContent.OnInitialized");
            // video-ads
            // ytp-ad-preview-container
            // "ytp-ad-skip-button-modern ytp-button"

            // <span class="ytp-ad-preview-container ytp-ad-preview-container-detached modern-countdown-next-to-thumbnail" style=""><div class="ytp-ad-text ytp-ad-preview-text-modern" id="ad-text:3" style="">2</div><span class="ytp-ad-preview-image-modern"><img class="ytp-ad-image" id="ad-image:4" src="https://i.ytimg.com/vi/YIUJyJEZ-10/mqdefault_live.jpg" alt="" style=""></span></span>

            SyncStorage = BrowserExtensionService.Chrome!.Storage!.Sync;

            VideoWebSiteExtension = new VideoWebSiteExtension(JS, BrowserExtensionService);
            VideoWebSiteExtension.OnWatchedNodesUpdated += VideoWebSiteExtension_OnWatchedNodesUpdated;
            VideoWebSiteExtension.WatchNodes = new List<WatchNode>
                {
                    // ytp-skip-ad-button
                    new WatchNode("skipAd", "button.ytp-skip-ad-button", checkVisibility: true),
                    new WatchNode("skipAd2", "button.ytp-ad-skip-button-modern", checkVisibility: true),
                    // 
                    new WatchNode("video", "video", checkVisibility: true),
                    new WatchNode("play", "button.ytp-play-button", checkVisibility: true),
                    new WatchNode("pause", "button.ytp-pause-button", checkVisibility: true),
                    new WatchNode("ad-indicator", ".ytp-ad-preview-container", checkVisibility: true),
                    new WatchNode("mute", "button.ytp-mute-button", checkVisibility: true),
                    //new WatchNode("mute-on", "button.mute-btn--on"),
                    //new WatchNode("mute-off", "button.mute-btn--off"),
                    //new WatchNode("fullscreen", "button.fullscreen-icon"),
                    //new WatchNode("exit-fullscreen", "button.exit-fullscreen-icon"),
                    //new WatchNode("restart", "button.play-from-start-icon"),
                };

            VideoWebSiteExtension.MuteAds = await SyncStorage.Get<bool>($"{nameof(YouTubeContent)}_MuteAds", true);
            VideoWebSiteExtension.SkipAds = await SyncStorage.Get<bool>($"{nameof(YouTubeContent)}_SkipAds", true);
        }
        async Task MuteAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.MuteAds = index == 1;
            await SyncStorage.Set($"{nameof(YouTubeContent)}_MuteAds", VideoWebSiteExtension.MuteAds);
            Console.WriteLine($"VideoWebSiteExtension.MuteAds: {VideoWebSiteExtension.MuteAds}");
        }
        async Task SkipAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.SkipAds = index == 1;
            await SyncStorage.Set($"{nameof(YouTubeContent)}_SkipAds", VideoWebSiteExtension.SkipAds);
            Console.WriteLine($"DisneyPlusExtension.SkipAds: {VideoWebSiteExtension.SkipAds}");
        }

        public void Dispose()
        {
            Console.WriteLine("YouTubeContent.Dispose");
            if (VideoWebSiteExtension != null)
            {

                VideoWebSiteExtension.Dispose();
            }
        }

        private void VideoWebSiteExtension_OnWatchedNodesUpdated(List<string> changedWatchNodes)
        {
            if (changedWatchNodes.Count == 0) return;
            var videoFound = VideoWebSiteExtension!.Found("video");
            if (!videoFound)
            {
                return;
            }
            var skipAd = VideoWebSiteExtension.Found("skipAd");
            var skipAd2 = VideoWebSiteExtension.Found("skipAd2");
            var ad = VideoWebSiteExtension.Found("ad-indicator") || skipAd || skipAd2;
            var muted = VideoWebSiteExtension.Muted;
            var playing = VideoWebSiteExtension.Playing;
            if (playing && ad)
            {
                if (VideoWebSiteExtension.SkipAds)
                {
                    Console.WriteLine("- Skipping ad");
                    if (skipAd) VideoWebSiteExtension.ClickWatchedNodeButton("skipAd");
                    else if (skipAd2) VideoWebSiteExtension.ClickWatchedNodeButton("skipAd2");
                }
            }
            if (ad && !muted)
            {
                if (VideoWebSiteExtension.MuteAds)
                {
                    Console.WriteLine("- Setting muted");
                    VideoWebSiteExtension.Muted = true;
                }
            }
            else if (!ad && muted)
            {
                if (VideoWebSiteExtension.MuteAds)
                {
                    Console.WriteLine("- Setting unmuted");
                    VideoWebSiteExtension.Muted = false;
                }
            }
        }
    }
}
