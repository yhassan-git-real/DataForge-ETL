# DataForge ETL - Self-Contained Deployment

## Overview
This DataForge ETL has been configured for **self-contained deployment** on Windows x64 platforms. This means the applications can run on any Windows machine without requiring .NET runtime installation.

## What's Changed

### 1. Project Configuration
All project files (`*.csproj`) now include self-contained deployment settings:
- `SelfContained=true` - Includes .NET runtime with the application
- `RuntimeIdentifier=win-x64` - Targets Windows x64 platforms
- `PublishSingleFile=true` - Creates single executable files
- `IncludeNativeLibrariesForSelfExtract=true` - Includes all dependencies

### 2. Updated Paths in appsettings.json
The executable paths now point to the self-contained publish directories:
```
ETL_DynamicTableManager\bin\Release\net8.0\win-x64\ETL_DynamicTableManager.exe
ETL_Excel\bin\Release\net8.0\win-x64\ETL_Excel.exe
ETL_ExcelToDatabase\bin\Release\net8.0\win-x64\ETL_ExcelToDatabase.exe
```

### 3. Enhanced Batch Files
All batch files now prioritize self-contained executables:
- First checks for self-contained version in `\win-x64\publish\` folder
- Falls back to debug version if self-contained not available
- Provides clear build instructions for self-contained deployment

## Building Self-Contained Applications

### Method 1: Use the Build Script (Recommended)
```batch
build_self_contained.bat
```
This script builds all applications as self-contained executables.

### Method 2: Manual Build Commands
For individual applications:

```batch
# Core Orchestrator
cd Core
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Excel Processor
cd ETL_Excel
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Database Loader
cd ETL_ExcelToDatabase
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Dynamic Table Manager
cd ETL_DynamicTableManager
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Deployment Benefits

### ✅ Advantages
- **No Runtime Dependencies**: Works on any Windows x64 machine
- **Simplified Deployment**: Just copy files and run
- **Version Consistency**: Bundled runtime ensures compatibility
- **Offline Installation**: No internet required for .NET download

### ⚠️ Considerations
- **Larger File Sizes**: Each executable is ~70-100MB (includes runtime)
- **Longer Build Times**: Publishing takes more time than regular builds
- **Platform Specific**: Only works on Windows x64

## File Structure After Build

```
DataForge_ETL/
├── Core/bin/Release/net8.0/win-x64/publish/
│   └── DataForgeETL.exe                    (~85MB)
├── ETL_Excel/bin/Release/net8.0/win-x64/publish/
│   └── ETL_Excel.exe                             (~95MB)
├── ETL_ExcelToDatabase/bin/Release/net8.0/win-x64/publish/
│   └── ETL_ExcelToDatabase.exe                   (~90MB)
├── ETL_DynamicTableManager/bin/Release/net8.0/win-x64/publish/
│   └── ETL_DynamicTableManager.exe               (~80MB)
└── appsettings.json                              (shared config)
```

## Deployment Instructions

### For End Users
1. Copy the entire `DataForge_ETL` folder to target machine
2. Ensure `appsettings.json` is configured with correct paths
3. Run `start_unified_etl.bat` or individual application batch files
4. **No .NET installation required!**

### For Developers
1. Use `build_self_contained.bat` for development builds
2. Test on machines without .NET to verify self-contained deployment
3. Update `appsettings.json` paths as needed for different environments

## Configuration Requirements

Only `appsettings.json` needs to be updated for different environments:
- **Database**: Update connection strings
- **Paths**: Adjust file paths for local environment
- **Tables**: Configure table names as needed

All other dependencies are bundled with the applications.

## Troubleshooting

### Build Issues
- Ensure .NET 8.0 SDK is installed on development machine
- Check for sufficient disk space (builds require ~2GB)
- Close running applications before building

### Runtime Issues
- Verify Windows x64 compatibility
- Check file permissions on target machine
- Ensure `appsettings.json` is in the same folder as executables

## Performance Notes
- **Startup Time**: Slightly slower due to self-extraction
- **Memory Usage**: Similar to framework-dependent deployments
- **File I/O**: No performance impact after startup