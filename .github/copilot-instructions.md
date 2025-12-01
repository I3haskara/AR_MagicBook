# Copilot Instructions for AR_MagicBook

## Project Overview
- This is a Unity-based AR project structured for extensibility and modularity.
- Major components are organized under `Assets/`, with subfolders for features (e.g., `CustomizableGrid`, `EpicVictoryEffects`, `Joystick Pack`, `VisionPilot`, `XR`).
- The solution uses multiple `.csproj` files for editor and runtime code separation.
- The `Packages/com.bezi.sidekick` directory contains a custom Unity plugin for integration with the Bezi App (see [Bezi docs](https://docs.bezi.com)).

## Key Workflows
- **Builds:** Use Unity Editor to build the project. No custom build scripts detected; standard Unity build pipeline applies.
- **Testing:** No explicit test folders or scripts found. If tests exist, they may be in custom locations or handled via Unity Test Runner.
- **Debugging:** Debug using Unity Editor's Play mode and standard debugging tools. No custom debug scripts detected.

## Conventions & Patterns
- Editor scripts are separated into `Editor/` subfolders (e.g., `Assets/CustomizableGrid/Editor`).
- Asset types (Materials, Meshes, Scenes, Shaders, Textures) are organized in dedicated subfolders for each feature.
- Project settings and configuration files are in `ProjectSettings/` and `Packages/manifest.json`.
- No custom code generation, build, or test automation scripts found.

## Integration Points
- **Bezi Plugin:** Located in `Packages/com.bezi.sidekick`. Enables asset search, project sync, and code modification via the Bezi App.
- **External Packages:** Managed via Unity's Package Manager (`Packages/manifest.json`).

## Examples of Patterns
- To add a new feature, create a new folder under `Assets/` with subfolders for `Materials`, `Meshes`, `Scenes`, etc.
- Editor extensions should be placed in an `Editor/` subfolder within the relevant feature directory.

## References
- [Bezi Plugin README](Packages/com.bezi.sidekick/README.md)
- Unity documentation for project structure and workflows

---

**If any section is unclear or missing important details, please provide feedback or point to relevant files to improve these instructions.**
