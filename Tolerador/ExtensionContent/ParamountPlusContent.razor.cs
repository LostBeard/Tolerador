using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Tolerador.WebSiteExtensions;
using Window = SpawnDev.BlazorJS.JSObjects.Window;

namespace Tolerador.ExtensionContent
{
    public partial class ParamountPlusContent : IDisposable
    {
        [Inject]
        BlazorJSRuntime JS { get; set; }
        [Inject]
        BrowserExtensionService BrowserExtensionService { get; set; }
        [Inject]
        ContentBridgeService ContentBridge { get; set; }
        VideoWebSiteExtension? VideoWebSiteExtension = null;
        StorageArea SyncStorage { get; set; }
        int MuteToggleValue => VideoWebSiteExtension!.MuteAds ? 1 : 0;
        int SkipAdsToggleValue => VideoWebSiteExtension!.SkipAds ? 1 : 0;
        Document Document { get; set; }
        Window Window { get; set; }

        Screen Screen { get; set; }
        Dictionary<string, ParamountPlusVideo> Videos { get; } = new Dictionary<string, ParamountPlusVideo>();
        //bool IsFullHeightWindow = false;
        //bool IsFullWidthWindow = false;
        //Element? FullscreenElement = null;
        bool OverflowHidden = false;
        void Window_OnResize()
        {
            UpdateOverflowCheck();
        }
        void UpdateOverflowCheck(bool? playingValue = null)
        {
            JS.Log("UpdateOverflowCheck");
            using var fullscreenElement = Document.FullscreenElement;
            // the difference can be a a pixel so give a little leeway
            bool isFullHeightWindow = Math.Abs(Window.InnerHeight - Screen.Height) < 5;
            var playing = playingValue != null ? playingValue.Value : Videos.Values.FirstOrDefault(o => o.VideoElement.IsPlaying()) != null;
            var overflowHidden = isFullHeightWindow && fullscreenElement == null && playing;
            JS.Log("- Playing", playing);
            JS.Log("- IsFullHeightWindow", isFullHeightWindow);
            JS.Log("- FullscreenElement == null", fullscreenElement == null);
            JS.Log("== overflowHidden", overflowHidden);
            if (OverflowHidden != overflowHidden)
            {
                OverflowHidden = overflowHidden;
                using var htmlElement = Document.DocumentElement!.JSRefMove<HTMLElement>();
                using var style = htmlElement.Style;
                if (OverflowHidden)
                {
                    ScrollToTop();
                    style.SetProperty("overflow", "hidden");
                }
                else
                {
                    style.RemoveProperty("overflow");
                }
            }
        }
        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine($"{GetType().Name}.OnInitialized");
            SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;
            Document = JS.Get<Document>("document");
            Window = JS.Get<Window>("window");
            Window.OnResize += Window_OnResize;
            Screen = Window.Screen;
            VideoWebSiteExtension = new VideoWebSiteExtension(JS, BrowserExtensionService);
            // watch for document changes
            VideoWebSiteExtension.OnBodyObserverObserved += VideoWebSiteExtension_OnBodyObserverObserved;
            // find existing elements of interest
            VideoWebSiteExtension.OnWatchedNodesUpdated += VideoWebSiteExtension_OnWatchedNodesUpdated;
            VideoWebSiteExtension.WatchNodes = new List<WatchNode>
            {
                // paramountplus.com

                // ad-indicator
                // div.controls-brand-logo-wrapper

                // Video player block
                // div.video__player-area
                // div.video__player-area div.player-wrapper

                // div.video__player-area div.player-wrapper div[data-rule="adContainer"]
                // div.video__player-area div.player-wrapper div[data-rule="videoContainer"]

                // div.controls-brand-logo-wrapper (Paramount + Original 7s video at beginning of show)
                // div.controls-manager

                // video.marqueeVideo

                // skip intro, skip ad
                // <button type="button" class="skip-button" tabindex="0" aria-label="Skip Intro" style="display: block; margin-right: 40px !important;"><div class="skip-button__layout"><div class="icon-container skip-button__icon"><div class="svg-container"><svg viewBox = "0 0 39 21" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M16.5 10.5L14.6774 9.3039L2.5 1.3125L0.5 0V2.39221V18.6078V21L2.5 19.6875L14.6774 11.6961L16.5 10.5ZM12.8547 10.5L2.5 3.70471V17.2953L12.8547 10.5ZM34.5 10.5L32.6774 9.3039L20.5 1.3125L18.5 0V2.39221V18.6078V21L20.5 19.6875L32.6774 11.6961L34.5 10.5ZM30.8547 10.5L20.5 3.70471V17.2953L30.8547 10.5ZM36.5 0H38.5V2V19V21H36.5V19V2V0Z" fill="white"></path></svg></div></div><span class="skip-button__text">SKIP</span></div></button>
                // <button type="button" class="skip-button" tabindex="0" aria-label="Skip Intro" style="display: block; margin-right: 40px !important;"><div class="skip-button__layout"><div class="icon-container skip-button__icon" style="width: 13px;"><div class="svg-container"><svg viewBox="0 0 39 21" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M16.5 10.5L14.6774 9.3039L2.5 1.3125L0.5 0V2.39221V18.6078V21L2.5 19.6875L14.6774 11.6961L16.5 10.5ZM12.8547 10.5L2.5 3.70471V17.2953L12.8547 10.5ZM34.5 10.5L32.6774 9.3039L20.5 1.3125L18.5 0V2.39221V18.6078V21L20.5 19.6875L32.6774 11.6961L34.5 10.5ZM30.8547 10.5L20.5 3.70471V17.2953L30.8547 10.5ZM36.5 0H38.5V2V19V21H36.5V19V2V0Z" fill="white"></path></svg></div></div><span class="skip-button__text">SKIP</span></div></button>

                // <div class="controls-time-wrapper show controls--md-margin" aria-hidden="false"><div class="controls-progress-time">00:32</div><div class="controls-duration">46:31</div></div>

                new WatchNode("video", "video", checkVisibility: true),
                new WatchNode("skipAd", "button.skip-button", checkVisibility: true),
                new WatchNode("play", "button.btn-play-pause"),
                new WatchNode("fullscreen", "button.btn-fullscreen"),
                new WatchNode("mute", "button.btn-volume"),
            };
            VideoWebSiteExtension.MuteAds = await SyncStorage.Get<bool>($"{GetType().Name}_MuteAds", true);
            VideoWebSiteExtension.SkipAds = await SyncStorage.Get<bool>($"{GetType().Name}_SkipAds", true);
            //
            ElementUpdate();
        }
        private void VideoWebSiteExtension_OnBodyObserverObserved(Array<MutationRecord> mutations, MutationObserver sender)
        {
            ElementUpdate();
        }
        /// <summary>
        /// checks if any video elements have been added or removed
        /// </summary>
        void ElementUpdate()
        {
            using var videoElements = Document.QuerySelectorAll("video");
            var videoIdsFound = new List<string>();
            foreach (Node node in videoElements)
            {
                var video = node.JSRefMove<HTMLVideoElement>();
                var videoId = ParamountPlusVideo.GetVideoElementVideoId(video);
                if (string.IsNullOrEmpty(videoId))
                {
                    var paramountPlusVideo = new ParamountPlusVideo(video, ContentBridge, JS);
                    videoIdsFound.Add(paramountPlusVideo.VideoId);
                    Videos.Add(paramountPlusVideo.VideoId, paramountPlusVideo);
                    VideoElementFound(paramountPlusVideo);
                }
                else
                {
                    videoIdsFound.Add(videoId);
                    video.Dispose();
                }
            }
            var lostIds = Videos.Keys.Except(videoIdsFound);
            foreach (var lostId in lostIds)
            {
                if (Videos.TryGetValue(lostId, out var paramountPlusVideo))
                {
                    Videos.Remove(lostId);
                    VideoElementLost(paramountPlusVideo);
                    paramountPlusVideo.Dispose();
                }
            }
        }
        void ScrollToTop()
        {
            // below method can load incorrect page url
            //using var location = Window.Location;
            //location.Href = "#";
        }
        void Video_OnPlay()
        {
            UpdateOverflowCheck(true);
        }
        void Video_OnPause()
        {
            UpdateOverflowCheck(false);
        }
        void VideoElementFound(ParamountPlusVideo videoElement)
        {
            var ext = videoElement.Player != null ? "++" : "";
            JS.Log($"ParamountPlusVideo{ext} element found", videoElement.VideoId, Videos.Count);
            videoElement.VideoElement.OnPlay += Video_OnPlay;
            videoElement.VideoElement.OnPause += Video_OnPause;
            UpdateOverflowCheck();
        }
        void VideoElementLost(ParamountPlusVideo videoElement)
        {
            var ext = videoElement.Player != null ? "++" : "";
            JS.Log($"ParamountPlusVideo{ext} element lost", videoElement.VideoId, Videos.Count);
            videoElement.VideoElement.OnPlay -= Video_OnPlay;
            videoElement.VideoElement.OnPause -= Video_OnPause;
            UpdateOverflowCheck();
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
                // 
                VideoWebSiteExtension.Dispose();
            }
        }
        private void VideoWebSiteExtension_OnWatchedNodesUpdated(List<string> changedWatchNodes)
        {
            //if (changedWatchNodes.Count == 0) return;
            var videoFound = Videos.Count > 0;
            if (!videoFound)
            {
                return;
            }
            //var playing1 = VideoWebSiteExtension.Playing;

            //var playing = Videos.Values.FirstOrDefault(o => o.VideoElement.IsPlaying()) != null;
            var playing = Videos.Values.FirstOrDefault(o => o.VideoElement.IsPlaying()) != null;

            var skipAd = VideoWebSiteExtension.Found("skipAd");
            var ad = VideoWebSiteExtension.Found("ad-indicator") || skipAd;
            var muted = VideoWebSiteExtension.Muted;
            //JS.Log(new
            //{
            //    videoFound = videoFound,
            //    playing = playing,
            //    ad = ad,
            //    skipAd = skipAd,
            //    muted = muted,
            //});
            //if (playing != Playing)
            //{
            //    Playing = playing;
            //    UpdateOverflowCheck();
            //}
            var requirePlaying = true;
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
            if (ad && !muted)
            {
                Console.WriteLine("- Setting muted ??");
                if (VideoWebSiteExtension.MuteAds)
                {
                    Console.WriteLine("- Setting muted");
                    VideoWebSiteExtension.Muted = true;
                }
            }
            if (!ad && muted)
            {
                Console.WriteLine("- Setting unmuted??");
                if (VideoWebSiteExtension.MuteAds)
                {
                    Console.WriteLine("- Setting unmuted");
                    VideoWebSiteExtension.Muted = false;
                }
            }
        }
    }
}
