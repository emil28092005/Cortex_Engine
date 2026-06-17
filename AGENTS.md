# AGENTS.md — Cortex Engine

## Project Overview

Cortex Engine is a C# (.NET 9) AI-Native 3D game engine. The primary render backend is Raylib-cs (OpenGL). A Vulkan backend exists but is deferred.

## Build Commands

```bash
# Build (Debug)
dotnet build CORTEX_ENGINE.sln -c Debug

# Build (Release)
dotnet build CORTEX_ENGINE.sln -c Release

# Run the engine
./scripts/run.sh

# Run with test scene + camera tour (headless screenshot capture)
dotnet run --project src/CortexEngine.App/CortexEngine.App.csproj -c Release -- --test-scene --camera-tour --mcp-port 0

# Run with MCP server
./scripts/run.sh --mcp-port 5000
```

## Lint / Typecheck

No separate lint command. `dotnet build` with 0 warnings is the standard. Run `dotnet build CORTEX_ENGINE.sln -c Release` to verify.

## Architecture

- **Engine.Core** — `IWindow`, `IInputState`, `Key` enum, `Sdl3Window`, camera controllers, ECS components (`Transform`, `Mesh`, `Material`, `Light`, `Camera`), `Timing`
- **Engine.Graphics** — HAL interfaces (`IRenderContext`, `IRenderer`), `RenderBackendFactory`, mesh loaders (`ObjLoader`, `GltfLoader`)
- **Engine.Graphics.Raylib** — Primary backend. `RaylibWindow` (GLFW), `RaylibInputState`, `RaylibRenderer` with custom GLSL 330 shader (Fresnel, ACES, gamma)
- **Engine.Graphics.Vulkan** — Deferred backend. Compiles but untested. Uses `Sdl3Window` for Vulkan surface.
- **Engine.AI** — `AiCommandProcessor` (7 commands), MCP HTTP + stdio servers
- **CortexEngine.App** — Entry point, main loop, scene setup

## Key Conventions

- Each render backend owns its window (`IWindow`). The app gets the window from `IRenderContext.Window`.
- Input is backend-agnostic via `IInputState` + `Key` enum. No SDL3 types in app code.
- Camera controllers use `IInputState`, not `InputMapping` directly.
- `RenderBackendFactory.Create(name, width, height, validation)` — backends register by name.
- Custom mesh CPU data uses `NativeMemory.Alloc` (not `Marshal.AllocHGlobal`) to match Raylib's `RL_FREE`.
- `SetShaderValue` uses `float[]` for vectors, not `Vector3`/`Vector4` (marshaling reliability).
- Backface culling disabled (`Rlgl.DisableBackfaceCulling`) for mixed-winding meshes.

## Current Roadmap

See `CORTEX_ENGINE_ARCHITECTURE.md` §11 for the full roadmap. Short-term priorities:
- Unit tests
- Texture loading verification
- ImGui integration (medium-term)

## Files Not to Edit

- `CORTEX_ENGINE_ARCHITECTURE.md` — canonical architecture reference, update only when architecture changes
- `src/Engine.Graphics.Vulkan/Shaders/*.spv` — compiled SPIR-V, regenerate from `.vert`/`.frag` with glslangValidator

## Environment

- .NET 9 SDK at `$HOME/.dotnet`
- `DOTNET_ROOT` and `PATH` must include `$HOME/.dotnet`
- Raylib-cs 8.0.0 (Raylib 6.0 native library bundled in NuGet)
- Display required (X11/Wayland) for Raylib window
