# Changelog

All notable changes to the PowerToys Run SpeedTest Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Pressing `Esc` now cancels a running speed test or closes the results window

## [1.0.5] - 2025-01-14

### Added
- Beautiful new loading animation with classic "running dots" spinner (like Speedtest.net)
- Window flash notification when speed test results are ready
- Dynamic center text that updates based on test stage (Connecting → Testing latency → Testing download → Testing upload → Complete)
- Enhanced window positioning - results window now centers on screen
- Optimized animation timing for smoother visual experience

### Changed
- Improved loading window UI with better text layout and positioning
- Enhanced animation performance with faster, more responsive dot animations
- Better visual feedback during different test phases
- Refined window management and positioning logic

### Fixed
- Fixed animation resource conflicts that caused application crashes
- Resolved text overlap issues in loading window
- Improved window positioning consistency
- Fixed animation cleanup on window close

## [1.0.4] - 2024-12-XX

### Added
- Real-time CLI output in UI
- Live progress tracking and updates
- Proper speedtest CLI integration

### Changed
- Removed hardcoded demo data
- Improved error handling and user feedback
- Better CLI argument handling

### Fixed
- Fixed issues with fake/demo data being displayed
- Resolved CLI execution problems
- Improved reliability of speed test results

## [1.0.3] - 2024-03-XX

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

## [1.0.2] - Previous Version

### Initial Features
- Internet speed testing using Ookla's Speedtest CLI
- Display of download, upload, and ping metrics
- Results window with detailed information
- Ability to copy results as text or image URL
- Dark/Light theme support
- Basic error handling and user feedback 