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

// Publication owns this portion of the optional Windows media companion.
internal static partial class MediaBridge {
    static void Publish(Dictionary<string, object> state) {
        if (publishedHash != artworkHash) {
            Write("media_bridge_artwork.data", new { source = artworkSource, artworkRevision = artworkHash });
            publishedHash = artworkHash;
        }
        state["updatedAt"] = NowMs();
        Write("media_bridge_state.data", state);
    }
    static void Clear(string error) {
        SegraArtworkReader.Watch(null); properties = null;
        artworkHash = ""; artworkSource = "";
        Publish(new Dictionary<string, object> {
            {"available", false}, {"artworkRevision", 0}, {"title", ""}, {"artist", ""}, {"error", error}
        });
    }
}
