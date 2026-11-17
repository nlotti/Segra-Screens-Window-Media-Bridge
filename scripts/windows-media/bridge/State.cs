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

// State owns this portion of the optional Windows media companion.
internal static partial class MediaBridge {
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 };
    static readonly string ModRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../.."));
    static readonly string Screen = Path.Combine(ModRoot, "vehicles/segra/infotainment");
    static readonly string Settings = Path.GetFullPath(Path.Combine(ModRoot, "../../../settings"));
    static readonly string Lease = Path.Combine(Settings, "segra_media_lease.json");
    static readonly string Command = Path.Combine(Settings, "segra_media_command.json");
    static GlobalSystemMediaTransportControlsSessionManager manager;
    static GlobalSystemMediaTransportControlsSessionMediaProperties properties;
    static string artworkHash = "", publishedHash, artworkSource = "", lastCommand = "";
    static object commandResult;
    static volatile bool stopping;
    static volatile string status = "Waiting for Music page";
    static bool active;
    static int failures;
}
