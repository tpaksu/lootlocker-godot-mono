<h1 align="center">LootLocker Godot Mono SDK</h1>

<h1 align="center">
  <a href="https://www.lootlocker.com/"><img src="https://s3.eu-west-1.amazonaws.com/cdn.lootlocker.io/public/lootLocker_wide_dark_whiteBG.png" alt="LootLocker"></a>
</h1>

<p align="center">
  <a href="#about">About</a> •
  <a href="#installation">Installation</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#updating">Updating</a> •
  <a href="#support">Support</a> •
  <a href="#development">Development</a>
</p>

---

## About
LootLocker is a game backend-as-a-service that unlocks cross-platform systems so you can build, ship, and run your best games.

Full documentation is available here https://docs.lootlocker.com/

## Installation

Be aware that this is a porting attempt from LootLocker's Unity SDK to Godot Mono, and for now, this can only be installed manually. When the library becomes mature enough, it can be added to Godot Asset Library, and can be installed from there.
For now, download this repository to your computer, and copy the `lootlocker` folder to your Godot project directory.

## Configuration

- Add a new node to your global scene (if you have any), and attach the script `lootlocker/Runtime/LootLocker.cs` file to it.
- Log on to the [LootLocker management console](https://console.lootlocker.com/login) and find your Game Settings.
- Find your Game Key and Domain Key in the [API section of the settings](https://console.lootlocker.com/settings/api-keys)
- Click the node you added the script, and fill the:
    - LootLocker API Key
    - LootLocker Domain Key
    - LootLocker Game Version
  fields.

## Updating
If you have installed the SDK from Open UPM then all you have to do in Package Manager is press the Update button when that tells you there's a new version.

For other install methods and more information, head over to our [official documentation](https://docs.lootlocker.com/the-basics/unity-quick-start/updating-sdk).

## Support
If you have any issues, please create an issue on this repository.

Contributions are very welcome.
