# 🚀 Rusty Hearts Toolkit - Release Notes v1.4.0

## What's Changed

### 🎯 Major Features

#### [Comprehensive 3D Model Support](https://github.com/JuniorDark/RustyHearts-Toolkit/pull/57)

**MMP (Map) & MGM (Model) Processing**
- Added full support for reading and writing MMP and MGM binary file formats with memory-efficient parsing
- Implemented FBX export functionality with advanced vertex deduplication and material management
- Navigation mesh (.navi) support with dedicated `NaviReader` and `NaviWriter` classes
- Heightmap support with `HeightReader` and `HeightWriter` for navigation mesh processing
- New Model Tools page for batch export operations
- Interactive 3D viewer powered by HelixToolkit with skeleton/mesh toggles
- Texture embedding options during FBX export
- Animation support foundation with `DSReader` and `MAReader` classes (work in progress)

**New Classes Added**
```
Models/Model3D/
├── MGM/           MGMReader, MGMExporter, MGMToHelix
├── Map/           MMPReader, MMPWriter, MMPExporter, MMPToHelix
│                  NaviReader, NaviWriter, HeightReader, HeightWriter
├── Animation/     DSReader, MAReader (WIP)
└── Core/          ModelManager, ModelViewManager, ModelMaterial
```

**WData Integration Enhancements**
- Introduced `ObbOverlayManager` for managing 3D entity overlays and visualization
- Enhanced `WDataManager` with comprehensive scene management and MMP file loading capabilities
- Added `IObbEntity` interface for unified entity handling across the application

### 🔧 Infrastructure Updates

#### .NET 10 Upgrade
- Upgraded framework to `net10.0-windows` with SDK 10.0.100
- Updated Microsoft.Extensions.* packages from 9.0.7 → 10.0.0
- Updated CI/CD workflows to use actions/setup-dotnet@v5 and actions/upload-artifact@v5
- Full compatibility with latest .NET 10 features and improvements

#### Aspose.3D → SharpAssimp Migration
- Replaced proprietary Aspose.3D library with open-source SharpAssimp
- Removes proprietary licensing requirements and improves accessibility
- New dependencies: `HelixToolkit.Wpf.SharpDX` v3.1.1 and `HelixToolkit.SharpDX.Assimp` v3.1.1
- Maintains full feature parity while improving maintainability

#### Localization Improvements
- Implemented resource-based string management system replacing hardcoded strings
- Enhanced Korean (ko-KR) translations throughout the application
- Improved multilingual support infrastructure for future language additions
- Note: Korean translations are machine-generated and community review is recommended

### 🐛 Bug Fixes & Improvements

#### Memory Leak Fixes
- Implemented `IDisposable` pattern on `PCKWriter` and `PCKReader` to fix memory leaks
- Improved resource cleanup and disposal throughout PCK file handling operations

### 📦 Updated Dependencies

#### Build Tools
* Bump cake.tool from 5.1.0 to 6.0.0 by @dependabot in https://github.com/JuniorDark/RustyHearts-Toolkit/pull/58
  - Added .NET 10 (net10.0) TFM support
  - C# 14 scripting support
  - Improved performance and modernized code patterns

#### GitHub Actions
* Bump actions/checkout from 4 to 5 by @dependabot in https://github.com/JuniorDark/RustyHearts-Toolkit/pull/41
  - Updated to use Node 24 for better compatibility
  - Minimum runner version: v2.327.1

#### .NET Packages
* Bump Microsoft.Data.SqlClient from 6.0.2 to 6.1.0 by @dependabot in https://github.com/JuniorDark/RustyHearts-Toolkit/pull/40
  - Added dedicated SQL Server vector datatype support
  - Revived .NET Standard 2.0 target support
  - Significant performance improvements for vector operations
  - Packet multiplexing support for improved large data read performance

* Bump dotnet-sdk from 9.0.302 to 9.0.303 by @dependabot in https://github.com/JuniorDark/RustyHearts-Toolkit/pull/39
  - Roslyn version updates and hotfixes
  - Improved stability and bug fixes

---

**Stats:** 61 files changed, +10,281 lines added, -493 lines removed  
**Breaking Changes:** None  
**Security:** CodeQL scan clean

**Full Changelog**: https://github.com/JuniorDark/RustyHearts-Toolkit/compare/v1.3.0...v1.4.0
