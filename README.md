# Rusty Hearts Toolkit
[![License](https://img.shields.io/github/license/JuniorDark/RustyHearts-Toolkit?color=green)](LICENSE)
[![Build](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/JuniorDark/RustyHearts-Toolkit)](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest) <a href="https://github.com/JuniorDark/RustyHearts-Toolkit/releases">

# Rusty Hearts Toolkit

A collection of tools for editing the Rusty Hearts game files/database.

## Preview


## Getting Started
To begin using this tool, download the latest release from the [GitHub repository](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest).

## Current Features
Please note that this tool is still in development and more features are planned.
- **Edit Character:** Edit Character informations.
- **Portable Item Database:** View items in a user-friendly datagrid. Selecting an item provides detailed information displayed in a in-game similar tooltip, and items can be sorted using various filters. The search functionality allows find items by name or ID.
- **Send Mail:** Send in-game mail to characters and attach any item with custom stats.
- **Mail Templates:** Save mail configurations to JSON templates for future use. This allows for quick and easy loading of predefined mail settings.

## Setup
The tool relies on a Sqlite database (`gmdb.db`). Follow these steps to set up and generate a new database:

1. Run `CreateGMDatabase` by placing the necessary XLSX table files in the `xlsx` folder. These files are used to create a new database. Move the generated `gmdb.db` to the `Resources` folder in the program directory.

   Required table files in xlsx format:
   - angelaweapon.rh
   - exp.rh
   - fortune.rh
   - frantzweapon.rh
   - itemcategory.rh
   - itemlist.rh
   - itemlist_string.rh
   - itemlist_armor.rh
   - itemlist_armor_string.rh
   - itemlist_costume.rh
   - itemlist_costume_string.rh
   - itemlist_weapon.rh
   - itemlist_weapon_string.rh
   - itemoptionlist.rh
   - natashaweapon.rh
   - nick_filter.rh
   - serverlobbyid.rh
   - setitem.rh
   - setitem_string.rh
   - tudeweapon.rh

2. Place item icons in the `Resources` folder.

A prebuild database and icons are avaliable on [`Resources.rar`](Resources.rar)

## Prerequisites for Building Locally/Development
The tool uses .NET 8 and as such, the packages listed below are required to create a local and development build of the tool. Furthermore, it uses many submodules and packages outside of this, which will automatically be loaded when the user sets up a local environment of the application.
* Visual Studio 2022 (Any Edition - 17.9 or later)
* Windows 10 SDK (10.0.19043.0) or Windows 11 SDK (10.0.22000.0) via Visual Studio Installer
* .NET: [.NET Core 8 SDK (8.0.203 or later)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## System Requirements for Ready-to-use build
* OS: Windows 10 1809 Update (build 17763) or later / Windows 11 (Any builds)
* Architecture: x64/AMD64

## License
This project is licensed under the terms found in [`LICENSE-0BSD`](LICENSE).

## Contributing
Contributions from the community are welcome! If you encounter a bug or have a feature request, please submit an issue on GitHub. If you would like to contribute code, please fork the repository and submit a pull request.

## FAQ
* Q: How do I report a bug?
  * A: Please submit an issue on GitHub with a detailed description of the bug and steps to reproduce it.
* Q: How do I request a new feature?
  * A: Please submit an issue on GitHub with a detailed description of the feature and why it would be useful.
* Q: How do I contribute code?
  * A: Please fork the repository, make your changes, and submit a pull request.

## Credits
The following nuget packages are used in this project:
* [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)
* [EPPlus](https://www.nuget.org/packages/EPPlus)
* [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient)
* [Microsoft.Extensions.DependencyInjectio](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
* [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core)

## Support
If you need help with the tool, please submit an issue on GitHub.
