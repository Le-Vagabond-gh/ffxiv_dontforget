## Don't Forget
A Dalamud plugin that automates common actions you always forget to do in FFXIV.

**Author:** Le Vagabond (forked from Spooee's original work)

## Features
- **Auto Peloton** - Automatically uses Peloton when moving (Physical Ranged jobs only)
- **Auto Sprint** - Automatically uses Sprint when moving (any job)
- **Auto Gysahl Greens** - Automatically feeds your chocobo companion when its timer falls below 15 minutes
- **Summon Fairy** - Automatically summons your fairy when standing still (Scholar)
- **Summon Carbuncle** - Automatically summons your carbuncle when standing still (Summoner)
- **Summon in Combat (After Death)** - Re-summons your pet in combat after being raised
- **Auto Tank Stance** - Automatically enables tank stance when standing still (PLD, WAR, DRK, GNB)
- **Gatherer Auto Buffs** - Automatically enables Prospect, Triangulate, and Sneak when standing still (Miner, Botanist)
- **Gatherer Auto Switch** - Automatically switches to the correct gatherer class when you try to gather from the wrong node type (requires a gearset saved for each gatherer class)

## Installation
- Download the DLL and manifest JSON from [Releases](https://github.com/Le-Vagabond-gh/ffxiv_dontforget/releases) in the same location
- Open the Dalamud Plugin Installer
- Go to Settings
- Head to the "Experimental" tab
- Under "Dev Plugin Locations", click "Select dev plugin DLL"
- Add the DLL you downloaded
- Press "Save and Close"
- In the main plugin installer window, enable the plugin in Dev Tools

Note: adding custom repositories to Dalamud is a security risk, this way protects you from malicious updates from untrusted sources.

## Usage
Type `/dontforget` or `/df` in chat to open the configuration window.

### Commands
- `/df` - Open the configuration window
- `/df tankstance` - Toggle auto tank stance on/off (useful for macros)
