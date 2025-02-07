using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Tolerador.WebSiteExtensions;

namespace Tolerador.ExtensionContent
{
    public partial class YouTubeContent : IDisposable
    {
        [Inject]
        BlazorJSRuntime JS { get; set; } = default!;

        [Inject]
        BrowserExtensionService BrowserExtensionService { get; set; } = default!;

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

            SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;

            VideoWebSiteExtension = new VideoWebSiteExtension(JS, BrowserExtensionService);
            VideoWebSiteExtension.OnWatchedNodesUpdated += VideoWebSiteExtension_OnWatchedNodesUpdated;
            VideoWebSiteExtension.WatchNodes = new List<WatchNode>
            {
                new WatchNode("video", "ytd-player video", checkVisibility: true),
                new WatchNode("skipAd", "ytd-player .video-ads button.ytp-skip-ad-button", checkVisibility: true),
                new WatchNode("skipAd2", "ytd-player .video-ads button.ytp-ad-skip-button-modern", checkVisibility: true),
                new WatchNode("ad-indicator", "ytd-player .video-ads button", checkVisibility: true),
                new WatchNode("play", "ytd-player button.ytp-play-button[data-title-no-tooltip=\"Play\"]"),
                new WatchNode("pause", "ytd-player button.ytp-play-button[data-title-no-tooltip=\"Pause\"]"),
                new WatchNode("unmute-button", "ytd-player button.ytp-mute-button[data-title-no-tooltip=\"Unmute\"]"),
                new WatchNode("mute-button", "ytd-player button.ytp-mute-button[data-title-no-tooltip=\"Mute\"]"),
                new WatchNode("mute", "ytd-player button.ytp-mute-button"),
                //new WatchNode("restart", "button.play-from-start-icon"),
                //new WatchNode("mute", "button.mute-btn"),
                //new WatchNode("mute-on", "button.mute-btn--on"),
                //new WatchNode("mute-off", "button.mute-btn--off"),
                //new WatchNode("video", "disney-web-player video", true){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                //new WatchNode("fullscreen", "toggle-fullscreen-button info-tooltip button.fullscreen-icon"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                //new WatchNode("exit-fullscreen", "toggle-fullscreen-button info-tooltip button.exit-fullscreen-icon"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                //new WatchNode("play", "toggle-play-pause info-tooltip button.play-button"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                //new WatchNode("pause", "toggle-play-pause info-tooltip button.pause-button"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                //new WatchNode("ad-indicator", ".overlay_interstitials"),
            };

            VideoWebSiteExtension.MuteAds = await SyncStorage.Get<bool>($"{nameof(YouTubeContent)}_MuteAds", true);
            VideoWebSiteExtension.SkipAds = await SyncStorage.Get<bool>($"{nameof(YouTubeContent)}_SkipAds", true);
            if (VideoWebSiteExtension.LocationSupported)
            {
                VideoWebSiteExtension.WatchedNodesUpdate();
            }
        }
        async Task MuteAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.MuteAds = index == 1;
            await SyncStorage.Set($"{nameof(YouTubeContent)}_MuteAds", VideoWebSiteExtension.MuteAds);
            Console.WriteLine($"{nameof(YouTubeContent)}_MuteAds: {VideoWebSiteExtension.MuteAds}");
        }
        async Task SkipAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.SkipAds = index == 1;
            await SyncStorage.Set($"{nameof(YouTubeContent)}_SkipAds", VideoWebSiteExtension.SkipAds);
            Console.WriteLine($"{nameof(YouTubeContent)}_SkipAds: {VideoWebSiteExtension.SkipAds}");
        }

        public void Dispose()
        {
            Console.WriteLine($"{nameof(YouTubeContent)}.Dispose");
            if (VideoWebSiteExtension != null)
            {

                VideoWebSiteExtension.Dispose();
            }
        }
        private void VideoWebSiteExtension_OnWatchedNodesUpdated(List<string> changedWatchNodes)
        {
            if (changedWatchNodes.Count == 0) return;
            StateHasChanged();
            var videoFound = VideoWebSiteExtension!.Found("video");
            var skipAd = VideoWebSiteExtension.Found("skipAd");
            var skipAd2 = VideoWebSiteExtension.Found("skipAd2");
            var ad = VideoWebSiteExtension.Found("ad-indicator") || skipAd || skipAd2;
            var muted = VideoWebSiteExtension.Muted;
            JS.Log(new
            {
                videoFound = videoFound,
                ad = ad,
                skipAd = skipAd,
                muted = muted,
            });
            if (!videoFound)
            {
                return;
            }
            if (ad && VideoWebSiteExtension.MuteAds && !muted)
            {
                Console.WriteLine("- Setting muted");
                VideoWebSiteExtension.Muted = true;
            }
            if (!ad && VideoWebSiteExtension.MuteAds && muted)
            {
                Console.WriteLine("- Setting unmuted");
                VideoWebSiteExtension.Muted = false;
            }
            if (ad && VideoWebSiteExtension.SkipAds)
            {
                Console.WriteLine("- Skipping ad");
                SkipAd();
            }
        }
        void SkipAd()
        {
            using var video = VideoWebSiteExtension?.GetWatchNodeEl<HTMLVideoElement>("video");
            if (video == null)
            {
                JS.Log("SkipAd video not found");
                return;
            }
            JS.Log("SkipAd video found");
            var duration = video.Duration;
            JS.Log("SkipAd video duration", duration);
            if (duration != null)
            {
                var currentTime = video.CurrentTime;
                var endTime = duration.Value - 1;
                if (currentTime < endTime)
                {
                    JS.Log("SkipAd setting currentTime", endTime);
                    video.CurrentTime = endTime;
                }
            }
        }
    }
}
