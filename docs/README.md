# PowerToys Run: SpeedTest Plugin

<div align="center">
  <img src="../SpeedTest/data/logo.png" alt="SpeedTest Icon" width="128" height="128">
  <h3>⚡ Run internet speed tests directly from PowerToys Run ⚡</h3>
  
  <!-- Badges -->
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/actions/workflows/build-and-release.yml">
    <img src="https://github.com/ruslanlap/PowerToysRun-SpeedTest/actions/workflows/build-and-release.yml/badge.svg" alt="Build Status">
  </a>
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest">
    <img src="https://img.shields.io/github/v/release/ruslanlap/PowerToysRun-SpeedTest?label=latest" alt="Latest Release">
  </a>
  <img src="https://img.shields.io/maintenance/yes/2025" alt="Maintenance">
  <img src="https://img.shields.io/badge/C%23-.NET-512BD4" alt="C# .NET">
  <img src="https://img.shields.io/badge/version-v1.0.5-brightgreen" alt="Version">
  <img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg" alt="PRs Welcome">
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/stargazers">
    <img src="https://img.shields.io/github/stars/ruslanlap/PowerToysRun-SpeedTest" alt="GitHub stars">
  </a>
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/issues">
    <img src="https://img.shields.io/github/issues/ruslanlap/PowerToysRun-SpeedTest" alt="GitHub issues">
  </a>
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest">
    <img src="https://img.shields.io/github/downloads/ruslanlap/PowerToysRun-SpeedTest/total" alt="GitHub all releases">
  </a>
  <img src="https://img.shields.io/badge/Made%20with-❤️-red" alt="Made with Love">
  <img src="https://img.shields.io/badge/Awesome-Yes-orange" alt="Awesome">
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest">
    <img src="https://img.shields.io/github/v/release/ruslanlap/PowerToysRun-SpeedTest?style=for-the-badge" alt="Latest Release">
  </a>
  <img src="https://img.shields.io/badge/PowerToys-Compatible-blue" alt="PowerToys Compatible">
  <img src="https://img.shields.io/badge/platform-Windows-lightgrey" alt="Platform">
  <a href="https://opensource.org/licenses/MIT">
    <img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License">
  </a>
  <a href="https://github.com/hlaueriksson/awesome-powertoys-run-plugins">
    <img src="https://awesome.re/mentioned-badge.svg" alt="Mentioned in Awesome PowerToys Run Plugins">
  </a>
</div>

<div align="center">
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/download/v1.0.5/SpeedTest-1.0.5-x64.zip">
    <img src="https://img.shields.io/badge/⬇️_DOWNLOAD-x64-blue?style=for-the-badge&logo=github" alt="Download x64">
  </a>
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/download/v1.0.5/SpeedTest-1.0.5-ARM64.zip">
    <img src="https://img.shields.io/badge/⬇️_DOWNLOAD-ARM64-blue?style=for-the-badge&logo=github" alt="Download ARM64">
  </a>
</div>

## Overview

Check your internet speed instantly from PowerToys Run. Just type `spt` and hit Enter—no browser required!

**Features:**
- ⚡ One-command speed test from PowerToys Run
- 📊 Shows download, upload, ping, server info, and shareable URL
- 🎨 Beautiful WPF UI with real-time progress and loading animation
- 📋 Optional clipboard integration (configurable)
- 🌙 Theme-aware (adapts to dark/light mode)

## Demo

<div align="center">
  <img src="../SpeedTest/data/demo-speedtest.gif" alt="SpeedTest Plugin Demo" width="600">
</div>

## Installation

**Requirements:** Windows 10/11 + PowerToys

1. Download the ZIP for your platform ([x64](https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/download/v1.0.5/SpeedTest-1.0.5-x64.zip) | [ARM64](https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/download/v1.0.5/SpeedTest-1.0.5-ARM64.zip))
2. Extract to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\`
3. Restart PowerToys
4. Press `Alt+Space`, type `spt`, and hit Enter!

## Usage

- Open PowerToys Run (`Alt+Space`)
- Type `spt` and select "Run Speed Test"
- View real-time progress with animated loading
- Results window flashes when complete
- Click the URL to share your results online
- Configure clipboard settings in PowerToys settings

## What's New in v1.0.5

- 🎨 Beautiful new loading animation (classic "running dots" like Speedtest.net)
- 📱 Enhanced UI with improved window positioning
- ⚡ Window flash notification when test completes
- 🔧 Better error handling and stability

## Building from Source

Requirements: .NET 6+ SDK, Windows 10/11

```bash
git clone https://github.com/ruslanlap/PowerToysRun-SpeedTest.git
cd PowerToysRun-SpeedTest
# Open Templates.sln in Visual Studio and build
```

## FAQ

**Q: How do I enable/disable clipboard copying?**  
A: Go to PowerToys settings → Plugins → SpeedTest → Toggle clipboard option

**Q: Can I choose a specific server?**  
A: Not yet, but server selection is planned for future releases

**Q: Does it work offline?**  
A: No, an internet connection is required

## Contributing

Contributions welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting PRs.

## Support

Like this plugin? ☕ [Buy me a coffee](https://ruslanlap.github.io/ruslanlap_buymeacoffe/)

## License

MIT License - see [LICENSE](LICENSE) for details.

---

<div align="center">
  <sub>Made with ❤️ by <a href="https://github.com/ruslanlap">ruslanlap</a></sub>
</div>