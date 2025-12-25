# Rusty Hearts Toolkit

[![License](https://img.shields.io/github/license/JuniorDark/RustyHearts-Toolkit?color=green)](LICENSE.txt)  
[![Build](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml)  
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/JuniorDark/RustyHearts-Toolkit)](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest)

## Overview

**Rusty Hearts Toolkit** is a comprehensive suite of graphical tools created for in-depth editing of *Rusty Hearts* game files and databases, enabling users to manage and customize various in-game elements efficiently.

## Getting Started

To start using Rusty Hearts Toolkit, download the latest release from the [GitHub repository](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest).

## Features

### Database Editing Tools
- **Character Edit Tools**: Customize character attributes, including equipment, inventory, storage, fortune, sanctions, and titles.
- **Coupon Generator**: Generate unique in-game coupon codes for redeemable items rewards.
- **Send Mail**: Send in-game mail with customizable item attachments. Save mail as templates in JSON format for easy reuse.

### PCK Tools
- **PCK Tools**: A specialized tool designed for working with [.pck](https://juniordark.gitbook.io/rusty-hearts-files-structures/external-files/pck-file) files, which are the primary container format for Rusty Hearts, containing almost all of the asset files for the game.
- Read and unpack `.pck` files to extract game assets.
- Pack and write assets back into `.pck` files.

### File Editor Tools
- **WData Editor**: A specialized tool designed for editing [.wdata](https://juniordark.gitbook.io/rusty-hearts-files-structures/wdata-file) files, which are binary containers that encapsulate all runtime data for a map, packaging map resources and configurations into a single versioned file.

### Model Tools
- **Model Tools**: A specialized tool designed for exporting 3D models map files (`.mmp`) and models (`.mgm`) to fbx. 
- **Model Viewer**: View 3D models (`.mmp`,`.mgm`), with option to export to FBX format and import fbx back to  (`.mmp`).

### Table Editor Tools
- **Table Editor**: Edit [.rh](https://juniordark.gitbook.io/rusty-hearts-files-structures/internal-files/rh-file) table files directly, with options to export into various formats like XML, XLSX, and MIP.
  
- **Dedicated Table Editors**:
  - **AddEffect Editor**: Edit buffs and effects (addeffect.rh).
  - **Cash Shop Editor**: Edit in-game cash shop items (cashshoplist.rh).
  - **Enemy Editor**: Edit enemy stats, attributes, and more (enemy.rh).
  - **Item Editor**: Edit item lists, including armor, costumes, and weapons (itemlist.rh, itemlist_armor.rh, itemlist_costume.rh, itemlist_weapon.rh).
  - **Item Drop Group Editor**: Edit item drop rates and groups across multiple files (itemdropgrouplist_f.rh, itemdropgrouplist.rh, championitemdropgrouplist.rh, eventworlditemdropgrouplist.rh, instanceitemdropgrouplist.rh, questitemdropgrouplist.rh, worldinstanceitemdropgrouplist.rh, worlditemdropgrouplist.rh, worlditemdropgrouplist_fatigue.rh, riddleboxdropgrouplist.rh, rarecarddropgrouplist.rh, rarecardrewarditemlist.rh).
  - **NPC Editor**: Edit NPC data (npcinstance.rh).
  - **NPC Shop Editor**: Edit NPC shop inventories, item crafting lists (itemmix.rh, costumemix.rh), shop visibility filters, and item dismantling rules (itembroken.rh).
  - **Package Editor**: Edit item packages and define package-specific effects (unionpackage.rh, conditionselectitem.rh).
  - **Pet Editor**: Edit pet characteristics (pet.rh).
  - **Quest Editor**: Edit quests requirements, rewards, and objectives.
  - **Random Rune Editor**: Edit 'gacha' style items pools (randomrune.rh).
  - **Set Editor**: Edit item sets and their effects (setitem.rh).
  - **Skill Editor**: Edit and configure character skills.
  - **Title Editor**: Edit titles and title effects (charactertitle.rh).
  - **World Editor**: Edit world data (world.rh, dungeoninfolist.rh, mapselect_curtis.rh).

## FBX → MMP (EXPERIMENTAL) Import Support
IMPORTANT
- This feature is experimental and intended for advanced users and expects FBX files produced by this toolkit’s exporter.
- This feature may produce incomplete or incorrect results, always back up original MMP, NAVI files before importing.

### IMPORTANT: Materials
- On export, the toolkit generates a `.materials.json` sidecar file containing material parameters.
- The importer uses this `.materials.json` to reconstruct materials in the `.mmp`.
- Many material parameter meanings are currently unknown, so round-tripping relies on preserving these values.

### IMPORTANT: Node Names (Required Suffix Flags)

Mesh nodes in the exported FBX include a special suffix that encodes flags required to rebuild the `.mmp`. **Do not remove or rename these suffixes**.

Suffix format:
- `__K{kind}`
- `__T{meshType}_A{IsAdditive}_B{hasAlpha}_E{isEnabled}_M{materialIdx}`

Examples:
- `NOF_wreck_cave_01__K5`
- `ship_1__T0_A0_B1_E1_M41`

### Importing Meshes/Materials From Other Maps

When importing meshes/materials from other maps, you must update:
- The `materialIdx` flag in FBX node names (`_M{materialIdx}`)
- The material indices inside `.materials.json` to avoid index collisions

### Workflow (Blender)

#### 1) Export the source MMP to FBX
- In the MMP exporter, enable **Export Separate Objects**.
  - This produces a separate FBX per mesh node.

#### 2) Bring additional meshes into a target map scene
- In Blender, import the target map FBX.
- Import the additional mesh FBX files.
- Move/parent imported mesh nodes under the target map’s main/root node as needed.

#### 3) Merge materials
- Open the imported mesh `.materials.json` and copy its materials to the end of the target map `.materials.json`.
- Update the new materials’ `MaterialIndex` values to avoid duplicates.
- Update each imported mesh node name `_M{materialIdx}` to match the new `MaterialIndex`.

#### 4) Fix texture paths
Update `TexturePath` to be correct relative to the target map’s `texture` folder.

Example:
- Target map: `\map\lobby\palme`
- Source map: `\map\event_dungeon\wreck_cave`

Source `TexturePath` in `wreck_cave`:
- `"TexturePath": ".\\texture\\wreckrock_08.dds"`

Updated `TexturePath` for `palme`:
- `"TexturePath": "..\\..\\event_dungeon\\wreck_cave\\texture\\wreckrock_08.dds"`

#### 5) Export FBX from Blender
- Export the scene to FBX using Blender default settings.

#### 6) Import FBX back to MMP
- In the 3D Model Viewer window, use **Import Map FBX to MMP**.
- The import generates:
  - a new `.mmp`
  - a `.navi` for the updated navigation mesh
  - a `.height` (height map generated from the `.navi`)


### Local Databases
- **Item Database** 
- **Item Craft Database**
- **Item DropGroup Database**
- **Skill Database** 

## Preview

<details>
  <summary>Click to expand preview images</summary>
  
  ![image](preview/preview01.png)
  ![image](preview/preview02.png)
  ![image](preview/preview03.png)
  ![image](preview/preview04.png)
  ![image](preview/preview05.png)
  ![image](preview/preview06.png)
  ![image](preview/preview07.png)
  ![image](preview/preview08.png)
  ![image](preview/preview09.png)
  ![image](preview/preview10.png)
  ![image](preview/preview11.png)
  ![image](preview/preview12.png)
  ![image](preview/preview13.png)
  ![image](preview/preview14.png)
  
</details>

### Language Settings

The Rusty Hearts Toolkit curently supports 2 languages, the language can be changed in the `Settings` page.

#### Available Languages
- **English (en-US)** - Default language
- **Korean (ko-KR)** - Machine translated

## Setup Guide

To set up the toolkit and generate the necessary SQLite database:

1. Navigate to the `SQLite Database Manager` page.
2. Select the `table` folder containing the `.rh` table files.
3. Click `Create Database` to generate the `gmdb_(Lang).db` database in the `Resources` folder.
4. Place the extracted game sprites `\ui\sprite\` in the `Resources` folder (Included by default).
5. Navigate to the `Settings` page and set the `SQL Server` credentials.

Note: After editing tables remember to rebuild the sqlite database on `SQLite Database Manager`.

Sprites are avaliable on [`Resources.zip`](Resources.zip)

## Prerequisites for Development
* Visual Studio 2026 (Any Edition - 18.0 or later)
* Windows 11 SDK via Visual Studio Installer
* .NET 10 SDK (10.0.100 or later)

## Building

If you wish to build the project yourself, follow these steps:

### Step 1

Install the [.NET 10.0 (or higher) SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
Make sure your SDK version is higher or equal to the required version specified.

### Step 2

Either use `git clone https://github.com/JuniorDark/RustyHearts-Toolkit` on the command line to clone the repository or use Code --> Download zip button to get the files.

### Step 3

To build Rusty Hearts Toolkit, open a command prompt inside the project directory.
You can quickly access it on Windows by holding shift in File Explorer, then right clicking and selecting `Open command window here`.
Then type the following command: `dotnet build -c Release` or using `dotnet cake` script.
 
The built files will be found in the newly created `bin` build directory.

## License

This project is licensed under the BSD 2-Clause License. See the [`LICENSE`](LICENSE.txt) file for details.

## Credits

This project is possible due to the following NuGet packages:

- [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)
- [EPPlus](https://www.nuget.org/packages/EPPlus)
- [HelixToolkit](https://www.nuget.org/packages/HelixToolkit)
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)
- [Microsoft.Xaml.Behaviors.Wpf](https://www.nuget.org/packages/Microsoft.Xaml.Behaviors.Wpf)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
- [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core)
- [WPF-UI](https://www.nuget.org/packages/WPF-UI/)
