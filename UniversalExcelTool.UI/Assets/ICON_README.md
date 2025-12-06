# Application Icon Setup

## Icon Files

The application uses `app-icon.svg` as the source icon design.

### Converting SVG to ICO

To create a Windows ICO file from the SVG, you can use one of these methods:

#### Option 1: Online Converter (Easiest)
1. Go to https://convertio.co/svg-ico/ or https://cloudconvert.com/svg-to-ico
2. Upload `Assets/app-icon.svg`
3. Convert to ICO format (256x256, 128x128, 64x64, 32x32, 16x16)
4. Download as `app-icon.ico`
5. Place in `Assets/` folder

#### Option 2: Using ImageMagick (Command Line)
```bash
magick convert -density 256x256 -background transparent app-icon.svg -define icon:auto-resize=256,128,64,48,32,16 app-icon.ico
```

#### Option 3: Using Inkscape (GUI)
1. Open `app-icon.svg` in Inkscape
2. Export as PNG at 256x256
3. Use an online ICO converter or tool like GIMP to create ICO

## Icon Design

The icon features:
- **Gradient Background**: Purple (#654ea3) to Pink (#eaafc8)
- **Excel Grid**: Pink-tinted grid representing spreadsheet cells
- **Database Symbol**: Overlapping cylinder representing database integration
- **Modern Style**: Rounded corners, clean lines, professional appearance

## For Build Process

Once you have the ICO file:
1. Place `app-icon.ico` in the `Assets/` folder
2. The project is configured to use it automatically
3. Build with `build_self_contained.bat` or `build_self_contained.ps1`

## Temporary Workaround

If you don't have an ICO file yet, the application will build without an icon. You can:
1. Comment out the `<ApplicationIcon>` line in `DataForgeETL.UI.csproj`
2. Add the icon later and rebuild

The SVG file is included for future reference and can be used for:
- Creating desktop shortcuts
- Documentation
- Web versions
- Marketing materials
