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

// Commands owns this portion of the optional Windows media companion.
internal static partial class MediaBridge {
    static void DiscardCommand() {
        try { lastCommand = Text(Read(Command), "id"); } catch { }
    }
    static async Task ReceiveCommand(GlobalSystemMediaTransportControlsSession session) {
        Dictionary<string, object> cmd;
        try { cmd = Read(Command); } catch { return; }
        var id = Text(cmd, "id");
        if (id == "" || id == lastCommand) return;
        lastCommand = id;
        var action = Text(cmd, "action");
        try {
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Convert.ToInt64(Field(cmd, "issuedAt"));
            if (age < 0 || age > 10 || !LeaseActive()) return;
            if (session == null || Text(cmd, "sourceApp") == "" || Text(cmd, "sourceApp") != session.SourceAppUserModelId)
                throw new InvalidOperationException("Media session changed; command ignored.");
            IAsyncOperation<bool> operation;
            switch (action) {
                case "toggle": operation = session.TryTogglePlayPauseAsync(); break;
                case "play": operation = session.TryPlayAsync(); break;
                case "pause": operation = session.TryPauseAsync(); break;
                case "stop": operation = session.TryStopAsync(); break;
                case "next": operation = session.TrySkipNextAsync(); break;
                case "previous": operation = session.TrySkipPreviousAsync(); break;
                case "seek":
                    if (!session.GetPlaybackInfo().Controls.IsPlaybackPositionEnabled)
                        throw new InvalidOperationException("Player does not support seeking.");
                    var seconds = Convert.ToDouble(Field(cmd, "seconds"));
                    if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                        throw new ArgumentException("Invalid seek position.");
                    var t = session.GetTimelineProperties();
                    var lo = Math.Max(t.StartTime.Ticks, t.MinSeekTime.Ticks);
                    var hi = Math.Min(t.EndTime.Ticks, t.MaxSeekTime.Ticks);
                    if (hi <= lo) throw new InvalidOperationException("No seekable range.");
                    var requested = t.StartTime.Ticks + (long)(Math.Min(seconds, (t.EndTime - t.StartTime).TotalSeconds) * 10000000);
                    operation = session.TryChangePlaybackPositionAsync(Math.Max(lo, Math.Min(hi, requested))); break;
                default: throw new ArgumentException("Unknown media command.");
            }
            commandResult = new { action = action, accepted = await Await(operation), error = (string)null };
        } catch (Exception ex) {
            commandResult = new { action = action, accepted = false, error = ex.GetBaseException().Message };
        }
    }
}
