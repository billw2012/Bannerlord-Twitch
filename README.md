# BLT: Bannerlord Twitch
This is a Mount and Blade: Bannerlord modification that adds Twitch integration to the game, along with a framework for extensibility.

# Features
- Define channel point rewards, along with their in game effects, via the configuration file, they will be automatically added to your channel for you
- Define bot commands for non channel points interactions
- Provides an interface for other mods to register action effects and command handlers
- Comes with the Adopt a Hero module

## Adopt a Hero
This is the first example action suite that comes with the mod (more to come hopefully).
Viewers can "adopt" and in-game hero of types that can be specified in the config -- this will give the in-game hero the viewers name, and allow further interactions with them:
- Upgrade battle equipment, civilian equipment, and horse.
- Buy skill points, attribute points, focus points
- Summon to the player when they are in missions (including battle, siege, arena, village and town)
- Call commands to show their health, gold, last known location, skills, attributes, battle and civilian equipment

# Instructions

## Installation
1. Install [Bannerlord Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006?tab=files)
2. Unzip to the Bannerlords Modules directory (by default at `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules`).
   It should create the `BannerlordTwitch` directory, and the `BannerlordTwitch.dll` should be at `Modules\BannerlordTwitch\bin\Win64_Shipping_Client\BannerlordTwitch.dll`
3. Copy `BannerlordTwitch\Bannerlord-Twitch.jsonc` to `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs` (this location is for Windows 10, for other OSes you should find out the equivalent location).
4. Edit the copy of the file to your requirements, it contains instructions, and documentation for the configuration sections.
5. Make sure Harmony loads first and Bannerlord Twitch loads after the game modules and before any BLT extensions.  
6. Run the game.
7. During startup watch for notification messages that indicate if the mod initialized successfully and connected to your Twitch channel.
8. Once you get to the main menu in game it should be initialized, and the Channel Rewards should have been created automatically.
9. If you have problems you can search for `[BLT]` lines in the `rgl_log` files at `C:\ProgramData\Mount and Blade II Bannerlord\logs`

If you have problems you can ping me (billw#7855) in the [TW modding discord](https://discord.gg/hqKcnSNfb6).

# Writing an Extension
You can implement new reward actions and commands quite easily:
1. Make another mod that depends on this one
2. Implement new classes derived from `IRedemptionAction` and/or `IBotCommand`
3. Register instances of your derived classes with the `RewardManager`

Rewards that have an `ActionId` that matches the class name of your `IRedemptionAction` will be passed to your registered instance, along with the `ActionConfig` specified.  
Commands work the same, with `Handler` matching the class name of your `IBotCommand`, and the `HandlerConfig` section being passed.