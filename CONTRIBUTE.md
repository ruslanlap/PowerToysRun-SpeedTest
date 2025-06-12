# Contributing to PowerToys Run SpeedTest Plugin

Thank you for your interest in contributing to the PowerToys Run SpeedTest Plugin! This document provides guidelines and instructions for contributing to the project.

## Table of Contents
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)

## Development Setup

### Prerequisites
- Visual Studio 2022 or later
- .NET 6.0 SDK or later
- PowerToys development environment
- Ookla Speedtest CLI (included in the plugin)

### Getting Started
1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/PowerToysRun-SpeedTest.git
   ```
3. Add the upstream repository:
   ```bash
   git remote add upstream https://github.com/ruslanlap/PowerToysRun-SpeedTest.git
   ```
4. Create a new branch for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Project Structure

```
PowerToysRun-SpeedTest/
├── SpeedTest/
│   └── Community.PowerToys.Run.Plugin.SpeedTest/
│       ├── Images/                 # Plugin icons
│       ├── Main.cs                # Plugin main logic
│       ├── SpeedTestResult.cs     # Result data model
│       ├── ResultsWindow.xaml     # Results UI
│       ├── LoadingWindow.xaml     # Loading UI
│       └── plugin.json           # Plugin metadata
├── CHANGELOG.md
├── CONTRIBUTE.md
└── README.md
```

## Coding Standards

### General Guidelines
- Follow C# coding conventions
- Use meaningful variable and method names
- Add comments for complex logic
- Keep methods focused and concise
- Use dependency injection where appropriate

### XAML Guidelines
- Use proper resource management
- Follow MVVM pattern where applicable
- Maintain consistent styling
- Use proper layout containers

### Best Practices
1. Error Handling:
   - Use appropriate exception handling
   - Provide meaningful error messages
   - Log errors appropriately

2. Performance:
   - Avoid blocking UI thread
   - Use async/await for I/O operations
   - Optimize resource usage

3. UI/UX:
   - Follow PowerToys design guidelines
   - Ensure dark/light theme support
   - Maintain consistent spacing and padding

## Making Changes

1. Create a feature branch from `main`
2. Make your changes following the coding standards
3. Update documentation as needed
4. Add or update tests if applicable
5. Update CHANGELOG.md with your changes

### Commit Messages
- Use clear and descriptive commit messages
- Start with a verb in present tense
- Reference issues if applicable
- Example: `Add clipboard settings option (#123)`

## Testing

### Manual Testing
1. Build and run the plugin
2. Test both dark and light themes
3. Verify all features work as expected
4. Check error handling
5. Test with different network conditions

### Automated Testing
- Add unit tests for new features
- Ensure existing tests pass
- Test edge cases and error conditions

## Submitting Changes

1. Push your changes to your fork
2. Create a Pull Request (PR) to the main repository
3. Include in your PR:
   - Description of changes
   - Screenshots if UI changes
   - Reference to related issues
   - Updates to documentation

### PR Review Process
1. Maintainers will review your PR
2. Address any feedback or comments
3. Once approved, your PR will be merged

## Questions or Problems?

If you have questions or run into problems, please:
1. Check existing issues
2. Create a new issue if needed
3. Ask for help in the PR comments

Thank you for contributing to make PowerToys Run SpeedTest Plugin better! 