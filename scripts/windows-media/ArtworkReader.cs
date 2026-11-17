using System;
using System.IO;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Media.Control;

public static class SegraArtworkReader {
    private static GlobalSystemMediaTransportControlsSession watchedSession;
    private static int metadataDirty = 1;
    public static int ArtworkReads = 0;
    public static void Watch(object value) {
        var session = value as GlobalSystemMediaTransportControlsSession;
        if (Object.ReferenceEquals(session, watchedSession)) return;
        if (watchedSession != null) watchedSession.MediaPropertiesChanged -= MediaChanged;
        watchedSession = session;
        if (watchedSession != null) watchedSession.MediaPropertiesChanged += MediaChanged;
        Interlocked.Exchange(ref metadataDirty, 1);
    }
    private static void MediaChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
        Interlocked.Exchange(ref metadataDirty, 1);
    }
    public static bool ConsumeMetadataChange() { return Interlocked.Exchange(ref metadataDirty, 0) != 0; }
    public static string ContentType = "image/png";
    public static byte[] Read(object thumbnail) {
        ArtworkReads++;
        var reference = (IRandomAccessStreamReference)thumbnail;
        var open = reference.OpenReadAsync();
        Wait(open);
        using (var stream = open.GetResults()) {
            ContentType = stream.ContentType;
            if (stream.Size > 8388608) throw new IOException("Media thumbnail exceeds 8 MB.");
            using (var reader = new DataReader(stream.GetInputStreamAt(0))) {
                var load = reader.LoadAsync((uint)stream.Size);
                Wait(load);
                uint count = load.GetResults();
                var bytes = new byte[count];
                reader.ReadBytes(bytes);
                return bytes;
            }
        }
    }
    private static void Wait(IAsyncInfo operation) {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (operation.Status == AsyncStatus.Started) {
            if (DateTime.UtcNow > timeout) { operation.Cancel(); throw new TimeoutException(); }
            Thread.Sleep(10);
        }
        if (operation.Status != AsyncStatus.Completed) throw new IOException("Artwork read failed.", operation.ErrorCode);
    }
}
