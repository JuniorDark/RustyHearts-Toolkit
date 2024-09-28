# Rusty Hearts Toolkit

[![License](https://img.shields.io/github/license/JuniorDark/RustyHearts-Toolkit?color=green)](LICENSE)  
[![Build](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml)  
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/JuniorDark/RustyHearts-Toolkit)](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest)

## Overview

**Rusty Hearts Toolkit** is a set of GUI tools designed for editing Rusty Hearts game files and database.

## Preview
<!-- Include screenshots later here to provide a visual overview of the toolkit. -->

## Getting Started

To start using the Rusty Hearts Toolkit, download the latest release from the [GitHub repository](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest).

## Current Features

### Database Editing Tools
- **Character Edit Tools:** Modify character such as equipment, inventory, storage, fortune, sanctions, and titles.
- **Coupon Generator:** Create redeem in-game coupons code.
- **Send Mail:** Send in-game mail with customizable item attachments.
- **Mail Templates:** Save and reuse mail templates in JSON format.

### Table Editor Tools
- **Table Editor:** Edit `.rh` table files on the go and export them to various formats, including XML, XLSX, and MIP.
- **Specialized Table Editors:**
- **Cash Shop Editor:** Cash shop items (cashshoplist.rh).
- **Item Editor:** Items (itemlist.rh, itemlist_armor.rh, itemlist_costume.rh, itemlist_weapon.rh).
- **Item Drop Group Editor** Item drops (itemdropgrouplist_f.rh, itemdropgrouplist.rh, championitemdropgrouplist.rh, eventworlditemdropgrouplist.rh, instanceitemdropgrouplist.rh, questitemdropgrouplist.rh, worldinstanceitemdropgrouplist.rh, worlditemdropgrouplist.rh, worlditemdropgrouplist_fatigue.rh, riddleboxdropgrouplist.rh, rarecarddropgrouplist.rh, rarecardrewarditemlist.rh).
- **NPC Editor:** (npcinstance.rh).
- **NPC Shop Editor** Npc Shop (npcshop.rh), Trade Shop (tradeshop.rh), Item Craft (itemmix.rh, costumemix.rh), shop item visible filter (shopitemvisiblefilter.rh).
- **Package Editor:** Item packages and effects (unionpackage.rh).
- **Random Rune Editor:** Edit 'gacha' items (randomrune.rh).
- **Set Editor:** Edit item sets and their effects (setitem.rh).
- **Title Editor:** Edit titles and their effects (charactertitle.rh).

### Language Settings

The toolkit curently supports 2 languages. You can change the language in the `Settings` page.

#### Available Languages
- **English (en-US)** - Default language
- **Korean (ko-KR)** - Machine translated

## Setup Guide

To set up the toolkit and generate the necessary SQLite database:

1. Navigate to the `SQLite Database Manager` page.
2. Select the folder containing the `.rh` table files.
3. Click `Create Database` to generate the `gmdb_(Lang).db` database in the `Resources` folder.
4. Place the extracted game sprites `\ui\sprite\1024` in the `Resources` folder.
5. Navigate to the `Settings` page and set the `SQL Server` credentials.


## Prerequisites for Development
* Visual Studio 2022 (Any Edition - 17.9 or later)
* Windows 10 SDK or Windows 11 SDK via Visual Studio Installer
* .NET Core 8 SDK (8.0.107 or later)

## Building

If you wish to build the project yourself, follow these steps:

### Step 1

Install the [.NET 8.0 (or higher) SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
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

## Frequently Asked Questions

**Q: How do I report a bug?**  
A: Submit an issue on GitHub with a detailed description of the bug and steps to reproduce it.

**Q: How do I request a new feature?**  
A: Submit an issue on GitHub with a description of the feature and its potential benefits.

**Q: How do I contribute?**  
A: Fork the repository, make your changes, and submit a pull request.

## Road Map

### SQLite Databases
- **Portable Databases:** Enemy/Item/Skills.

### Database Tools
- **Character Skills**
- **Character Quests**
- **Character Pets**

### Specialized Table Editors
- **Add Effect Editor**
- **Enemy Editor**
- **Pet Editor**
- **Quest Editor**
- **Skill Editor**
- **World/Map Editor**

## Credits

This project is possible due to the following NuGet packages:

- [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)
- [DotNetZip](https://www.nuget.org/packages/dotnetzip)
- [EPPlus](https://www.nuget.org/packages/EPPlus)
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)
- [Microsoft.Xaml.Behaviors.Wpf](https://www.nuget.org/packages/Microsoft.Xaml.Behaviors.Wpf)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
- [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core)
- [WPF-UI](https://www.nuget.org/packages/WPF-UI/)