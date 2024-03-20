using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;

namespace Tolerador.ExtensionContent
{
    /// <summary>
    /// This class will be accessed via a the content-bridge
    /// </summary>
    public class ParamountPlusPlayer : JSObject
    {
        public ParamountPlusPlayer(IJSInProcessObjectReference _ref) : base(_ref) { }
        public List<ParamountPlayerAdBreak>? GetAdBreakTimes()
        {
            // call getAdBreakTimes and unwrap the return value
            using var ads = JSRef!.Call<WrappedObjectProxy>("getAdBreakTimes");
            var ret = ads == null ? null : ads.WrappedObjectDirect<List<ParamountPlayerAdBreak>>();
            ads?.WrappedObjectRelease();
            return ret;
        }
        /// <summary>
        /// Returns the video element that player belongs to via a proxy (as this is also proxy)
        /// </summary>
        public HTMLVideoElement Video => JSRef.Get<HTMLVideoElement>("video");
    }
    public class ParamountPlusVideo : IDisposable
    {
        public const string VideoIdTag = "__extensionVideoId";
        public static string? GetVideoElementVideoId(HTMLVideoElement videoElement) => videoElement.JSRef!.Get<string?>(VideoIdTag);
        public static void SetVideoElementVideoId(HTMLVideoElement videoElement, string videoId) => videoElement.JSRef!.Set(VideoIdTag, videoId);
        public string VideoId { get; private set; }
        BlazorJSRuntime JS;
        ContentBridgeService ContentBridge;
        public HTMLVideoElement VideoElement { get; private set; }
        public ParamountPlusPlayer? Player { get; private set; }
        public List<ParamountPlayerAdBreak> AdBreaks { get; private set; } = new List<ParamountPlayerAdBreak>();
        public ParamountPlusVideo(string videoId, HTMLVideoElement videoElement, ContentBridgeService contentBridge, BlazorJSRuntime js)
        {
            VideoId = videoId;
            JS = js;
            ContentBridge = contentBridge;
            VideoElement = videoElement;
            Player = GetParamountPlusPlayer(VideoElement, ContentBridge);
            AttachVideoElementEvents();
            UpdateVideoElement();
        }
        public ParamountPlusVideo(HTMLVideoElement videoElement, ContentBridgeService contentBridge, BlazorJSRuntime js)
        {
            VideoId = GetVideoElementVideoId(videoElement) ?? "";
            if (string.IsNullOrEmpty(VideoId))
            {
                VideoId = Guid.NewGuid().ToString();
                SetVideoElementVideoId(videoElement, VideoId);
            }
            JS = js;
            ContentBridge = contentBridge;
            VideoElement = videoElement;
            Player = GetParamountPlusPlayer(VideoElement, ContentBridge);
            AttachVideoElementEvents();
            UpdateVideoElement();
        }
        public static ParamountPlusPlayer? GetParamountPlusPlayer(HTMLVideoElement videoElement, ContentBridgeService contentBridge)
        {
            try
            {
                // get videoElement from the main side so the javascript properties are available
                using var el = contentBridge.SyncDispatcher.GetDocumentElementRemote(videoElement);
                if (el != null)
                {
                    var player = el.JSRef!.Get<ParamountPlusPlayer?>("player");
                    el.WrappedObjectRelease();
                    return player;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"GetParamountPlusPlayer failed: {ex.Message}");
#endif
            }
            return null;
        }
        void AttachVideoElementEvents()
        {
            VideoElement.OnLoadedMetadata += VideoElement_OnLoadedMetadata;
            VideoElement.OnPlay += VideoElement_OnPlay;
            VideoElement.OnPlaying += VideoElement_OnPlaying;
            VideoElement.OnPause += VideoElement_OnPause;
            VideoElement.OnVolumeChange += VideoElement_OnVolumeChange;
            VideoElement.OnDurationChange += VideoElement_OnDurationChange;
            VideoElement.OnTimeUpdate += VideoElement_OnTimeUpdate;
        }
        void DetachVideoElementEvents()
        {
            VideoElement.OnLoadedMetadata -= VideoElement_OnLoadedMetadata;
            VideoElement.OnPlay -= VideoElement_OnPlay;
            VideoElement.OnPlaying -= VideoElement_OnPlaying;
            VideoElement.OnPause -= VideoElement_OnPause;
            VideoElement.OnVolumeChange -= VideoElement_OnVolumeChange;
            VideoElement.OnDurationChange -= VideoElement_OnDurationChange;
            VideoElement.OnTimeUpdate -= VideoElement_OnTimeUpdate;
        }

        public bool IsDisposed { get; private set; } = false;
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            if (Player != null)
            {
                Player.WrappedObjectRelease();
                Player.Dispose();
                Player = null;
            }
            if (VideoElement != null)
            {
                // detach events 
                DetachVideoElementEvents();
                VideoElement.Dispose();
            }
        }
        void UpdateVideoElement()
        {
            if (Player != null)
            {
                var ads = Player.GetAdBreakTimes();
                JS.Log("__ads", ads);
                if (ads != null)
                {
                    AdBreaks = ads;
                }
            }
        }
        void VideoElement_OnLoadedMetadata(Event e)
        {
            JS.Log("VideoElement_OnLoadedMetadata", VideoId);
            // get the video element proxy for website side of given element
            UpdateVideoElement();
        }
        void VideoElement_OnTimeUpdate(Event e)
        {
            //JS.Log("VideoElement_OnTimeUpdate", videoId, videoElement.CurrentTime, videoElement.Duration);
        }
        void VideoElement_OnDurationChange(Event e)
        {
            JS.Log("VideoElement_OnDurationChange", VideoId, VideoElement.CurrentTime, VideoElement.Duration);
        }
        void VideoElement_OnVolumeChange(Event e)
        {
            JS.Log("VideoElement_OnVolumeChange", VideoId, VideoElement.Volume, VideoElement.Muted);
        }
        void VideoElement_OnPlay(Event e)
        {
            JS.Log("VideoElement_OnPlay", VideoId);

        }
        void VideoElement_OnPlaying(Event e)
        {
            JS.Log("VideoElement_OnPlaying", VideoId);
            UpdateVideoElement();
        }
        void VideoElement_OnPause(Event e)
        {
            JS.Log("VideoElement_OnPause", VideoId);

        }
    }
}
