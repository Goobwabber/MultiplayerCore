# MultiplayerCore (Steam/PC-Only) [![Build](https://github.com/Goobwabber/MultiplayerCore/workflows/Build/badge.svg?event=push)](https://github.com/Goobwabber/MultiplayerCore/actions?query=workflow%3ABuild+branch%3Amain)
A Beat Saber mod that implements core custom multiplayer functionality. This is a core mod and doesn't really add anything to the game without a custom server to support it's features. I Recommend [BeatTogether](https://github.com/BeatTogether/BeatTogether).

## Installation
1. Ensure you have the [required mods](https://github.com/Goobwabber/MultiplayerCore#requirements).
2. Download the `MultiplayerCore` file listed under `Assets` **[Here](https://github.com/Goobwabber/MultiplayerCore/releases)**.
   * Optionally, you can get a development build by downloading the file listed under `Artifacts`  **[Here](https://github.com/Goobwabber/MultiplayerCore/actions?query=workflow%3ABuild+branch%3Amain)** (pick the topmost successful build).
   * You must be logged into GitHub to download a development build.
3. Extract the zip file to your Beat Saber game directory (the one `Beat Saber.exe` is in).
   * The `MultiplayerCore.dll` (and `MultiplayerCore.pdb` if it exists) should end up in your `Plugins` folder (**NOT** the one in `Beat Saber_Data`).
4. **Optional**: Edit `Beat Saber IPA.json` (in your `UserData` folder) and change `Debug` -> `ShowCallSource` to `true`. This will enable BSIPA to get file and line numbers from the `PDB` file where errors occur, which is very useful when reading the log files. This may have a *slight* impact on performance.

Lastly, check out [other mods](https://github.com/Goobwabber/MultiplayerCore#related-mods) that depend on MultiplayerCore!

## Requirements
These can be downloaded from [BeatMods](https://beatmods.com/#/mods) or using Mod Assistant.
* BSIPA v4.1.4+
* SongCore v3.9.3+
* BeatSaverSharp v3.0.1+
* SiraUtil 3.0.0+
* BeatSaberMarkupLanguage 1.6.3+

## Reporting Issues
* The best way to report issues is to click on the `Issues` tab at the top of the GitHub page. This allows any contributor to see the problem and attempt to fix it, and others with the same issue can contribute more information. **Please try the troubleshooting steps before reporting the issues listed there. Please only report issues after using the latest build, your problem may have already been fixed.**
* Include in your issue:
  * A detailed explanation of your problem (you can also attach videos/screenshots)
  * **Important**: The log file from the game session the issue occurred (restarting the game creates a new log file).
    * The log file can be found at `Beat Saber\Logs\_latest.log` (`Beat Saber` being the folder `Beat Saber.exe` is in).
* If you ask for help on Discord, at least include your `_latest.log` file in your help request.

## For Other Modders
How do I send and receive packets???
ok just do this
oh yeah also you have to register the packet before you send it!!!!
```cs
// ok cool wanna make a packet?
public class WhateverTheFuckPacket : MpPacket {
    public override void Serialize(NetDataWriter writer)
    {
        // write data into packet
    }

    public override void Deserialize(NetDataReader reader)
    {
        // read data from packet
    }
}

// ok cool wanna send it and receive it now?
public class WhateverTheFuckManager : IInitializable, IDisposable
{
    [Inject]
    private readonly MpPacketSerializer _packetSerializer;

    public void Initialize()
        => _packetSerializer.RegisterCallback<WhateverTheFuckPacket>(HandlePacket);

    public void Dispose()
        => _packetSerializer.UnregisterCallback<WhateverTheFuckPacket>();

    public void Send()
    {
        // send (you can do this from anywhere really that the game can handle it but i prefer to do it here)
        _multiplayerSessionManager.Send(new WhateverTheFuckPacket());
    }

    public void HandlePacket(WhateverTheFuckPacket packet, IConnectedPlayer player)
    {
        // handle that shit fam 
    }
}
```

## Contributing
Anyone can feel free to contribute bug fixes or enhancements to MultiplayerCore. Please keep in mind that this mod's purpose is to implement core functionality of modded multiplayer, so we will likely not be accepting enhancements that fall out of that scope. GitHub Actions for Pull Requests made from GitHub accounts that don't have direct access to the repository will fail. This is normal because the Action requires a `Secret` to download dependencies.
### Building
Visual Studio 2022 with the [BeatSaberModdingTools](https://github.com/Zingabopp/BeatSaberModdingTools) extension is the recommended development environment.
1. Check out the repository
2. Open `MultiplayerCore.sln`
3. Right-click the `MultiplayerCore` project, go to `Beat Saber Modding Tools` -> `Set Beat Saber Directory`
   * This assumes you have already set the directory for your Beat Saber game folder in `Extensions` -> `Beat Saber Modding Tools` -> `Settings...`
   * If you do not have the BeatSaberModdingTools extension, you will need to manually create a `MultiplayerCore.csproj.user` file to set the location of your game install. An example is showing below.
4. The project should now build.

**Example csproj.user File:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <BeatSaberDir>Full\Path\To\Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```
## Donate
You can support development of MultiplayerCore by donating at the following links:
* https://www.patreon.com/goobwabber
* https://ko-fi.com/goobwabber

## Related Mods
* [MultiplayerExtensions](https://github.com/Goobwabber/MultiplayerExtensions)
* [BeatTogether](https://github.com/BeatTogether/BeatTogether)
