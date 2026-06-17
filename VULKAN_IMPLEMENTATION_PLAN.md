# CORTEX ENGINE — VULKAN RENDERER IMPLEMENTATION PLAN

## Project State (June 2026)

### What Exists
- **Engine.Core** — Sdl3Window (SDL3, Vulkan surface ready), IWindow, IInputState, Key enum, InputMapping, 
  camera controllers (FreeFly, Orbit), components (Transform, Mesh, Material, Light, Camera, RigidBody),
  Vertex struct (Position, Color, Normal — 9 floats), Timing, IScreenshotProvider
- **Engine.Physics** — JoltPhysicsSharp 2.21.0, PhysicsWorld wrapper, RigidBody component
- **Engine.AI** — AiCommandProcessor (7 commands), MCP HTTP + stdio servers, AiCommandQueue
- **CortexEngine.App** — main loop (broken, references deleted graphics projects)
- **tests/Engine.Tests** — 66 tests (broken, reference Engine.Graphics)
- **Content/** — cube.obj, torusknot.obj, checker.png

### What Was Deleted
- Engine.Graphics (interfaces + loaders + factory)
- Engine.Graphics.Raylib
- Engine.Graphics.OpenTK  
- Engine.Graphics.Vulkan (Silk.NET version)

### Environment
- .NET 9 SDK at $HOME/.dotnet
- Vulkan 1.4.329, NVIDIA RTX 2080 Ti, validation layers available
- SDL3 (ppy.SDL3-CS 2026.520.0) — window + Vulkan surface
- glslangValidator NOT installed (need: sudo apt install glslang-tools)
- Linux (X11), cross-platform target (Windows: vulkan-1.dll, Linux: libvulkan.so.1)

### Key Architecture Decisions
- NO wrapper libraries (no Silk.NET, no Vortice, no OpenTK for Vulkan)
- Pure P/Invoke to libvulkan.so.1 / vulkan-1.dll
- SDL3 for windowing (Sdl3Window already works, creates Vulkan surface)
- ImGui planned (later phase)
- Shadow mapping planned (Vulkan gives full control)
- Validation layers for debugging

## Implementation Plan

### Phase 1: Restore Engine.Graphics (interfaces + loaders)

Create `src/Engine.Graphics/` with:
- `IRenderContext.cs` — interface: Window, CreateRenderer(), Resize(), Dispose
- `IRenderer.cs` — interface: RenderWorld(World), RequestScreenshot, IsScreenshotRequested, ScreenshotProvider, Dispose  
- `RenderBackendFactory.cs` — static registry: Register(name, factory), Create(name, w, h, validation)
- `IScreenshotProvider.cs` already in Engine.Core
- `Loaders/ObjLoader.cs` — parse .obj files → Mesh component
- `Loaders/GltfLoader.cs` — parse .gltf/.glb → Mesh component
- `MeshMath.cs` — ComputeFaceNormal(a, b, c)
- `ProceduralMesh.cs` — CreateSphere(), CreateGrid()
- `SceneSerializer.cs` — save/load ECS world to JSON

csproj: references Engine.Core, Flecs.NET, SharpGLTF.Core

### Phase 2: Vulkan P/Invoke Layer

Create `src/Engine.Graphics.Vulkan/` with:
- `VulkanNative.cs` — all P/Invoke declarations:
  - Library loading: `const string VulkanLib = OperatingSystem.IsWindows() ? "vulkan-1.dll" : "libvulkan.so.1"`
  - ~80 Vulkan functions (vkCreateInstance through vkQueuePresentKHR)
  - ~50 structs (InstanceCreateInfo, DeviceCreateInfo, SwapchainCreateInfoKHR, etc.)
  - ~20 enums (Result, Format, ImageLayout, PipelineStageFlags, etc.)
  - Extension function loading via vkGetInstanceProcAddr/vkGetDeviceProcAddr
  - SDL_Vulkan_CreateSurface via SDL3 (already in Sdl3Window)

- `VulkanContext.cs` — Instance + PhysicalDevice + Device + Queues + Surface:
  - CreateInstance with SDL3 extensions + validation layers
  - PickPhysicalDevice (prefer discrete GPU)
  - CreateLogicalDevice with VK_KHR_swapchain
  - CreateSurface via SDL_Vulkan_CreateSurface
  - Get graphics + present queues

- `VulkanSwapchain.cs` — Swapchain + image views + depth + render pass + framebuffers:
  - Query surface capabilities
  - Create swapchain (format, extent, present mode)
  - Create image views
  - Create depth image + view (D32_SFLOAT)
  - Create render pass (color + depth attachments)
  - Create framebuffers

- `VulkanPipeline.cs` — Graphics pipeline:
  - Load SPIR-V shader modules (vertex + fragment)
  - Vertex input description (Position vec3, Normal vec3, Color vec4)
  - Descriptor set layouts (frame UBO + texture sampler)
  - Pipeline layout + graphics pipeline
  - Push constants for MVP matrix + material params

- `VulkanBuffer.cs` — Buffer management:
  - CreateBuffer (vertex/index/uniform)
  - AllocateMemory + bind
  - Map/unmap for writing
  - FindMemoryType
  - Staging buffer for copy

- `VulkanRenderer.cs` — Render loop:
  - Command pool + command buffers (2 frames in flight)
  - Semaphores + fences for sync
  - Descriptor pools + sets
  - RenderWorld(World):
    - Get camera from ECS
    - Collect lights from ECS
    - Update frame UBO (camera pos, lights)
    - For each Mesh+Transform entity: upload/cache vertex+index buffers, 
      set push constants (MVP + material), draw indexed
  - Screenshot capture (copy image to staging buffer → PNG)
  - Present

- `VulkanBackendRegistrar.cs` — Register("vulkan", factory)

csproj: references Engine.Core, Engine.Graphics, Flecs.NET
NO external Vulkan packages — pure P/Invoke

### Phase 3: Shaders

GLSL → SPIR-V shaders (compiled with glslangValidator):
- `Shaders/vertex.vert` — #version 450, position/normal/color inputs, MVP+model uniforms, outputs
- `Shaders/fragment.frag` — #version 450, PBR lighting (Fresnel, ACES, gamma), directional + point lights
- Compile: `glslangValidator -V vertex.vert -o vertex.spv && glslangValidator -V fragment.frag -o fragment.spv`
- Embed .spv files as project resources or copy to output directory

### Phase 4: App Integration

- Fix `CortexEngine.App.csproj` — remove deleted project refs, add Engine.Graphics + Engine.Graphics.Vulkan
- Fix `Program.cs`:
  - Remove all Raylib/OpenTK/old OpenGL imports
  - Remove ImGuiLayer, ObjectManipulator (Raylib-specific, will reimplement later)
  - Use `RenderBackendFactory.Create("vulkan", 1280, 720, enableValidation: true)`
  - Keep: physics, camera controllers, AI commands, tour mode, scene setup
  - Sdl3Window creates Vulkan surface automatically (vulkanSurface: true)

### Phase 5: Fix Tests

- Update `Engine.Tests.csproj` — reference restored Engine.Graphics
- Tests that reference Engine.Graphics: ObjLoaderTests, RenderBackendFactoryTests, 
  SceneSerializerTests, MeshMathAndProceduralTests
- All 66 tests should pass after Engine.Graphics is restored

### Phase 6: ImGui (later)

- ImGui.NET NuGet + Vulkan ImGui backend
- ImGui_ImplVulkan for rendering
- Entity inspector, hierarchy, debug overlay

### Phase 7: Shadow Mapping (later)

- Depth-only render pass from light's POV
- Shadow image (depth texture, 2048x2048)
- Shadow matrix (lightViewProj) in push constants
- PCF sampling in fragment shader

## Cross-Platform Notes

- Vulkan P/Invoke: only difference is library name (vulkan-1.dll vs libvulkan.so.1)
- SDL3: already cross-platform (ppy.SDL3-CS)
- SPIR-V: binary format, works everywhere
- .NET 9: NativeLibrary.Load for dynamic resolution if needed

## File Layout

```
src/
├── Engine.Core/          (exists, unchanged)
├── Engine.Graphics/      (new — interfaces + loaders)
│   ├── Engine.Graphics.csproj
│   ├── IRenderContext.cs
│   ├── IRenderer.cs
│   ├── RenderBackendFactory.cs
│   ├── MeshMath.cs
│   ├── ProceduralMesh.cs
│   ├── SceneSerializer.cs
│   └── Loaders/
│       ├── ObjLoader.cs
│       └── GltfLoader.cs
├── Engine.Graphics.Vulkan/  (new — pure Vulkan P/Invoke)
│   ├── Engine.Graphics.Vulkan.csproj
│   ├── VulkanNative.cs        (~800 lines)
│   ├── VulkanContext.cs       (~300 lines)
│   ├── VulkanSwapchain.cs     (~250 lines)
│   ├── VulkanPipeline.cs      (~200 lines)
│   ├── VulkanBuffer.cs        (~150 lines)
│   ├── VulkanRenderer.cs      (~400 lines)
│   ├── VulkanBackendRegistrar.cs
│   └── Shaders/
│       ├── vertex.vert
│       ├── fragment.frag
│       ├── vertex.spv
│       └── fragment.spv
├── Engine.Physics/       (exists, unchanged)
├── Engine.AI/            (exists, unchanged)
└── CortexEngine.App/     (fix references)
```

## Solution Update

Remove from solution:
- Engine.Graphics.Raylib (deleted)
- Engine.Graphics.OpenTK (deleted)
- Engine.Graphics.Vulkan (old Silk.NET, deleted)

Add to solution:
- Engine.Graphics (new)
- Engine.Graphics.Vulkan (new, pure P/Invoke)

## Key Technical Details

### Matrix Layout
- System.Numerics.Matrix4x4 is row-major
- Vulkan expects column-major in shaders (layout(row_major) or transpose)
- Solution: use `layout(row_major) uniform mat4` in GLSL → no transpose needed
- OR transpose in C# before writing to uniform buffer

### Vertex Layout
```
Vertex struct (9 floats, 36 bytes):
  Position: vec3 (offset 0)
  Color:    vec3 (offset 12)  
  Normal:   vec3 (offset 24)
```

### Push Constants (96 bytes max)
```
offset 0:  mat4 MVP (64 bytes)
offset 64: vec3 materialAlbedo + float roughness (16 bytes)
offset 80: float metallic + uint useTexture + uint pad + uint pad (16 bytes)
```

### Frame UBO (224 bytes)
```
offset 0:   vec3 cameraPosition + uint lightCount (16 bytes)
offset 16:  vec3 ambientColor + float pad (16 bytes)
offset 32:  Light[4] — each 48 bytes (vec3 direction + float intensity + vec3 color + float pad)
```

### Light Struct (48 bytes)
```
vec3 direction (12 bytes)
float intensity (4 bytes)
vec3 color (12 bytes)
float padding (4 bytes)
```

### Validation Layers
```csharp
string[] layers = enableValidation 
    ? new[] { "VK_LAYER_KHRONOS_validation" } 
    : Array.Empty<string>();
```
Validation errors print to stderr — use for debugging.

### Memory Allocation
Simple approach (no VMA):
1. vkGetPhysicalDeviceMemoryProperties
2. Find memory type with VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | HOST_COHERENT_BIT
3. vkAllocateMemory + vkBindBufferMemory
4. vkMapMemory for writing, vkUnmapMemory

For GPU-only buffers (vertex/index):
1. Find memory type with DEVICE_LOCAL_BIT
2. Use staging buffer (host visible) + vkCmdCopyBuffer
