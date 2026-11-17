using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Windows.Media.Control;
using Windows.Foundation;

// Session owns this portion of the optional Windows media companion.
internal static partial class MediaBridge {
    static async Task Update() {
        if (manager == null) manager = await Await(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
        if (!LeaseActive()) return;
        var session = manager.GetCurrentSession();
        SegraArtworkReader.Watch(session);
        if (session == null) { Clear(null); status = "Connected - no media playing"; return; }
        await ReceiveCommand(session);
        if (SegraArtworkReader.ConsumeMetadataChange() || properties == null) {
            properties = await Await(session.TryGetMediaPropertiesAsync());
            byte[] bytes = new byte[0];
            // Missing/broken artwork must not disable metadata and controls.
            try { if (properties.Thumbnail != null) bytes = SegraArtworkReader.Read(properties.Thumbnail); } catch { }
            using (var sha = SHA256.Create()) artworkHash = bytes.Length == 0 ? "" : Convert.ToBase64String(sha.ComputeHash(bytes));
            artworkSource = bytes.Length == 0 ? "" : "data:" + SegraArtworkReader.ContentType + ";base64," + Convert.ToBase64String(bytes);
        }
        if (!LeaseActive()) return;
        var timeline = session.GetTimelineProperties(); var playback = session.GetPlaybackInfo();
        Publish(new Dictionary<string, object> {
            {"available", true}, {"artworkRevision", artworkHash}, {"title", properties.Title}, {"artist", properties.Artist},
            {"sourceApp", session.SourceAppUserModelId}, {"artworkReads", SegraArtworkReader.ArtworkReads},
            {"positionSeconds", Math.Max(0, (timeline.Position - timeline.StartTime).TotalSeconds)},
            {"durationSeconds", Math.Max(0, (timeline.EndTime - timeline.StartTime).TotalSeconds)},
            {"positionUpdatedAt", timeline.LastUpdatedTime.ToUnixTimeMilliseconds()},
            {"playbackStatus", playback.PlaybackStatus.ToString()}, {"playbackRate", playback.PlaybackRate ?? 1},
            {"canSeek", playback.Controls.IsPlaybackPositionEnabled}, {"commandResult", commandResult}, {"error", null}
        });
        status = "Connected to Music page";
    }
}
