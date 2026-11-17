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

// Transport owns this portion of the optional Windows media companion.
internal static partial class MediaBridge {
    static long NowMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
    static Dictionary<string, object> Read(string path) {
        return Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8));
    }
    static object Field(Dictionary<string, object> data, string key) {
        object value; return data != null && data.TryGetValue(key, out value) ? value : null;
    }
    static string Text(Dictionary<string, object> data, string key) { return Convert.ToString(Field(data, key)); }
    static bool LeaseActive() {
        try {
            var lease = Read(Lease);
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Convert.ToInt64(Field(lease, "issuedAt"));
            return Field(lease, "vehicleId") != null && Text(lease, "page") == "music" && age >= 0 && age < 3;
        } catch { return false; }
    }
    static async Task<T> Await<T>(IAsyncOperation<T> operation) {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        try {
            while (operation.Status == AsyncStatus.Started) {
                if (DateTime.UtcNow >= deadline) {
                    operation.Cancel(); throw new TimeoutException("Windows media request timed out.");
                }
                await Task.Delay(10);
            }
            if (operation.Status != AsyncStatus.Completed)
                throw new IOException("Windows media request failed.", operation.ErrorCode);
            return operation.GetResults();
        } finally { operation.Close(); }
    }
    static void Write(string filename, object data) {
        if (filename != "media_bridge_state.data" && filename != "media_bridge_artwork.data")
            throw new ArgumentException("Unexpected output filename");
        var path = Path.Combine(Screen, filename);
        var temp = path + ".tmp";
        File.WriteAllText(temp, Json.Serialize(data), new UTF8Encoding(false));
        if (File.Exists(path)) File.Replace(temp, path, null);
        else File.Move(temp, path);
    }
}
