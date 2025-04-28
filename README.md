# 🚀 PowerToys Run: SpeedTest Plugin

<div align="center">
  <img src="SpeedTest/data/demo-speedtest.gif" alt="SpeedTest Plugin Demo" width="800">
  <p align="center">
    <img src="SpeedTest/data/logo.png" alt="SpeedTest Icon" width="128" height="128">
  </p>
  <h1>⚡ SpeedTest for PowerToys Run ⚡</h1>
  <h3>Run internet speed tests directly from PowerToys Run</h3>

  <!-- Badges -->
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest">
    <img src="https://img.shields.io/github/v/release/ruslanlap/PowerToysRun-SpeedTest?style=for-the-badge" alt="Latest Release">
  </a>
  <img src="https://img.shields.io/badge/PowerToys-Compatible-blue" alt="PowerToys Compatible">
  <img src="https://img.shields.io/badge/platform-Windows-lightgrey" alt="Platform">
  <a href="https://opensource.org/licenses/MIT">
    <img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License">
  </a>
  <img src="https://img.shields.io/maintenance/yes/2025" alt="Maintenance">
  <img src="https://img.shields.io/badge/C%23-.NET-512BD4" alt="C# .NET">
  <img src="https://img.shields.io/badge/version-v0.90.1-brightgreen" alt="Version">
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
</div>

<div align="center">
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest/download/SpeedTest-x64.zip">
    <img src="https://img.shields.io/badge/⬇️_DOWNLOAD-x64-blue?style=for-the-badge&logo=github" alt="Download x64">
  </a>
  <a href="https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest/download/SpeedTest-ARM64.zip">
    <img src="https://img.shields.io/badge/⬇️_DOWNLOAD-ARM64-blue?style=for-the-badge&logo=github" alt="Download ARM64">
  </a>
</div>

---

## 📋 Table of Contents
- [📝 Overview](#-overview)
- [✨ Features](#-features)
- [🎬 Demo](#-demo)
- [⚡ Easy Install](#-easy-install)
- [🚀 Usage](#-usage)
- [📁 Data Folder](#-data-folder)
- [🛠️ Building from Source](#️-building-from-source)
- [📊 Project Structure](#-project-structure)
- [🤝 Contributing](#-contributing)
- [❓ FAQ](#-faq)
- [☕ Support](#-support)
- [📄 License](#-license)
- [🙏 Acknowledgements](#-acknowledgements)

---

## 📝 Overview

**SpeedTest** is a PowerToys Run plugin that lets you check your internet speed instantly from your keyboard. Just type `speedtest` in PowerToys Run and launch a test—no browser required!

- **Plugin ID:** `5A0F7ED1D3F24B0A900732839D0E43DB`
- **Action Keyword:** `speedtest`
- **Platform:** Windows 10/11 (x64, ARM64)
- **Tech:** C#/.NET, WPF, PowerToys Run API

## ✨ Features
- ⚡ One-command internet speed test from PowerToys Run
- 📊 Shows download, upload, ping, server info, and shareable result URL
- 🖼️ Modern WPF UI with real-time progress and results
- 🎨 Theme-aware (dark/light icons, adapts to system theme)
- 📝 Copy/share results instantly
- 🛠️ Robust error handling and informative messages
- 🧪 Automated tests and CI/CD (GitHub Actions)

## 🎬 Demo
<div align="center">
  <img src="SpeedTest/data/demo1.png" width="350" alt="Demo 1">
  <img src="SpeedTest/data/demo2.png" width="350" alt="Demo 2">
  <img src="SpeedTest/data/demo3.png" width="350" alt="Demo 3">
</div>

## ⚡ Easy Install
1. [Download the latest release (x64)](https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest/download/SpeedTest-x64.zip)
2. [Download the latest release (ARM64)](https://github.com/ruslanlap/PowerToysRun-SpeedTest/releases/latest/download/SpeedTest-ARM64.zip)
3. Extract to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\`
4. Restart PowerToys
5. Press `Alt+Space`, type `speedtest`, and hit Enter!

## 🚀 Usage
- Open PowerToys Run (`Alt+Space`)
- Type `speedtest` and select `Run Speed Test`
- View real-time progress and detailed results
- Click the result URL to view/share your result online

## 📁 Data Folder
The [`SpeedTest/data`](SpeedTest/data/) folder contains:
- Demo GIFs and screenshots for documentation
- Plugin and theme icons (`logo.png`, `speedtest.dark.png`, `speedtest.light.png`)

Feel free to use these assets in your own documentation or to customize the plugin's appearance.

## 🛠️ Building from Source
- Requires .NET 6+ SDK and Windows 10/11
- Clone the repo and open `Templates.sln` in Visual Studio
- Build the `SpeedTest` project (x64 or ARM64)
- Output: `SpeedTest-x64.zip` or `SpeedTest-ARM64.zip` in the root directory

## 📊 Project Structure
```
SpeedTest/
├── Community.PowerToys.Run.Plugin.SpeedTest/    # Plugin source code
├── data/                                       # Demo assets and icons
├── tests/                                      # Unit & integration tests
├── Publish/                                    # Build output
├── ...
```

## 🤝 Contributing
Contributions are welcome! Here's how you can help:
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

Please make sure to update tests as appropriate.

### Contributors
- [ruslanlap](https://github.com/ruslanlap) - Project creator and maintainer

## ❓ FAQ
<details>
<summary><b>How do I change the plugin's theme?</b></summary>
<p>Theme adapts automatically to your system. Dark and light icons are included.</p>
</details>
<details>
<summary><b>Where are my results stored?</b></summary>
<p>Results are not stored persistently; you can copy or share them after each test.</p>
</details>
<details>
<summary><b>Does it work offline?</b></summary>
<p>No, an internet connection is required to run speed tests.</p>
</details>
<details>
<summary><b>Can I choose a specific server?</b></summary>
<p>Not yet, but server selection support is planned for future releases.</p>
</details>

## ☕ Support
Enjoying SpeedTest? ☕ Buy me a coffee to support development:

[![Buy me a coffee](https://img.shields.io/badge/Buy%20me%20a%20coffee-☕️-FFDD00?style=for-the-badge&logo=buy-me-a-coffee)](https://ruslanlap.github.io/ruslanlap_buymeacoffe/)

## 📄 License
MIT License. See [LICENSE](LICENSE).

## 🙏 Acknowledgements
- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) team
- [Ookla Speedtest CLI](https://www.speedtest.net/apps/cli)
- All contributors and users!

---

<div align="center">
  <sub>Made with ❤️ by <a href="https://github.com/ruslanlap">ruslanlap</a></sub>
</div>
