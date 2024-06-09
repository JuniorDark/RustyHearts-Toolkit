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
- **Database Editor:** Edit Character informations.
- **Send Mail:** Send in-game mail to characters and attach any item with custom stats.
- **Mail Templates:** Save mail  to JSON templates for future use.

- **Item Database:** View items in a user-friendly datagrid. Selecting an item provides detailed information displayed in a in-game similar tooltip, and items can be sorted using various filters. The search functionality allows find items by name or ID.


## Setup
The tool relies on a Sqlite database (`gmdb_(Lang).db`). Follow these steps to set up and generate a new database:

1. Go to `SQLite Database Manager` page and select the folder with `.rh` table files and click `Create Dabatase`. It will create the database with the required tables.

A prebuild database and icons are avaliable on [`Resources.rar`](Resources.rar)

## Language
The language can be changed on `Settings` page.

### Avaliable Languages
* **en** - English (en-US) default language
* **ko** - Korean ("ko-KR) (Machine Translated)

## Prerequisites for Building Locally/Development
The tool uses .NET 8 and as such, the packages listed below are required to create a local and development build of the tool. Furthermore, it uses many submodules and packages outside of this, which will automatically be loaded when the user sets up a local environment of the application.
* Visual Studio 2022 (Any Edition - 17.9 or later)
* Windows 10 SDK or Windows 11 SDK via Visual Studio Installer
* .NET: [.NET Core 8 SDK (8.0.301 or later)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## System Requirements for Ready-to-use build
* OS: Windows 10 (build 19045) or later / Windows 11
* Architecture: x64/AMD64

## License
This project is licensed under the terms found in [`LICENSE-0BSD`](LICENSE).

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
* [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
* [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)
* [Microsoft.Xaml.Behaviors.Wpf](https://www.nuget.org/packages/Microsoft.Xaml.Behaviors.Wpf)
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
* [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core)
* [WPF-UI](https://www.nuget.org/packages/WPF-UI/)
