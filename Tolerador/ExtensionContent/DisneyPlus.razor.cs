using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Tolerador.WebSiteExtensions;

namespace Tolerador.ExtensionContent
{
    public partial class DisneyPlus : IDisposable
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
            Console.WriteLine("DisneyPlus.OnInitialized");
            // video-ads
            // ytp-ad-preview-container
            // "ytp-ad-skip-button-modern ytp-button"

            // <span class="ytp-ad-preview-container ytp-ad-preview-container-detached modern-countdown-next-to-thumbnail" style=""><div class="ytp-ad-text ytp-ad-preview-text-modern" id="ad-text:3" style="">2</div><span class="ytp-ad-preview-image-modern"><img class="ytp-ad-image" id="ad-image:4" src="https://i.ytimg.com/vi/YIUJyJEZ-10/mqdefault_live.jpg" alt="" style=""></span></span>

            SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;

            VideoWebSiteExtension = new VideoWebSiteExtension(JS, BrowserExtensionService);
            VideoWebSiteExtension.OnWatchedNodesUpdated += VideoWebSiteExtension_OnWatchedNodesUpdated;
            VideoWebSiteExtension.WatchNodes = new List<WatchNode>
            {
                new WatchNode("restart", "button.play-from-start-icon"),
                new WatchNode("mute", "button.mute-btn"),
                new WatchNode("mute-on", "button.mute-btn--on"),
                new WatchNode("mute-off", "button.mute-btn--off"),
                new WatchNode("video", "disney-web-player video", true){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                new WatchNode("fullscreen", "toggle-fullscreen-button info-tooltip button.fullscreen-icon"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                new WatchNode("exit-fullscreen", "toggle-fullscreen-button info-tooltip button.exit-fullscreen-icon"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                new WatchNode("play", "toggle-play-pause info-tooltip button.play-button"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                new WatchNode("pause", "toggle-play-pause info-tooltip button.pause-button"){ ShadowRootQueryMode = ShadowRootQueryMode.Wide },
                new WatchNode("ad-indicator", ".overlay_interstitials"),
            };

            VideoWebSiteExtension.MuteAds = await SyncStorage.Get<bool>($"{nameof(DisneyPlus)}_MuteAds", true);
            VideoWebSiteExtension.SkipAds = await SyncStorage.Get<bool>($"{nameof(DisneyPlus)}_SkipAds", true);
            if (VideoWebSiteExtension.LocationSupported)
            {
                VideoWebSiteExtension.WatchedNodesUpdate();
            }
        }
        async Task MuteAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.MuteAds = index == 1;
            await SyncStorage.Set($"{nameof(DisneyPlus)}_MuteAds", VideoWebSiteExtension.MuteAds);
            Console.WriteLine($"{nameof(DisneyPlus)}_MuteAds: {VideoWebSiteExtension.MuteAds}");
        }
        async Task SkipAds_OnClicked(int index)
        {
            VideoWebSiteExtension!.SkipAds = index == 1;
            await SyncStorage.Set($"{nameof(DisneyPlus)}_SkipAds", VideoWebSiteExtension.SkipAds);
            Console.WriteLine($"{nameof(DisneyPlus)}_SkipAds: {VideoWebSiteExtension.SkipAds}");
        }

        public void Dispose()
        {
            Console.WriteLine($"{nameof(DisneyPlus)}.Dispose");
            if (VideoWebSiteExtension != null)
            {

                VideoWebSiteExtension.Dispose();
            }
        }
        bool AdShowing = false;
        private void VideoWebSiteExtension_OnWatchedNodesUpdated(List<string> changedWatchNodes)
        {
            if (changedWatchNodes.Count == 0) return;
            StateHasChanged();
            var videoFound = VideoWebSiteExtension!.Found("video");
            var skipAd = VideoWebSiteExtension.Found("skipAd");
            var ad = VideoWebSiteExtension.Found("ad-indicator") || skipAd;
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
            var adStateChanged = AdShowing != ad;
            if (AdShowing != ad)
            {
                AdShowing = ad;
                if (ad && !muted)
                {
                    Console.WriteLine("- Setting muted ??");
                    if (VideoWebSiteExtension.MuteAds)
                    {
                        Console.WriteLine("- Setting muted");
                        VideoWebSiteExtension.Muted = true;
                    }
                }
                else if (!ad && muted)
                {
                    Console.WriteLine("- Setting unmuted??");
                    if (VideoWebSiteExtension.MuteAds)
                    {
                        Console.WriteLine("- Setting unmuted");
                        VideoWebSiteExtension.Muted = false;
                    }
                }
            }
            using var video = VideoWebSiteExtension?.GetWatchNodeEl<HTMLVideoElement>("video");
            if (video != null)
            {
                if (ad && VideoWebSiteExtension!.SkipAds)
                {
                    if (video.PlaybackRate != AdPlaybackRate)
                    {
                        Console.WriteLine("- Skipping ad via playback rate");
                        video.PlaybackRate = AdPlaybackRate;
                    }
                }
                else
                {
                    if (video.PlaybackRate != DefaultPlaybackRate)
                    {
                        Console.WriteLine("- Resetting playback rate");
                        video.PlaybackRate = DefaultPlaybackRate;
                    }
                }
            }
        }
        float AdPlaybackRate = 16;
        float DefaultPlaybackRate = 1;
    }
}
