# QuickPrice

**Real-time item value display for SPT (Single Player Tarkov) 4.0.0**

![Version](https://img.shields.io/badge/version-1.3.1-blue)
![SPT](https://img.shields.io/badge/SPT-4.0.0-green)
![License](https://img.shields.io/badge/license-MIT-green)

[English](README.md) | [中文文档](README_CN.md)

---

## 📖 Overview

**QuickPrice** is a comprehensive SPT mod that displays real-time item prices directly in your inventory tooltips. Make informed decisions about what to loot, sell, or keep with color-coded price indicators and detailed market information.

### ✨ Key Features

- 💰 **Real-time Price Display** - Shows flea market and trader prices on item hover
- 🎨 **Color-Coded Items** - Six-tier color system based on item value
- 🔫 **Weapon Mod Pricing** - Calculates total value including all attachments
- 🛡️ **Armor Class Coloring** - Visual armor tier indicators (1-6)
- 🎯 **Ammo Penetration Display** - Color-coded by penetration power
- 📊 **Price-per-Slot** - See value density for optimal looting
- 🏪 **Trader Comparison** - Compare flea vs. best trader buyback prices
- ⚡ **Performance Optimized** - Smart container calculation with configurable limits
- 🌐 **Fully Chinese Localized** - Complete Chinese UI and configuration

---

## 📸 Screenshots

### Price Display & Tooltip
![Price Display](Docs/Screenshots/price-display-tooltip.png)
*Real-time price information displayed in item tooltips with color-coded value indicators*

### Color-Coded Inventory
![Color Coded Inventory](Docs/Screenshots/color-coded-inventory.png)
*Large inventory view showing the six-tier color coding system across multiple items*

### Inventory Overview
![Inventory Overview](Docs/Screenshots/inventory-overview.png)
*Mixed item types including weapons, ammo, and containers with price information*

---

## 🚀 Installation

### Prerequisites

- SPT (Single Player Tarkov) **4.0.0**
- BepInEx **5.4.22** or higher (included with SPT)

### Client Installation (BepInEx Plugin)

1. Download the latest release from [Releases](https://github.com/2324834989/quickprice-spt/releases)
2. Extract `QuickPrice.dll` to:
   ```
   SPT_Install_Directory/BepInEx/plugins/QuickPrice/
   ```
3. Launch SPT and enjoy!

### Server Installation (Optional - For Dynamic Prices)

1. Extract the server module to:
   ```
   SPT_Install_Directory/user/mods/QuickPrice/
   ```
2. Restart the SPT server
3. Enable "Use Dynamic Prices" in the client configuration (F12)

---

## 🎮 Usage

### Basic Controls

- **Hold Ctrl** - Press and hold `Left Ctrl` or `Right Ctrl` while hovering over items to see prices
  - Can be disabled in config to show prices on hover without Ctrl

### Color Coding System

#### Price-Based Colors (Default Items)
| Color | Price Range | Indicator |
|-------|-------------|-----------|
| ⚪ White | ≤ 5,000₽ | Common items |
| 🟢 Green | 5,001 - 18,000₽ | Low value |
| 🔵 Blue | 18,001 - 35,000₽ | Medium value |
| 🟣 Purple | 35,001 - 70,000₽ | High value |
| 🟠 Orange | 70,001 - 180,000₽ | Very high value |
| 🔴 Red | > 180,000₽ | Extremely valuable |

#### Penetration-Based Colors (Ammo & Magazines)
| Color | Penetration | Effectiveness |
|-------|-------------|---------------|
| ⚪ White | < 15 | Low penetration |
| 🟢 Green | 15 - 24 | Light armor |
| 🔵 Blue | 25 - 34 | Medium armor |
| 🟣 Purple | 35 - 44 | Heavy armor |
| 🟠 Orange | 45 - 54 | Class 5 armor |
| 🔴 Red | ≥ 55 | Class 6 armor |

#### Armor Class Colors
| Color | Armor Class | Protection Level |
|-------|-------------|------------------|
| ⚫ Gray | Class 1 | Minimal |
| 🟢 Green | Class 2 | Light |
| 🔵 Blue | Class 3 | Medium |
| 🟣 Purple | Class 4 | Heavy |
| 🟠 Orange | Class 5 | Very Heavy |
| 🔴 Red | Class 6 | Maximum |

### Configuration

Press **F12** in-game to open BepInEx Configuration Manager and customize:

- Toggle price display (Ctrl required or always-on)
- Adjust color thresholds for each tier
- Enable/disable specific features
- Performance tuning for large containers
- Cache mode selection

---

## ⚙️ Configuration Options

### Main Settings
- **Enable Plugin** - Master toggle
- **Show Flea Prices** - Display flea market values
- **Show Price Per Slot** - Calculate value density
- **Show Weapon Mods Price** - Include attachment values
- **Require Ctrl Key** - Hold Ctrl to show prices (default: enabled)
- **Tooltip Delay** - Hover delay before showing prices (0-2 seconds)
- **Show Tooltip Underline** - Draw an underline separator beneath the item name

### Display Settings
- **Enable Color Coding** - Color items by value/penetration
- **Show Best Price in Bold** - Highlight highest price
- **Use Caliber Penetration Power** - Color ammo by penetration instead of price
- **Color Item Name** - Apply colors to item names in tooltips
- **Enable Armor Class Coloring** - Color armor by protection level
- **Auto-color Item Backgrounds** - Apply colors to inventory grid (experimental)

### Performance Settings
- **Use Dynamic Prices** - Fetch real-time flea market prices (slower)
- **Price Cache Mode** - Permanent / 5min / 10min / Manual refresh
- **Max Container Depth** - Recursion limit for nested containers (default: 10)
- **Max Container Items** - Item limit per container (default: 100)
- **Skip Large Containers** - Skip calculation for containers with 150+ items

### Advanced Features
- **Show Trader Prices** - Display best trader buyback offers
- **Show Flea Tax** - Calculate listing fees and net profit
- **Auto Refresh on Open Inventory** - Refresh prices when opening stash

---

## 🛠️ Building from Source

### Client (BepInEx Plugin)

```bash
cd Sources/Client
dotnet build -c Release
```

Output: `bin/Release/net471/QuickPrice.dll`

### Server (SPT Module)

```bash
cd Sources/Server
dotnet build -c Release
```

Output: `bin/Release/quickprice.dll`

### Requirements
- .NET Framework 4.7.1 SDK (client)
- .NET 9.0 SDK (server)
- SPT 4.0.0 assemblies (see .csproj for paths)

---

## 📊 Performance Optimization

QuickPrice includes intelligent performance optimizations:

- **Smart Container Calculation** - Limits recursion depth and item count
- **Async Price Loading** - Non-blocking background updates
- **Efficient Caching** - Configurable cache expiration
- **Large Container Skipping** - Avoids lag on item boxes with 150+ items

Default limits (recommended):
- Max recursion depth: **10 layers**
- Max items per container: **100 items**
- Large container threshold: **150 items**

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

### Development Guidelines
- Follow existing code style
- Test thoroughly with SPT 4.0.0
- Update documentation for new features
- Maintain Chinese localization

---

## 📝 Changelog

### Version 1.3.1 (2026-01-13)
- Added client log gating for debug/warn output and improved debug toggle behavior
- Added search sound/time settings with six-tier duration presets (SearchPatch merge)
- Added server reloadable config (Enable/CacheTimeout/AutoRefresh) with disable flow
- Aggregated ragfair offers during cache refresh and logged refresh duration
- Loaded ragfair dynamic blacklist after init and added SptDir/XML doc updates

### Version 1.0.0 (2025-10-21)
- Initial release
- Complete rewrite with new project structure
- Full SPT 4.0.0 compatibility
- Performance optimizations for large containers
- Six-tier color-coding system
- Armor class detection and coloring
- Trader price comparison
- Configurable cache modes
- Chinese localized configuration

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- SPT-AKI Development Team for the amazing SPT platform
- BepInEx team for the modding framework
- All contributors and testers

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/2324834989/quickprice-spt/issues)
- **Discussions**: [GitHub Discussions](https://github.com/2324834989/quickprice-spt/discussions)

---

**Made with ❤️ for the SPT community**
