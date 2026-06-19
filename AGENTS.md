# AGENTS.md — Cortex Engine

## Project Overview

Cortex Engine is a C# (.NET 9) AI-Native 3D game engine with a pure P/Invoke Vulkan 1.3 render backend. No wrapper libraries (Silk.NET, Vortice, OpenTK) — direct Vulkan API calls via `vkGetInstanceProcAddr`/`vkGetDeviceProcAddr`.

## Build Commands

```bash
# Build (Debug)
dotnet build CORTEX_ENGINE.sln -c Debug

# Run the engine
./scripts/run.sh

# Run with MCP server (AI control via HTTP/SSE)
./scripts/run.sh -- --mcp-port 5000

# Run tests
dotnet test tests/Engine.Tests/Engine.Tests.csproj -c Debug
```

## Lint / Typecheck

No separate lint command. `dotnet build` with 0 errors is the standard. Run `dotnet build CORTEX_ENGINE.sln -c Debug` to verify. 227 xUnit tests cover Vulkan struct sizes, enum values, OBJ loading, vertex layout, shadow mapping, camera controllers, AI commands.

## Architecture

- **Engine.Core** — `IWindow`, `IInputState`, `Key` enum, `Sdl3Window` (SDL3 + Vulkan surface), camera controllers (`FreeFly`, `Orbit`), ECS components (`Transform`, `Mesh`, `Material`, `Light`, `Camera`, `RigidBody`), `Vertex` struct, `Timing`
- **Engine.Graphics** — HAL interfaces (`IRenderContext`, `IRenderer`, `IScreenshotProvider`), `RenderBackendFactory`, `ObjLoader`, `MeshMath`, `ProceduralMesh`, `SceneSerializer`
- **Engine.Graphics.Vulkan** — Pure P/Invoke Vulkan 1.3 backend. Vulkan 1.3 features: dynamic rendering, synchronization2, imageCubeArray. Multi-light PBR with cubemap array shadows. ImGui integration. Video recording via FFmpeg pipe.
- **Engine.Physics** — JoltPhysicsSharp wrapper, `PhysicsWorld`, `RigidBody` component (box/sphere colliders)
- **Engine.AI** — `AiCommandProcessor` (7 commands), `AiCommandQueue` (thread-safe), MCP HTTP server (Kestrel + SSE), stdio MCP server
- **CortexEngine.App** — Entry point, main loop, scene setup, ImGui debug panels

## Key Conventions

- Each render backend owns its window (`IWindow`). The app gets the window from `IRenderContext.Window`.
- Input is backend-agnostic via `IInputState` + `Key` enum. No SDL3 types in app code.
- Camera controllers use `IInputState`, not `InputMapping` directly.
- `RenderBackendFactory.Create(name, width, height, validation)` — backends register by name.
- Vulkan types split into `VulkanHandles.cs`, `VulkanEnums.cs`, `VulkanStructs.cs` — struct sizes verified by tests against C headers.
- Push constants: single range, `Vertex|Fragment`, 64B (main pipeline) or 160B (shadow pipeline).
- Light data in SceneUBO (448B): `mat4 vp` + `int numLights` + `int numShadowLights` + `LightData[8]` + `shadowParams[4]` + `ambientColor`.
- Shadow cubemap array: 24 layers (4 lights × 6 faces), `samplerCubeArray` in shader, 16-tap Poisson disk PCF.
- `vkCmdCopyImageToBuffer` for video recording, BGRA format, FFmpeg pipe.
- Matrix convention: `view * proj` (row-major, no `row_major` in GLSL). `proj.M22 *= -1` for Vulkan Y-down.

## Files Not to Edit

- `src/Engine.Graphics.Vulkan/Shaders/*.spv` — compiled SPIR-V, regenerate from `.vert`/`.frag` with `glslangValidator -V`
- `CORTEX_ENGINE_ARCHITECTURE.md` — canonical architecture reference, update only when architecture changes

## Environment

- .NET 9 SDK at `$HOME/.dotnet`
- `DOTNET_ROOT` and `PATH` must include `$HOME/.dotnet`
- SDL3 (ppy.SDL3-CS 2026.520.0, bundled native libSDL3.so)
- ImGui.NET 1.91.6.1
- JoltPhysicsSharp 2.21.0
- Vulkan 1.3+ (validation layers recommended for development)
- FFmpeg (for video recording feature)
- glslangValidator (for shader compilation: `sudo apt install glslang-tools`)
- Display required (X11/Wayland) for Vulkan window
