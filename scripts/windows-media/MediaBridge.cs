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

// MediaBridge owns this portion of the optional Windows media companion.
internal static partial class MediaBridge {
    static async Task Run() {
        DiscardCommand();
        while (!stopping) {
            try {
                if (LeaseActive()) {
                    if (!active) { DiscardCommand(); properties = null; }
                    active = true; await Update(); failures = 0;
                } else {
                    if (active) Clear(null);
                    active = false; manager = null; DiscardCommand(); status = "Waiting for Music page";
                }
            } catch (Exception ex) {
                status = "Media unavailable"; properties = null; manager = null; failures++;
                try { Clear(ex.GetBaseException().Message); } catch { }
            }
            await Task.Delay(failures > 0 ? 2000 : active ? 200 : 1000);
        }
        try { Clear(null); } catch { }
    }
    [STAThread]
    static int Main(string[] args) {
        try {
            if (args.Length == 1 && args[0] == "--check") {
                // Read-only Windows API check: no media files or playback commands.
                var check = Task.Run(async delegate {
                    var m = await Await(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
                    return m.GetSessions().Count;
                });
                check.GetAwaiter().GetResult(); return 0;
            }
            if (!Directory.Exists(Screen) || !Directory.Exists(Settings))
                throw new IOException("Keep the helper in scripts/windows-media inside the unpacked SmartShift mod.");
            string key;
            using (var sha = SHA256.Create()) key = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(ModRoot.ToLowerInvariant()))).Replace("-", "");
            bool first;
            using (var instance = new Mutex(true, "Local\\SigmaMediaBridge_" + key, out first)) {
                if (!first) return 0;
                Application.EnableVisualStyles();
                using (var tray = new NotifyIcon())
                using (var menu = new ContextMenuStrip())
                using (var timer = new System.Windows.Forms.Timer()) {
                    tray.Icon = SystemIcons.Information; tray.Text = "Sigma media - waiting for Music page";
                    menu.Items.Add("Sigma media companion").Enabled = false;
                    menu.Items.Add("Exit", null, delegate { stopping = true; Application.ExitThread(); });
                    tray.ContextMenuStrip = menu; tray.Visible = true;
                    tray.DoubleClick += delegate { tray.ShowBalloonTip(3000, "Sigma media", status, ToolTipIcon.Info); };
                    var worker = Task.Run((Func<Task>)Run);
                    timer.Interval = 1000;
                    timer.Tick += delegate { tray.Text = "Sigma media - " + status; };
                    timer.Start(); Application.Run(); stopping = true;
                    worker.Wait(6000); tray.Visible = false;
                }
                instance.ReleaseMutex();
            }
            return 0;
        } catch (Exception ex) {
            Console.Error.WriteLine(ex.ToString());
            if (args.Length == 0) MessageBox.Show(ex.GetBaseException().Message, "Sigma media", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
