# Nebulous Community Highlander

## Why

Often, modding support for a game doesn't extend as far as the community for that game would like. Highlanders are special mods that do not add any new features or content to a game, but instead are focused on making it easier for other mods to be created or allowing mods to co-exist with each other. 

Highlanders have their name because the changes they make to a game's structure are often far-reaching and incompatible with other mods making similar changes, resulting in the community clustering around a single mod of this kind rather than having many such mods. There can only be one, thus, 'Highlander'.

## What

The NCH allows mods to easily copy basegame hull parts and edit the copies, which is not otherwise possible. The official method of modding the game through Unity requires any new content to be made from scratch or something close to scratch. It also requires you to learn Unity, which is more daunting than basic C# to some.

The NCH is fully integrated with the game's modding framework that ensures modded fleets are kept separate from basegame fleets, and allows everyone in a multiplayer lobby to quickly sync their mods with the host. However, all players in a multiplayer lobby must have the highlander itself installed or bad things will happen if you try to use a mod that requires the highlander.

If no mods that require the highlander are currently enabled, your Nebulous game experience will be entirely unchanged.

## How

1. Download BepInEx here: https://github.com/BepInEx/BepInEx/releases
2. Install BepInEx by unzipping it inside your game folder. The structure should be `steamapps/common/Nebulous/BepInEx/core`.
3. Run the game to the main menu, then quit. There should be more folders inside `Nebulous/BepInEx`.
4. Download the latest version of the Highlander from here: https://github.com/cannonfodder1/nebulous-community-highlander/releases
5. Place `CommunityHighlander.dll` inside `Nebulous/BepInEx/plugins`. All done!

The example mod, containing the Mk67 Autocannon and Mk601 Small Beam Turret, is available on the Steam Workshop. Please note that the Nebulous workshop is currently private, so you'll need to be logged into a Steam account that owns the game. Grab it here: https://steamcommunity.com/sharedfiles/filedetails/?id=2647917560
