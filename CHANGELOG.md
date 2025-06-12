# Changelog

All notable changes to the PowerToys Run SpeedTest Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.91.0] - 2024-03-XX

### Added
- New setting option to control clipboard behavior
- Settings persistence through JSON file
- Better error handling in disposal process
- Enhanced UI feedback for speed test results

### Changed
- Made clipboard copying optional (disabled by default)
- Improved button styling in results window:
  - Better padding and margins
  - Consistent button sizes
  - Enhanced visual feedback (hover/pressed states)
  - More modern look with larger corner radius
- Refined disposal process to prevent unnecessary notifications

### Fixed
- Issue with unnecessary notifications when closing PowerToys
- UI inconsistencies in button layouts
- Potential null reference exceptions in cleanup process

## [0.90.1] - Previous Version

### Initial Features
- Internet speed testing using Ookla's Speedtest CLI
- Display of download, upload, and ping metrics
- Results window with detailed information
- Ability to copy results as text or image URL
- Dark/Light theme support
- Basic error handling and user feedback 