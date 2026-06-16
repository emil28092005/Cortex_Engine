# CORTEX ENGINE — Technical Architecture Manifesto

**Project**: AI-Native, Multiplatform 3D Game Engine
**Language**: C# (.NET 9)
**Status**: LOCKED — All dependencies verified as of June 2026
**Created**: 2026-06-16

---

## 1. EXECUTIVE SUMMARY

Cortex Engine is a 3D game engine built from scratch to provide a Unity-like development experience (GameObject/Component paradigm, Inspector, Hierarchy, Scene View) while being deeply integrated with Multimodal Large Language Models (MMLMs). The engine allows an AI to:

- **See** the engine state via rendered-frame screenshots, virtual cameras, semantic segmentation maps, and profiler screenshots.
- **Read** the complete ECS world state through native JSON serialization.
- **Modify** the running engine via declarative JSON commands and, in Development Mode, via hot-reloaded C# scripts.

The architecture prioritizes **production maturity** over experimental technologies: Vulkan (via Silk.NET.Vulkan), Flecs.NET (C# bindings for the C-based Flecs ECS), SDL3-cs (ppy.SDL3-CS), and Hexa.NET.ImGui with a native Vulkan backend.

---

## 2. EVOLUTION OF ARCHITECTURE DECISIONS

### 2.1 Initial Discussion

The project began with an exploration of NVIDIA's open-source physics and graphics ecosystems (PhysX 5, Newton, Warp, Falcor, Flow) and modern middleware for building a custom engine from scratch. Initial candidates included:

- **Graphics**: WebGPU (wgpu), Diligent Engine, BGFX, NVIDIA Falcor
- **Physics**: Jolt Physics, NVIDIA PhysX 5, Box2D v3
- **Windowing**: SDL3, GLFW
- **ECS**: Flecs, EnTT, Arch
- **UI**: Dear ImGui

### 2.2 First Iteration

The first proposed stack was:

- C# (.NET 9) + NativeAOT
- Silk.NET + wgpu-native (WebGPU)
- SDL3 (via Silk.NET)
- Flecs ECS
- Dear ImGui with custom WebGPU backend
- Jolt Physics
- Roslyn Compiler API for AI hot-reload

### 2.3 Identified Risks from Internet Research

Research revealed critical issues with the first iteration:

1. **NativeAOT + Roslyn conflict**: NativeAOT explicitly does not support `Assembly.LoadFile()` or `System.Reflection.Emit`. Dynamic C# compilation cannot run inside a NativeAOT binary. This is confirmed by Microsoft Learn documentation.
2. **Silk.NET WebGPU bindings**: The maintainers stated that the official WebGPU examples in Silk.NET are "very bad" and "smoke tests" — not production-ready.
3. **Custom WebGPU ImGui backend**: Would require writing ~400 lines of custom rendering code.
4. **Flecs vs Friflo tradeoff**: Friflo.Engine.ECS is pure managed and faster, but Flecs has mature native C-reflection and built-in JSON serialization that works identically in NativeAOT.

### 2.4 Final Locked Stack

The final stack was chosen to eliminate experimental dependencies and maximize production maturity:

- **C# (.NET 9) with dual-runtime strategy**: JIT for development (Roslyn hot-reload), NativeAOT for release.
- **SDL3-cs**: `ppy.SDL3-CS` — direct, zero-overhead P/Invoke bindings maintained by the osu! team.
- **Vulkan**: `Vortice.Vulkan` — mature C# Vulkan bindings, .NET 9/10 support.
- **MoltenVK**: For macOS/iOS compatibility.
- **Flecs.NET**: `Flecs.NET.Release` — C# bindings for Flecs with NativeAOT static-link support.
- **ImGui**: `Hexa.NET.ImGui` — ships pre-built SDL3 + Vulkan native backends.
- **Jolt Physics**: `JoltPhysicsSharp` — C# bindings for Jolt Physics, .NET 9/10.
- **Roslyn Compiler API**: For Dev-Mode AI hot-reload.
- **SixLabors.ImageSharp**: For JPEG encoding of diagnostic textures.

---

## 3. TECHNOLOGICAL STACK (Zero-Contradiction Core)

### 3.1 Language & Runtime

**C# (.NET 9)**

The engine uses a **dual-runtime strategy**:

| Mode | Runtime | AI Scripting | Use Case |
|------|---------|--------------|----------|
| **Development** | .NET 9 JIT | ✅ Roslyn Compiler API + `AssemblyLoadContext` hot-reload | Editor, AI co-development, rapid iteration |
| **Release** | .NET 9 NativeAOT | ❌ JSON commands only | Shipped PC/Mobile/Console/WASM builds |

**Why this is not a contradiction:**

NativeAOT cannot JIT new code or load assemblies dynamically. Therefore, the engine builds in two configurations:

```xml
<!-- Development -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>DEV_MODE</DefineConstants>
</PropertyGroup>

<!-- Release -->
<PropertyGroup Condition="'$(Configuration)' == 'ReleaseAOT'">
  <DefineConstants>RELEASE_AOT</DefineConstants>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

All Roslyn and `AssemblyLoadContext` code is wrapped in `#if DEV_MODE`.

### 3.2 System Layer

**SDL3 via `ppy.SDL3-CS`**

- NuGet: `ppy.SDL3-CS` 2026.520.0
- Direct P/Invoke bindings, zero overhead
- Cross-platform: Windows, Linux, macOS, iOS, Android
- Handles: window creation, input (keyboard, mouse, touch, gamepad, accelerometer), audio, events
- Used by the osu! framework (2K+ stars), battle-tested

### 3.3 Graphics HAL

**Vulkan via `Silk.NET.Vulkan`**

- NuGet: `Silk.NET.Vulkan` 2.21.0
- .NET 9/10 low-level bindings
- Mature, used by Silk.NET ecosystem
- MoltenVK provides macOS/iOS support

**Note:** Initial prototype used Vortice.Vulkan, but its loader segfaulted on the Kubuntu development setup. Silk.NET.Vulkan is the verified working binding.

**Why Vulkan over WebGPU:**

- Battle-tested in production engines
- Full compute shader support (mandatory for AI vision pipelines)
- Mature C# tooling and ImGui integration
- MoltenVK provides macOS/iOS support

**macOS/iOS path:**

- MoltenVK 1.4 supports Vulkan 1.4 on macOS, iOS, tvOS, visionOS
- `VK_KHR_portability_subset` and `VK_KHR_portability_enumeration` must be enabled
- Loader and MoltenVK libraries must be bundled with the application
- KosmicKrisp (via Mesa 3D) is an emerging alternative for Apple Silicon desktops

### 3.4 Data Architecture

**Flecs.NET**

- NuGet: `Flecs.NET.Debug` (Debug) / `Flecs.NET.Release` (Release) 4.0.4-build.546
- High-level C# wrapper over the C-based Flecs ECS
- Supports .NET Standard 2.1, .NET 5/6/7/8
- NativeAOT-compatible via static linking: `<FlecsStaticLink>true</FlecsStaticLink>`
- Native C-reflection: `ecs_world_to_json()` serializes the entire ECS world
- Used in AAA engines

**Why Flecs.NET over pure managed ECS (Friflo, Arch):**

- Native C-reflection works without C# `System.Reflection` — essential for NativeAOT
- Built-in JSON serialization of the entire world
- Production maturity and industry usage
- Component registration across shared libraries/DLLs
- Automatic archetype management and lockless scheduler

### 3.5 Editor UI

**Hexa.NET.ImGui**

- NuGet: `Hexa.NET.ImGui` (and related backend packages)
- Alternative to ImGui.NET that ships pre-built native backends
- Includes SDL3 + Vulkan backend combinations
- MIT licensed
- Higher performance, reduced startup time
- Eliminates the need to write a custom Vulkan renderer for ImGui

**Why Hexa.NET.ImGui over ImGui.NET:**

- `ImGui.NET` does not ship a C# Vulkan renderer out of the box
- The official Vulkan backend is `imgui_impl_vulkan.cpp` (C++), which must be manually compiled and P/Invoked
- Hexa.NET.ImGui bundles the C++ backends as native libraries with C# bindings
- This is the fastest path to a production-ready editor UI

### 3.6 Physics

**JoltPhysicsSharp**

- NuGet: `JoltPhysicsSharp` 2.21.0
- .NET 9/10 bindings for Jolt Physics
- Cross-platform via `joltc` C wrapper
- Used in Horizon Forbidden West and Death Stranding 2
- Integrated into Godot 4.4

**Note:** Physics module is not part of the foundational MVP but is included in the final project layout.

### 3.7 Diagnostic Encoding

**SixLabors.ImageSharp**

- Pure managed JPEG/PNG encoder
- NativeAOT-compatible
- No native dependencies
- Used to compress Vulkan-rendered RGBA textures into JPEG for MMLM vision input

---

## 4. DATA STRUCTURE & UNITY-LIKE ABSTRACTION

### 4.1 Core Principle

The engine exposes a Unity-like API surface (`GameObject`, `AddComponent`, `GetComponent`) while internally storing all data in Flecs components. The `GameObject` wrapper is **never** a place for state.

### 4.2 GameObject Facade

```csharp
public readonly struct GameObject
{
    public readonly Entity Entity;
    public readonly World World;

    public GameObject(World world, Entity entity)
    {
        World = world;
        Entity = entity;
    }

    public void AddComponent<T>(T component) where T : unmanaged
    {
        Entity.Set(component);
    }

    public ref T GetComponent<T>() where T : unmanaged
    {
        return ref Entity.GetMut<T>();
    }

    public bool HasComponent<T>() where T : unmanaged
    {
        return Entity.Has<T>();
    }

    public void RemoveComponent<T>() where T : unmanaged
    {
        Entity.Remove<T>();
    }
}
```

### 4.3 Component Definitions

Components are plain C# structs registered with the Flecs type system:

```csharp
public struct Transform : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}

public struct MeshRef : IComponent
{
    public ulong MeshId;
}

public struct Camera : IComponent
{
    public Vector3 Position;
    public Vector3 Target;
    public Vector3 Up;
    public float FieldOfView;
    public float AspectRatio;
    public float NearPlane;
    public float FarPlane;
}

public struct Material : IComponent
{
    public Vector3 Albedo;
    public float Roughness;
    public float Metallic;
    public string? TexturePath;
}

public struct Light : IComponent
{
    public Vector3 Direction;
    public Vector3 Color;
    public float Intensity;
}

public struct SemanticClass : IComponent
{
    public byte ClassId; // 0=environment, 1=enemy, 2=player, 3=interactive, 4=trigger
}
```

### 4.4 AI Hot-Reloading (Dev Mode)

When the AI generates a C# script, the engine:

1. Receives the script string via the `AiGateway`.
2. Runs a pre-validation pass (syntax check, banned namespace check, unsafe code check).
3. Feeds the script to `Microsoft.CodeAnalysis.CSharp` (Roslyn).
4. Emits the compiled assembly into a `MemoryStream`.
5. Loads the assembly into a **dedicated** `AssemblyLoadContext`.
6. Extracts systems marked with `[Slot("name")]` attribute.
7. Calls `SystemSlotRegistry.HotSwap()` to replace the old system with the new one.
8. Migrates entities using the old component types to the new types.
9. Unloads the old `AssemblyLoadContext`.

**Important caveat:** Flecs stores component type metadata in its native C memory. If old C# types are still referenced by Flecs, the old `AssemblyLoadContext` cannot be fully unloaded. The migration step must remove old components and re-add them as the new types.

### 4.5 Rendering & Shading

The renderer uses a simple forward-lit pipeline:

- **Vertex format**: position, color, normal.
- **Per-entity**: Mesh + Transform + optional Material.
- **Per-frame constants** via a Vulkan uniform buffer (descriptor set 0): camera position, up to 4 directional lights, ambient color.
- **Per-entity constants** via push constants: MVP matrix, material albedo/roughness/metallic, texture use flag.
- **Lighting model**: multiple directional lights with ambient + diffuse + Blinn-Phong specular.
- **Material**: `Material.Albedo` tints vertex color, `Roughness` and `Metallic` control specular falloff and intensity; an optional `TexturePath` enables albedo texture sampling.
- **Textures**: PNG files are loaded into Vulkan images with a combined image sampler (descriptor set 1). UVs are derived from vertex position XZ for the floor plane; other meshes use world-space XZ as a simple mapping.

### 4.6 SystemSlotRegistry

```csharp
public class SystemSlotRegistry
{
    private readonly Dictionary<string, Entity> _slots = new();
    private readonly World _world;

    public void Register(string slotName, Entity systemEntity)
    {
        _slots[slotName] = systemEntity;
    }

    public void HotSwap(string slotName, Entity newSystemEntity)
    {
        if (_slots.TryGetValue(slotName, out var oldSystem))
        {
            oldSystem.Destruct(); // Flecs system destructor
        }

        _slots[slotName] = newSystemEntity;
    }

    public IEnumerable<KeyValuePair<string, Entity>> EnumerateSlots()
    {
        return _slots;
    }
}
```

---

## 5. MMLM SELF-DIAGNOSTIC CONTEXT LOOP

### 5.1 Purpose

The `DiagnosticsManager` captures a multimodal **Diagnostic Payload** that gives the AI full context:

- **Visual context** — what the engine is currently rendering
- **Structural context** — the exact ECS world state
- **Code context** — current scripts, logs, and stack traces

### 5.2 Diagnostic Payload Structure

```csharp
public class DiagnosticPayload
{
    public byte[] VisualJpeg { get; set; }        // Semantic scene screenshot
    public byte[] ProfilerJpeg { get; set; }      // ImGui performance graph
    public string WorldJson { get; set; }           // Flecs ECS state
    public string SystemGraphSvg { get; set; }      // System pipeline diagram
    public string ConsoleLogs { get; set; }         // Recent log tail
    public string StackTrace { get; set; }          // Exception trace if any
    public Dictionary<string, string> SourceFiles { get; set; } // Active scripts
}
```

### 5.3 Capture Flow

```
[1] TRIGGER
    │
    ├─ User clicks "AI Inspect" in the editor
    ├─ Unhandled exception occurs
    └─ Automatic capture on frame-time spike
    │
[2] CAPTURE
    │
    ├─ [2a] Visual Layer (Vulkan)
    │    ├─ Allocate off-screen VkImage (R8G8B8A8_UNORM)
    │    ├─ Encode semantic render pass using SemanticClass component
    │    ├─ Use Vulkan dynamic rendering (vkCmdBeginRendering / vkCmdEndRendering)
    │    ├─ Copy image to host-visible staging buffer (vkCmdCopyImageToBuffer)
    │    ├─ Map memory → Span<byte> RGBA
    │    └─ Encode to JPEG via ImageSharp
    │
    ├─ [2b] Visual Layer (Profiler)
    │    ├─ Render ImGui profiler graph to the same RTT pipeline
    │    └─ Encode to JPEG
    │
    ├─ [2c] Structural Layer (Flecs)
    │    ├─ Call ecs_world_to_json(world, &params)
    │    ├─ Marshal native UTF-8 pointer to managed string
    │    └─ Optionally filter by camera frustum
    │
    ├─ [2d] Structural Layer (System Graph)
    │    ├─ Enumerate SystemSlotRegistry
    │    └─ Generate SVG dependency graph
    │
    └─ [2e] Code & Log Layer
         ├─ Read circular log buffer
         ├─ Capture exception stack trace if present
         └─ Read active scripts from /projects/
    │
[3] PACK
    │
    └─ JSON envelope with base64-encoded JPEGs:
        {
          "visual": "<base64>",
          "profiler": "<base64>",
          "world": { ... flecs json ... },
          "systems": "<svg>",
          "logs": "...",
          "error": "...",
          "sources": { "PlayerController.cs": "..." }
        }
    │
[4] DELIVER
    │
    ├─ Engine.Broker HTTP POST /diagnostics
    │    (External script forwards to MMLM API)
    │
    └─ Future: embedded llama.cpp / ONNX Runtime direct inference
```

### 5.4 Timing Budget

| Phase | Target Time |
|-------|-------------|
| Vulkan semantic render pass | 2–5 ms (GPU) |
| Texture readback + JPEG encode | 5–10 ms (CPU) |
| `ecs_world_to_json()` | 1–3 ms (native C) |
| SVG graph generation | <1 ms |
| Log collection | <1 ms |
| **Total** | **~10–20 ms** |

Non-critical captures can be spread across multiple frames to avoid stuttering.

### 5.5 Flecs JSON Advantage

`ecs_world_to_json()` uses **Flecs native C-reflection** instead of C# `System.Reflection`. This means:

- ✅ Works in NativeAOT Release Mode
- ✅ No P/Invoke overhead for serialization itself
- ✅ Schema-stable output, perfect for MMLM prompts
- ✅ Works with runtime-registered components

---

## 6. AI MUTATION GATEWAY

### 6.1 Purpose

The `AiGateway` is the only allowed path for the AI to modify the running engine. It prevents memory corruption, invalid state, and unsafe code execution.

### 6.2 MCP Server (Dev / Release)

The engine can expose its AI commands through two MCP transports:

1. **HTTP MCP server** (Debug/Release): an in-process ASP.NET Core server using `ModelContextProtocol.AspNetCore` with SSE on `http://localhost:<port>/`. Enable with `--mcp-port <port>`.
2. **Stdio MCP server** (Debug/Release): a minimal JSON-RPC server that reads from stdin and writes to stdout. Enable with `--mcp-stdio`. This is the format expected by Claude Desktop and other stdio MCP clients.

Available tools:

- `spawn_model` — spawn a named entity from a model file.
- `set_transform` — update entity position, rotation, scale.
- `set_material` — update entity albedo, roughness, metallic, and texture path.
- `delete_entity` — delete an entity by name.
- `list_entities` — list all named entities with a `Transform`.
- `get_world_state` — dump the ECS world as JSON (Transform, Camera, Material, Light, Mesh).
- `capture_screenshot` — save a PNG of the current frame (HTTP/render mode only).

Commands are queued and executed on the main engine thread so the Flecs world is never touched from a background thread.

### 6.3 Command Pattern (Works in Both Dev and Release)

The AI can also issue declarative JSON commands directly:

```json
{
  "type": "spawn_model",
  "name": "Enemy",
  "modelPath": "Models/enemy.obj",
  "position": [10, 0, 5],
  "rotation": [0, 0, 0, 1],
  "scale": [1, 1, 1]
}
```

Validation steps:

1. JSON schema validation
2. Type existence check via Flecs reflection
3. Coordinate sanity check (e.g., no NaN, no extreme values)
4. Safe-name check (no `..` in model paths)
5. Queue operation for execution at the next frame boundary

### 6.4 Scripting Validation (Dev Mode Only)

Before Roslyn compilation:

1. **Syntax pre-check**: Parse as C# syntax tree.
2. **Banned symbols check**: Disallow `unsafe`, `Marshal`, `File`, `Process`, `Thread`, `Assembly`, `Reflection.Emit`.
3. **Namespace whitelist**: Allow only `Engine.*`, `System`, `System.Numerics`, `Flecs.NET`.
4. **Reference validation**: Ensure all referenced types exist in the engine API surface.
5. **Sandboxed compilation**: Compile into isolated `AssemblyLoadContext`.

### 6.5 Release Mode Limitation

In Release (NativeAOT), the MCP server and ASP.NET Core are excluded. The AI cannot compile new C# code. It can only send JSON commands via `AiCommandProcessor`. This is a deliberate security and stability choice.

---

## 7. LOGICAL PROJECT LAYOUT

```text
/home/emil/Desktop/Cortex_Engine
├── AGENTS.md                              # This file
├── CORTEX_ENGINE_ARCHITECTURE.md          # Mirror / detailed specification
├── src/
│   ├── Engine.Core/
│   │   ├── EngineApp.cs                  # Entry point, main loop
│   │   ├── Sdl3Window.cs                 # SDL3 window wrapper
│   │   ├── Timing.cs                     # DeltaTime, fixed timestep
│   │   ├── InputMapping.cs               # Keyboard, mouse, gamepad input
│   │   ├── ICameraController.cs            # Camera controller interface
│   │   ├── OrbitCameraController.cs      # Mouse orbit camera
│   │   ├── FreeFlyCameraController.cs    # WASD + mouse look camera
│   │   └── Components/                   # Transform, Camera, Light, Material, Mesh
│   │
│   ├── Engine.Data/
│   │   ├── GameObject.cs                 # Thin struct facade
│   │   ├── ComponentTypes.cs             # Transform, MeshRef, Camera, SemanticClass
│   │   ├── WorldContext.cs               # Flecs world initialization
│   │   └── SystemSlotRegistry.cs         # Named system hot-swap registry
│   │
│   ├── Engine.Graphics/
│   │   ├── VulkanContext.cs              # Device, instance, queues, command pool
│   │   ├── Swapchain.cs                  # Swapchain + depth buffer
│   │   ├── MeshRenderer.cs               # ECS mesh rendering
│   │   ├── ScreenshotCapture.cs          # Vulkan readback → PNG
│   │   ├── VulkanPipeline.cs             # Graphics pipeline + descriptor layouts
│   │   ├── UniformBuffer.cs              # Per-frame uniform buffer
│   │   ├── Texture.cs                    # Vulkan texture (image, view, sampler)
│   │   ├── VertexBuffer.cs               # Vertex buffer helpers
│   │   ├── IndexBuffer.cs                # Index buffer helpers
│   │   └── Loaders/                      # ObjLoader, GltfLoader
│   │
│   ├── Engine.Diagnostics/
│   │   ├── DiagnosticsManager.cs         # Orchestrator
│   │   ├── FlecsJsonExporter.cs          # ecs_world_to_json wrapper
│   │   ├── SystemGraphSvg.cs             # SVG dependency graph generator
│   │   ├── Payload.cs                    # DiagnosticPayload class
│   │   └── LogBuffer.cs                  # Circular console log buffer
│   │
│   ├── Engine.AI/
│   │   ├── AiCommandProcessor.cs         # Parses and executes JSON commands
│   │   ├── AiCommandQueue.cs             # Thread-safe command queue
│   │   ├── Mcp/
│   │   │   ├── EngineMcpTools.cs         # MCP HTTP tool definitions
│   │   │   └── McpEngineServerHost.cs    # In-process MCP HTTP server
│   │   ├── Stdio/
│   │   │   └── McpStdioServer.cs         # Minimal stdio MCP server
│   │   ├── Commands/                     # AI command DTOs
│   │   └── Serialization/                # JSON converters for Vector3/Quaternion
│   │
│   ├── Engine.Editor/
│   │   ├── ImGuiController.cs           # Hexa.NET.ImGui initialization
│   │   ├── HierarchyWindow.cs            # Scene hierarchy panel
│   │   ├── InspectorWindow.cs            # Component inspector
│   │   ├── AiConsoleWindow.cs            # AI Co-Developer panel
│   │   └── ProfilerWindow.cs            # Performance graphs
│   │
│   ├── Engine.Broker/
│   │   └── DiagnosticsHttpServer.cs      # Local HTTP endpoint for AI payloads
│   │
│   └── Engine.Physics/
│       └── PhysicsModule.cs              # JoltPhysicsSharp integration (future)
│
├── projects/                              # AI-generated game scripts
│   └── .gitkeep
├── tests/
│   └── Engine.Tests/
├── shaders/
│   ├── semantic.vert.spv
│   └── semantic.frag.spv
└── tools/
    └── svg-generator/                     # Optional CLI tools
```

---

## 8. FOUNDATIONAL MVP — 3 INITIAL CODE STEPS

### Step 1: Engine.Core — Window + Vulkan Context + Clear Screen

**Goal**: A visible window with a functioning Vulkan device and a frame loop that clears the screen to a solid color.

**Deliverables**:

- `EngineApp.cs` — `Init`, `Update`, `Render`, `Shutdown` loop
- `Sdl3Window.cs` — `ppy.SDL3-CS` wrapper (create window, poll events, resize)
- `VulkanContext.cs` — Vortice.Vulkan instance, physical device, logical device, queues
- `Swapchain.cs` — swapchain creation and recreation
- First frame: `vkCmdClearColorImage` → present

**Dependencies**:

- `ppy.SDL3-CS`
- `Vortice.Vulkan`
- `Vortice.VulkanMemoryAllocator` (optional but recommended)

### Step 2: Engine.Data — Flecs World + GameObject + SystemSlotRegistry

**Goal**: A working ECS world with Unity-like access patterns and a hot-swap registry skeleton.

**Deliverables**:

- `GameObject.cs` — readonly struct facade
- `ComponentTypes.cs` — `Transform`, `MeshRef`, `Camera`, `SemanticClass`
- `WorldContext.cs` — Flecs world initialization
- `SystemSlotRegistry.cs` — named system registration and hot-swap
- Test: create 1000 entities, add `Transform`, iterate, print FPS

**Dependencies**:

- `Flecs.NET.Release`

### Step 3: Engine.Diagnostics — DiagnosticsManager + Flecs JSON Export

**Goal**: The MMLM context loop skeleton — captures world state as JSON plus a placeholder visual capture.

**Deliverables**:

- `DiagnosticsManager.cs` — `CapturePayload()` orchestrator
- `FlecsJsonExporter.cs` — `ecs_world_to_json()` wrapper
- `Payload.cs` — unified diagnostic payload structure
- `SystemGraphSvg.cs` — SVG dependency graph generator
- `LogBuffer.cs` — circular console log buffer
- Visual capture stub (placeholder JPEG until Step 1's Vulkan readback is wired)
- Console test: `CapturePayload()` → print JSON + SVG to stdout

**Dependencies**:

- `Flecs.NET.Release`
- `SixLabors.ImageSharp`

---

## 9. VERIFIED DEPENDENCY TABLE

| Component | Package | Version | .NET | AOT | WASM | Mobile | Status |
|-----------|---------|---------|------|-----|------|--------|--------|
| SDL3 | `ppy.SDL3-CS` | 2026.520.0 | 9/10 | ✅ | ✅ | ✅ | Production |
| Vulkan | `Silk.NET.Vulkan` | 2.21.0 | 9/10 | ✅ | ❌ | MoltenVK | Production |
| ImGui | `Hexa.NET.ImGui` | latest | 9/10 | ✅ | ✅ | ✅ | Production |
| ECS | `Flecs.NET.Release` | 4.0.4-build.546 | 8/9 | ✅* | ✅ | ✅ | Production |
| Model loading | `SharpGLTF.Core` | 1.0.6 | 9/10 | ✅ | ✅ | ✅ | Production |
| AI bridge | `ModelContextProtocol` | 1.4.0 | 8/9 | ❌† | ✅ | ✅ | Production |
| Screenshot | `SixLabors.ImageSharp` | 3.1.11 | 9/10 | ✅ | ✅ | ✅ | Production |
| Physics | `JoltPhysicsSharp` | 2.21.0 | 9/10 | ✅ | ❌ | ✅ | Production |
| JPEG | `SixLabors.ImageSharp` | latest | 9/10 | ✅ | ✅ | ✅ | Production |

\* Via `<FlecsStaticLink>true</FlecsStaticLink>`

† MCP server is excluded from `ReleaseAOT` because it depends on ASP.NET Core. JSON-only AI commands still work in AOT via `AiCommandProcessor`.

---

## 10. KNOWN RISKS & MITIGATIONS

| Risk | Impact | Mitigation |
|------|--------|------------|
| NativeAOT + Roslyn conflict | High | Dual-runtime strategy: JIT for dev, AOT for release |
| Flecs AssemblyLoadContext leak | Medium | Type migration before unloading old context |
| MoltenVK limitations | Medium | Use Vulkan 1.3 baseline + portability subset; test on Apple hardware early |
| Vulkan verbosity | Medium | Build a high-level renderer abstraction; let the AI generate systems, not raw Vulkan |
| Hexa.NET.ImGui version drift | Low | Pin version; fork if necessary |
| JoltPhysicsSharp mobile perf | Low | Profile on target devices; use Jolt's SIMD paths |

---

## 11. PROMPT ENGINEERING FOR AI CODING

When generating code with an MMLM for this engine, always include this context header:

```
You are coding for Cortex Engine, a C# (.NET 9) AI-Native multiplatform 3D game engine.

Stack:
- C# .NET 9 with dual-runtime: JIT (Debug) for Roslyn hot-reload, NativeAOT (ReleaseAOT) for JSON-only AI commands
- SDL3-cs (ppy.SDL3-CS) for windowing and input
- Silk.NET.Vulkan for graphics
- Flecs.NET for ECS
- Hexa.NET.ImGui for editor UI
- JoltPhysicsSharp for physics
- ModelContextProtocol for AI tool integration

Rules:
1. All state lives in ECS components (Transform, Camera, Light, Mesh, Material).
2. All AI mutations go through Engine.AI (AiCommandProcessor / MCP tools).
3. All Dev-Mode AI scripts must be AOT-compatible and avoid unsafe, Reflection.Emit, Assembly.Load, File I/O.
4. All systems are registered in SystemSlotRegistry and tagged with [Slot("name")].
5. Use Roslyn syntax trees for validation before compilation.
6. Prefer Flecs native reflection (ecs_world_to_json) over C# reflection.
7. Keep modules isolated; do not create circular dependencies between Engine.Core, Engine.Graphics, Engine.AI.

Current file context: [insert path here]
```

---

## 12. NEXT DECISION POINTS

1. Add ImGui editor UI (`Hexa.NET.ImGui`) for scene hierarchy and inspector.
2. Add physics integration (`JoltPhysicsSharp`) with rigid bodies and colliders.
3. Implement semantic segmentation render pass for AI vision.
4. Add audio module (`NAudio` or `OpenAL` bindings).
5. Add networking / multiplayer foundation.

---

## 13. RUNTIME NOTES & CRITICAL CONTEXT

### 13.1 Building & Running

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DISPLAY=:0
dotnet build CORTEX_ENGINE.sln -c Debug
dotnet run --project src/CortexEngine.App/CortexEngine.App.csproj
```

- `RuntimeIdentifier=linux-x64` is required in Debug to use the bundled native `libSDL3.so` from `ppy.SDL3-CS` (system `libSDL3.so.3.4.2` is ABI-incompatible).
- AOT builds: `dotnet build CORTEX_ENGINE.sln -c ReleaseAOT`.

### 13.2 CLI Arguments

- `--mcp-port <port>` — start the HTTP MCP server on `http://localhost:<port>/` (SSE).
- `--mcp-stdio` — run the headless stdio MCP server for Claude Desktop / other stdio clients.
- Any other positional argument is treated as a model path (`.obj`, `.gltf`, `.glb`).

### 13.3 Vulkan & Shader Pipeline

- Pipeline layout uses **two descriptor sets**: set 0 = per-frame uniform buffer (camera + lights), set 1 = per-entity combined image sampler.
- Push constants: 96 bytes (`mat4 mvp` + material albedo/roughness/metallic + texture flag + padding), stages `VertexBit | FragmentBit`.
- Uniform buffer: std140 224 bytes (`cameraPosition`, `lightCount`, `ambientColor`, up to 4 `Light` structs).
- Shaders are compiled with `glslangValidator`:
  ```bash
  /tmp/glslang/bin/glslangValidator -V src/Engine.Graphics/Shaders/vertex.vert -o src/Engine.Graphics/Shaders/vertex.spv
  /tmp/glslang/bin/glslangValidator -V src/Engine.Graphics/Shaders/fragment.frag -o src/Engine.Graphics/Shaders/fragment.spv
  ```

### 13.4 SDL3 Input

- `SDL3 2026.520.0` API: `SDL_Init` returns `SDLBool`, `SDL_PollEvent` returns `SDLBool`, `evt.type` is `uint`.
- Keyboard: `evt.key.key`; Mouse: `evt.motion.x`, `evt.motion.y`, `evt.wheel.y`.
- **Orbit camera** (по умолчанию): правый клик + движение мыши — вращать, колесо — zoom.
- **FreeFly camera** (переключается клавишей `F`): `WASD` — двигаться, `Q`/`E` — вниз/вверх, `Shift` — ускорение, правый клик + мышь — осмотр.
- `ESC` — выход.

### 13.5 MCP Client Config

Sample Claude Desktop config (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "cortex-engine": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/emil/Desktop/Cortex_Engine/src/CortexEngine.App/CortexEngine.App.csproj",
        "--",
        "--mcp-stdio"
      ],
      "env": {
        "DOTNET_ROOT": "/home/emil/.dotnet",
        "PATH": "/home/emil/.dotnet:/usr/bin:/bin"
      }
    }
  }
}
```

For the HTTP MCP server, use the `--mcp-port` argument and connect an SSE MCP client.

### 13.6 Process Cleanup

Background `dotnet run` processes may leave the apphost running. Kill them with:

```bash
ps -C CortexEngine.App -o pid= | xargs -r kill -9
```

---

*This document is the canonical architecture reference for Cortex Engine. Any changes to stack, project layout, or core data flow must be reflected here before implementation.*
