# Sigma Windows media companion

Double-click **SigmaMedia.exe** once, then open the truck's **Music** page.
The helper appears in the Windows notification area. Right-click its icon and
choose **Exit** to stop it. Launching it twice does not create another instance.

Requires Windows 10 version 1809 or newer on x64, with .NET Framework 4.8.
Running the helper requires no PowerShell script, installer, account login, localhost server,
package download, registry change, or automatic startup registration.
The helper uses Windows' current media session, so player support determines
which artwork and playback controls are available.

Keep this folder inside `scripts/windows-media` in the unpacked SmartShift mod.
The mod's folder name and Windows username do not matter. The helper discovers
the mod and BeamNG settings relative to its executable. A zipped mod must be
unpacked first: the dashboard reads media updates from the unpacked mod files.

The helper reads Windows media only while the player vehicle's installed screen
is on Music. Within three seconds of leaving Music, switching vehicles, removing
the part, or closing BeamNG, access stops and the helper idles. It does not alter
playback unless a dashboard playback command is received. Commands expire after
ten seconds and must match the current application. Seeking is limited to the
range supported by that player.

Track information and artwork are written to the existing `.data` files in
`vehicles/segra/infotainment`. There are no HTTP requests to outside services.
Artwork is cached until the track/session changes; timeline updates occur every
200 ms. Current-track artwork is available; next-track previews are not.
Reset both generated `.data` files to empty placeholders before release so the
package contains no personal media information.

BeamNG's default Lua sandbox does not permit loading a custom Windows API DLL.
Therefore the executable needs one manual start; opening Music activates an
already-running companion. No game sandbox changes are required.

`MediaBridge.cs`, `bridge/*.cs` and `ArtworkReader.cs` contain the complete source.
The entry file owns the tray and process loop. The `bridge` folder separates
state, transport/lease, publication, playback commands and session reading.
These are ordinary C# partial-class files sharing the existing private state.

From the mod root, run `./scripts/windows-media/build.ps1` to compile the x64
`winexe` using .NET Framework `csc.exe` and the installed Windows metadata.
The script resolves the required framework references and prints the output's
SHA-256 hash. Use `-OutputPath` to build to another location. The build never
starts the helper. All dependencies are Windows/.NET components; no separate
companion DLL ships.

`SigmaMedia.exe --check` performs a read-only Windows API compatibility check
and returns exit code 0 on success, or 1 on failure, without changing playback
or writing media files.
