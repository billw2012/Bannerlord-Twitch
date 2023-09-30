# [Trailer](https://youtu.be/mFDuIjnoTQ0) | [Download](https://github.com/billw2012/Bannerlord-Twitch/releases) | [Discord](https://discord.gg/q2p4eHsxFn) | [Github](https://github.com/billw2012/Bannerlord-Twitch) | [Installation Guide](https://youtu.be/ATf5zilwNWk)

# Bannerlord Twitch (BLT)
This is a modification for [Mount & Blade II: Bannerlord](https://www.taleworlds.com/en/Games/Bannerlord) that adds Twitch integration to the game. This allows events in a Twitch stream to trigger actions in game, for instance redemption of Channel Point Rewards, or specific chat messages.

# Features
- **Define Channel Point Rewards**, along with their in game effects (using the provided custom built configuration UI), they will be automatically added to your channel for you, and removed again when the game exits
- **Define Bot Commands** for non Channel Points interactions, with optional limits such as subscriber only commands 
- Provides an **extensibility framework** allowing for other mods to register action effects and command handlers
- Comes with the **Adopt a Hero module**, allowing viewers to "adopt" an in-game hero, improve them, and perform actions with them in game
- Comes with the **BLT Buffet module**, allowing viewers to perform various actions to spawned agents in game (the player, friendlies, enemies) such as temporary stat changes, attached particle effects, triggering sounds, scaling the character up or down.

## Adopt a Hero
This is the first example action suite that comes with BLT.
Viewers can "adopt" an in-game hero of types that can be specified in the config -- this will give the in-game hero the viewers name, and allow further interactions with them:
- Upgrade battle equipment, civilian equipment, and horse
- Buy skill points, attribute points, focus points
- Summon to the player when they are in missions (including battle, siege, arena, village and town), on the player or enemies side
- Win / lose gold at the end of battles or fights, depending on the outcome
- Queue to join the next tournament the player starts 
- Call commands to show their health, gold, last known location, skills, attributes, equipment

## BLT Buffet
This is the second example action suite that comes with BLT.
So far it contains an action/handler called CharacterEffect that is aimed at performing temporary changes to agents that are spawned in missions (i.e. in battle, siege, village, town walkabouts etc., NOT on the map view).  
The changes can only last until the end of the mission at most, and can be limited by time also.  
Possible changes:
- Many agent stats, including things like swing speed, run speed, mount speed, armor, shield skill, courage, etc. [Full list here](https://raw.githubusercontent.com/billw2012/Bannerlord-Twitch/main/BannerlordTwitch/BLTBuffet/CharacterEffectProperties.txt)
- Agent scaling: make giants or dwarves!
- Apply damage or healing over time
- Force the agent to drop their weapons and be unable to pick any up
- Remove an agents armor
- Apply a damage multiplier to all the agents hits
- Play particles and sounds at the start and end of the effect, [full particle list here](https://raw.githubusercontent.com/billw2012/Bannerlord-Twitch/main/BannerlordTwitch/BLTBuffet/ParticleEffects.txt), [full sound list here](https://raw.githubusercontent.com/billw2012/Bannerlord-Twitch/main/BannerlordTwitch/BLTBuffet/Sounds.txt)
- Attach particles to the agent, their weapon, head, hands, or everywhere on their body

# Instructions

## Installation

### [Installation Guide Video](https://youtu.be/ATf5zilwNWk)

1. Install [Bannerlord Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006?tab=files) and [ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018?tab=files).
   
2. Unzip to the Bannerlord Modules directory (by default at `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules`).
   It should create the `BannerlordTwitch` directory, and the `BannerlordTwitch.dll` should be at `Modules\BannerlordTwitch\bin\Win64_Shipping_Client\BannerlordTwitch.dll`
   ![image](https://user-images.githubusercontent.com/1453936/115397098-9daae880-a1dd-11eb-87c7-0bda9af4c79d.png)
   It should also create the `BLTAdoptAHero`, `BLTBuffet`, and `BLTConfigure` directories.
   
3. Run the launcher, make sure Harmony loads first and Bannerlord Twitch loads after the game modules and before any BLT extensions:  
   ![image](https://user-images.githubusercontent.com/1453936/116240320-95155d80-a75b-11eb-8920-6e0629ab81b9.png)
   
4. Run the game.
   
5. The mod should popup in game messages to indicate that it requires authorization and is disabled.
   
6. Tab out of the game and use the BLT Configure window to Authorize:
   1. Click the Authorize button on the Auth tab
   2. It should open a new browser window or tab, showing a twitch authorization page (you may need to sign in to twitch first)
   3. Click the authorize button at the bottom of the page
   4. You should get a confirmation message, after which you can close the browser and go back to the BLT Configure window, which should now display Authorized in green if it was successful
 
7. Close and then restart the game.  
8. During startup watch for notification messages in the BLT Overlay window, that indicate if the mod initialized successfully and connected to your Twitch channel.
9. Once you get to the main menu in game it should be initialized, and the default Channel Rewards should have been created automatically. You should also see the bot in your twitch channel.

## Trouble Shooting   
If you have problems you can search for `[BLT]` lines in the `rgl_log` files at `C:\ProgramData\Mount and Blade II Bannerlord\logs`. I added logging for everything so you should see failures and critical errors in here.

If you need help then join the [Discord](https://discord.gg/q2p4eHsxFn).

# Developer Guide
This guide only explains things specific to developing the BLT mod, if there are other words, concepts or procedures you don't understand mentioned here then you should google them!

## Requirements
You need Bannerlord installed to be able to build or run this mod.

You also need 7zip installed and available on the path environment variable. 

The project uses a number of NuGet packages, these should be installed automatically by your build system when you try to compile, debug or run the mod via your IDE. If they aren't then check you have this feature enabled, and/or use your IDEs NuGet package manager to run a package restore manually. Until package restore has been done (automatically or manually), the solution will likely show many "intellisense" errors related to missing references. Once packages are restored these should go away (it may take a while or require an IDE restart though). If some still remain then you may have a different version of the game dlls than the mod supports, see the Setup instructions below for more details.

## Developer Setup
1. Clone the repo locally.
2. Setup the `BANNERLORD_GAME_DIR` property, either as an environment variable (if you want to avoid modifying a repo file), or directly in the `BLTProperties.targets` file at line 31 (you can see there where it should point to). This value is used to allow the project to directly reference your installed Bannerlord dlls, and to enable deployment and debugging.
3. Ensure the `GameVersion` property in `BLTProperties.targets` at line 29 matches the game version installed. If it doesn't then either you intend to update the mod to support a newer version of the game (which is an advanced operation*), or you don't and should switch the installed game beta in steam to the match the value.
4. Try running the default "Bannerlord" debugging configuration. This configuration is specified at lines 29 to 34 of the `BannerlordTwitch.csproj` project file. You can see there that it uses the `BANNERLORD_GAME_DIR` property, and specifies an explicit module list that includes only the main game and BLT modules. Any mods you have installed will not be loaded when you are debugging BLT in this manner, if you want them then add them to the commandline here, or via your IDEs debugging configuration editor. 
5. The mod should now be compiled (including restoring required NuGet packages), deployed to your game directory (replacing any BLT files that already exist), then run with the debugger attached.

## Updating for a New Game Version
This is potentially an advanced operation, if any of the hooked functions have changed or been removed then it requires an understanding of function hooking with Harmony, what the previously used function did, and how it can be replaced or its functionality replicated (requiring deep understanding of the mod, and how to view the implementation of the method that was removed).

Then again if nothing the mod depends on changed, then its a trivial operation involving only updating the version numbers!

You won't necessarily know if something has changed until you run the mod, the function hooking is done during startup, and the log will show if there were any hooking errors. Even if there aren't hooking errors there is still a small chance that the meaning of a function parameter, of the functions behaviour itself, changed, without changing the function signature (which is what hooking relies on).  Detecting this is most easily done by in game testing: exceptions, crashes, weird behavior might be explained by this. 

1. Update the version number in `BLTProperties.targets` at line 29.
2. Update the version number in `BannerlordTwitch\BLTModule.cs` at line 32.
3. Debug the mod using the provided debug configuration.
4. Once at the main menu, or crashed, check the log file for hooking errors.
5. If you have hooking errors and don't know where to start with addressing them then you are probably done, either go back to using an older game version and make the changes you want without updating the mod game version, or get ready to learn some .NET black magic, starting with what Harmony is.

## Sharing a New Build
First you should make sure to update the `ModuleVersion` property in `BLTProperties.targets` at line 4. This uses an approximation of [semver versioning](https://semver.org/). Basically if you are updating only for a new game version or making a small tweak that doesn't suggest starting a new campaign, then increment the "patch" version, e.g., `1.1.1` becomes `1.1.2`. If the change is large enough that starting a new campaign would make sense then increment the "minor" version, e.g., `1.1.1` becomes `1.2.0`. Finally for large changes that would require a new campaign, or significantly change or add to the experience, you should increment the "major" version, e.g., `1.1.1` becomes `2.0.0`. Note that the smaller parts of the version are reset to zero when a higher part is changed.

The Release config build process generates a sharable package which will be found in the `BannerlordTwitch\deploy\release` under the cloned repo. It should include the game and updated mod version number. This package can be released as is through appropriate channels.

It would be preferable, although not required as per the MIT license, that 

## Writing an Extension
You can implement new reward actions and command handlers quite easily:
1. Make another mod that depends on `BannerlordTwitch` (in the `Submodule.xml`, AND reference the dll itself)
2. Implement a new class derived from `IActionHandler` (for channel point rewards), `ICommandHandler` (for bot commands) or `ActionAndCommandBase` (to make both in one class)
3. Register instances of your derived classes with the `RewardManager`, this can be done easily with a call to:
   ```c#
   RewardManager.RegisterAll(typeof(your module class).Assembly);
   ```

Example:
```c#
// Command handler to allow playing a sound effect in game, passed as the argument to the command itself
public class PlaySfx : ICommandHandler
{
    public void Execute(ReplyContext context, object config)
    {
        // Play the specified sound on the main agent
        if (!string.IsNullOrEmpty(context.Args) && Agent.Main != null)
        {
            Mission.Current.MakeSound(
                SoundEvent.GetEventIdFromString(context.Args),
                Agent.Main.AgentVisuals.GetGlobalFrame().origin,
                false, true, Agent.Main.Index, -1);
        }   
    }

    // We don't need any config for this
    public Type HandlerConfigType => null;
}
```

Rewards that have a `Handler` that matches the class name of your `IActionHandler` will be passed to your registered instance, along with the `HandlerConfig`.  
Commands work the same, with `Handler` matching the class name of your `ICommandHandler`.

Once compiled into a module and installed in the game, your handlers should show up in the BLT Configure tool (make sure the tool is loaded after your modules).

See the `BLTBuffet` and `BLTAdoptAHero` projects for some more examples.


