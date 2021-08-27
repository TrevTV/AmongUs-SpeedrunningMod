# Among Us Speedrunning Mod [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6S244CYE)
A mod for Among Us that provides a timer in freeplay for speedrunners.

## Installation

1. Follow the [Automated Installation guide](https://melonwiki.xyz/#/?id=automated-installation) on the MelonLoader wiki page, installing to the Among Us exe.
2. Download the DLL from the [releases page](https://github.com/trevtv/AmongUs-SpeedrunningMod/releases)
3. Put that mod into `Among Us/Mods` (creating it, if needed) and run the game, the mod is now installed and can be used in-game.

## Usage
1. After installation, go into any freeplay map, if installed correctly you should see a clock in the bottom right corner.
2. Open the Task Laptop and the Tasks bar should be populated by every task available in that map.
3. Once you walk away after closing that laptop, the timer will begin counting and will end once the ending dialogue box appears.

## Hotkeys
`0 (number row)` - If the timer is not running, the timer is restarted. If the timer *is* running, it is stopped.

`9 (number row)` - Reloads the current scene.

## Toggle Enabling All Tasks
1. Open `Among Us\UserData\MelonPreferences.cfg` in Notepad after launching the game with the mod at least once.
2. Find the line that says `ToggleAllTasks = true` and change it to `ToggleAllTasks = false`
3. Reboot the game and those changes should take effect

## Acknowledgements
[Sinai](https://github.com/sinai-dev/) - Making [UnityExplorer](https://github.com/sinai-dev/UnityExplorer) and the AssetBundle that allowed me to fix the broken shader issues.

jkr#7021 - Helping with testing and other stuff.