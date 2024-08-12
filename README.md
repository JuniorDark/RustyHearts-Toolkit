# Rusty Hearts Toolkit

[![License](https://img.shields.io/github/license/JuniorDark/RustyHearts-Toolkit?color=green)](LICENSE)  
[![Build](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/JuniorDark/RustyHearts-Toolkit/actions/workflows/build.yml)  
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/JuniorDark/RustyHearts-Toolkit)](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest)

## Overview

**Rusty Hearts Toolkit** is a set of tools designed for editing Rusty Hearts game files and database. Whether you're looking to customize characters, manage in-game items, or edit game files, this toolkit offers a variety of features to assist in the process.

## Preview
<!-- Include screenshots later here to provide a visual overview of the toolkit. -->

## Features

### Database Editing Tools
- **Character Edit Tools:** Modify character such as equipment, inventory, storage, fortune, sanctions, and titles.
- **Coupon Generator:** Create in-game coupons.
- **Send Mail:** Send in-game mail with customizable item attachments.
- **Mail Templates:** Save and reuse mail templates in JSON format.

### File Editing Tools
- **Table Editor:** Edit `.rh` table files and export them to various formats, including XML, XLSX, and MIP.
- **Specialized Table Editors:**
  - **Cash Shop Editor:** Manage in-game cash shop items and packages.
  - **Package Editor:** Manage and edit item packages and effects.
  - **Random Rune Editor:** Manage and edit 'gacha' items.
  - **Set Editor:** Edit item sets and their effects.

### Database Tools
- **Item Database:** View detailed item information in an in-game-like tooltip. Sort items with various filters and search by name or ID.

## Getting Started

To start using the Rusty Hearts Toolkit, download the latest release from the [GitHub repository](https://github.com/JuniorDark/RustyHearts-Toolkit/releases/latest).

### Language Settings

The toolkit curently supports 2 languages. You can change the language in the `Settings` page.

#### Available Languages
- **English (en-US)** - Default language
- **Korean (ko-KR)** - Machine translated

## Setup Guide

To set up the toolkit and generate the necessary database:

1. Navigate to the `SQLite Database Manager` page.
2. Select the folder containing the `.rh` table files.
3. Click `Create Database` to generate the `gmdb_(Lang).db` database in the `Resources` folder.
4. Place the required icons in the `Resources` folder. Icons are available in [`Resources.rar`](Resources.rar).

## Building from Source

If you wish to build locally or contribute to its development, ensure that you have the following prerequisites:

### Prerequisites
- **Visual Studio 2022 (17.9 or later)** - Any edition
- **Windows 10 SDK** or **Windows 11 SDK** via Visual Studio Installer
- **.NET Core 8 SDK (8.0.107 or later)** - [Download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### System Requirements
- **OS:** Windows 10 (build 19045) or later / Windows 11
- **Architecture:** x64/AMD64

## License

This project is licensed under the BSD 2-Clause License. See the [`LICENSE`](LICENSE) file for details.

## Frequently Asked Questions

**Q: How do I report a bug?**  
A: Submit an issue on GitHub with a detailed description of the bug and steps to reproduce it.

**Q: How do I request a new feature?**  
A: Submit an issue on GitHub with a description of the feature and its potential benefits.

**Q: How do I contribute?**  
A: Fork the repository, make your changes, and submit a pull request.

## Credits

This project utilizes the following NuGet packages:

- [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)
- [EPPlus](https://www.nuget.org/packages/EPPlus)
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)
- [Microsoft.Xaml.Behaviors.Wpf](https://www.nuget.org/packages/Microsoft.Xaml.Behaviors.Wpf)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
- [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core)
- [WPF-UI](https://www.nuget.org/packages/WPF-UI/)