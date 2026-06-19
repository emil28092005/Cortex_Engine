# CORTEX ENGINE — Technical Architecture

**Project**: AI-Native 3D Game Engine
**Language**: C# (.NET 9)
**Render Backend**: Pure P/Invoke Vulkan 1.3
**Last Updated**: June 2026

---

## 1. Overview

Cortex Engine is a 3D game engine built from scratch with a pure P/Invoke Vulkan 1.3 render backend. No wrapper libraries — direct Vulkan API calls via `vkGetInstanceProcAddr`/`vkGetDeviceProcAddr`.

The engine allows an AI to:
- **See** the engine state via rendered-frame screenshots and video recording
- **Read** the complete ECS world state through native JSON serialization
- **Modify** the running engine via declarative JSON commands through MCP (Model Context Protocol)

### Key Technologies
- **Graphics**: Vulkan 1.3 (dynamic rendering, synchronization2, imageCubeArray)
- **ECS**: Flecs.NET 4.0.4
- **Windowing**: SDL3 (ppy.SDL3-CS 2026.520.0)
- **Physics**: JoltPhysicsSharp 2.21.0
- **UI**: ImGui.NET 1.91.6.1
- **AI**: MCP HTTP server (Kestrel + SSE), 7 tools

---

## 2. Project Structure

```
src/
├── Engine.Core/              # Core abstractions, windowing, ECS components
│   ├── IWindow.cs            # Backend-agnostic window interface
│   ├── IInputState.cs        # Input abstraction
│   ├── Sdl3Window.cs         # SDL3 window with Vulkan surface
│   ├── Key.cs                # Key enum
│   ├── InputMapping.cs       # Input state implementation
│   ├── Timing.cs             # Frame timing
│   ├── Vertex.cs             # Vertex struct (Position, Color, Normal)
│   ├── FreeFlyCameraController.cs
│   ├── OrbitCameraController.cs
│   └── Components/
│       ├── Transform.cs      # Position, Rotation, Scale
│       ├── Mesh.cs           # Vertex[], uint[] indices
│       ├── Material.cs       # Albedo, Roughness, Metallic, TexturePath
│       ├── Light.cs          # Point/Directional light
│       ├── Camera.cs         # Perspective camera
│       └── RigidBody.cs      # Physics body (box/sphere)
│
├── Engine.Graphics/         # Graphics HAL + utilities
│   ├── IRenderContext.cs     # Window, CreateRenderer, Resize
│   ├── IRenderer.cs         # RenderWorld, Screenshot, ImGui hooks
│   ├── IScreenshotProvider.cs
│   ├── RenderBackendFactory.cs  # Register/Create by name
│   ├── ObjLoader.cs          # OBJ file parser
│   ├── MeshMath.cs           # Face normal computation
│   ├── ProceduralMesh.cs     # Sphere, Grid generators
│   └── SceneSerializer.cs   # JSON scene save/load
│
├── Engine.Graphics.Vulkan/  # Pure P/Invoke Vulkan 1.3
│   ├── VulkanNative.cs       # Library loading, vkGetInstanceProcAddr
│   ├── VulkanHandles.cs      # Opaque pointer types
│   ├── VulkanEnums.cs        # All Vulkan enums/flags
│   ├── VulkanStructs.cs      # All Vulkan structs (verified sizes)
│   ├── Vk.cs                 # Function delegates + loaded pointers
│   ├── VulkanContext.cs      # Instance, device, queue, surface, debug
│   ├── VulkanSwapchain.cs    # Swapchain, image views, depth buffer
│   ├── VulkanPipeline.cs     # Graphics pipeline (PBR + dynamic rendering)
│   ├── VulkanShadowMap.cs    # Cubemap array shadows (4 lights × 6 faces)
│   ├── VulkanFrameResources.cs  # Command buffers, fences, semaphores, UBO
│   ├── VulkanVertexBuffer.cs # Staging → device-local vertex buffer
│   ├── VulkanIndexBuffer.cs  # Staging → device-local index buffer
│   ├── VulkanImGui.cs        # ImGui font atlas + pipeline + render
│   ├── VulkanRenderer.cs     # Main renderer: shadow passes + main pass
│   ├── VulkanRenderContext.cs
│   ├── VulkanBackendRegistrar.cs
│   └── Shaders/
│       ├── triangle.vert/frag  # PBR + multi-light + shadow sampling
│       ├── shadow.vert/frag    # Depth-only shadow pass
│       └── imgui.vert/frag     # ImGui rendering
│
├── Engine.Physics/          # Jolt physics wrapper
│   └── PhysicsWorld.cs      # Body creation, update, sync transforms
│
├── Engine.AI/               # AI command system + MCP server
│   ├── AiCommandProcessor.cs   # 7 commands (spawn, transform, material, etc.)
│   ├── AiCommandQueue.cs       # Thread-safe queue (MCP → main thread)
│   ├── Commands/               # Command DTOs
│   ├── Mcp/                    # HTTP MCP server (Kestrel + SSE)
│   └── Stdio/                  # Stdio MCP server
│
└── CortexEngine.App/        # Entry point
    └── Program.cs            # Main loop, scene, ImGui panels, video recording
```

---

## 3. Vulkan Backend

### 3.1 Initialization

1. **Load `libvulkan.so.1`** via `NativeLibrary.Load()`
2. **`vkGetInstanceProcAddr`** — only directly-loaded function
3. **Create instance** with SDL3 extensions + `VK_EXT_debug_utils` (if validation available)
4. **Pick physical device** — prefer `DiscreteGpu`
5. **Create logical device** with features:
   - `VK_KHR_swapchain` extension
   - `VkPhysicalDeviceDynamicRenderingFeatures` (dynamic rendering)
   - `VkPhysicalDeviceSynchronization2Features` (sync2)
   - `VkPhysicalDeviceFeatures.imageCubeArray = VK_TRUE` (cubemap array shadows)
6. **Create surface** via `SDL_Vulkan_CreateSurface()`

### 3.2 Frame Loop

```
Each frame:
  1. WaitFrame (fence)
  2. Read captured frame buffer (if recording video)
  3. AcquireNextImageKHR
  4. Begin command buffer
  5. Shadow passes (numShadowLights × 6 faces):
     - Transition shadow cubemap array → ColorAttachment + DepthAttachment
     - For each shadow light, for each face:
       - Begin rendering (color R32_SFLOAT + depth D32_SFLOAT)
       - Bind shadow pipeline (depth-only, CULL_NONE, depth bias)
       - Push constants: model + lightViewProj + lightPos + shadowParams (160B)
       - Draw all shadow-casting objects
       - End rendering
     - Transition shadow cubemap array → ShaderReadOnlyOptimal
  6. Main pass:
     - Transition swapchain image → ColorAttachmentOptimal
     - Transition depth image → DepthStencilAttachmentOptimal
     - Begin rendering (color B8G8R8A8_UNORM + depth D32_SFLOAT)
     - Bind pipeline, descriptor sets (SceneUBO + shadowCubeArray)
     - For each object: push constants (model 64B), draw indexed
     - [If recording] End rendering, capture frame (vkCmdCopyImageToBuffer)
     - [If recording] Begin new rendering with loadOp=LOAD for ImGui
     - ImGui render
     - End rendering
     - Transition swapchain → PresentSrcKHR
  7. End command buffer
  8. QueueSubmit2 (sync2)
  9. QueuePresentKHR
```

### 3.3 SceneUBO Layout (464B)

```
Offset 0:    mat4 vp                    (64B)
Offset 64:   int numLights              (4B)
Offset 68:   int numShadowLights        (4B)
Offset 72:   vec2 padding               (8B)
Offset 80:   LightData lights[8]        (256B) — 2 × vec4 per light
Offset 336:  vec4 shadowParams[4]       (64B) — bias, sampleRadius, farPlane
Offset 400:  vec4 ambientColor          (16B)
Total: 416B → padded to 464B (align 64)
```

### 3.4 Push Constants

**Main pipeline**: `mat4 model` (64B), `Vertex|Fragment`
**Shadow pipeline**: `mat4 model` + `mat4 lightViewProj` + `vec4 lightPos` + `vec4 shadowParams` (160B), `Vertex|Fragment`

### 3.5 Shadow Mapping

- **Cubemap array**: 1 image, 24 layers (4 lights × 6 faces), `CubeCompatible` flag
- **Color attachment**: R32_SFLOAT (stores linear distance / farPlane)
- **Depth attachment**: D32_SFLOAT (depth test only)
- **Sampling**: `samplerCubeArray`, `texture(shadowArray, vec4(dir, lightIndex))`
- **Filtering**: 16-tap Poisson disk PCF, slope-dependent bias
- **Vulkan cubemap conventions**: up=(0,-1,0) for X/Z faces, up=(0,0,1) for +Y, up=(0,0,-1) for -Y

### 3.6 PBR Shading

Cook-Torrance BRDF:
- **D**: Trowbridge-Reitz GGX distribution
- **G**: Smith geometry with Schlick-GGX
- **F**: Schlick Fresnel approximation
- **Tonemapping**: ACES filmic
- **Gamma**: 2.2 correction
- Multi-light loop in fragment shader

---

## 4. AI/MCP Integration

### 7 MCP Tools

| Tool | Description |
|---|---|
| `spawn_model` | Create entity with mesh + optional physics + shape (cube/sphere) |
| `set_transform` | Update position/rotation/scale by name |
| `set_material` | Update albedo/roughness/metallic/texture |
| `delete_entity` | Remove entity by name |
| `list_entities` | List all named entities |
| `get_world_state` | Full JSON state dump |
| `capture_screenshot` | Request screenshot |

### Threading

- MCP server runs on `Task.Run` (Kestrel thread pool)
- Commands enqueued via `AiCommandQueue` (thread-safe `ConcurrentQueue`)
- Main thread calls `ProcessPending()` before render each frame
- `CompletePendingScreenshots()` after render

---

## 5. Physics

- JoltPhysicsSharp 2.21.0
- `PhysicsWorld`: gravity (0, -9.81, 0), 2 object layers (NonMoving, Moving)
- `RigidBody` component: DynamicBox, DynamicSphere, StaticBox, StaticPlane
- Lazy initialization: `IsInitialized` flag, bodies created on first frame
- `SyncTransforms`: pulls Jolt positions/rotations back into ECS Transforms
- Pause/Resume via `physicsEnabled` flag
- Reset Scene: `RemoveBody` before `Destruct` to clean Jolt references

---

## 6. ImGui

- Separate pipeline with alpha blending, no depth write
- Font atlas uploaded to R32_SFLOAT VkImage via staging buffer
- Mouse input from `IInputState` fed to `ImGui.GetIO()` each frame
- Debug panel: FPS, camera, entity count, physics toggle, reset scene
- Shadow & Light panel: per-light intensity/range/RGB, shadow bias/radius/farplane, ambient RGB
- Video recording panel: Start/Stop buttons

---

## 7. Video Recording

- `vkCmdCopyImageToBuffer` copies swapchain image to HOST_VISIBLE buffer
- BGRA pixel data piped to FFmpeg via stdin
- FFmpeg: `rawvideo bgra → libx264 yuv420p`, 60 FPS input, 30 FPS output
- Capture before ImGui (video without UI)
- Deferred read: buffer read at start of next frame (after fence wait)

---

## 8. Content

| File | Description |
|---|---|
| `cube.obj` | Unit cube, CCW winding |
| `sphere.obj` | UV sphere 24×16, CCW winding |
| `torusknot.obj` | Torus knot (3,2), 4800 verts, smooth normals |
| `torus.obj` | Donut, 24×16 segments, CCW winding |
| `pyramid.obj` | 4-sided pyramid, CCW winding |
| `diamond.obj` | Octahedron (8 faces), CCW winding |
| `cone.obj` | 24-sector cone with bottom cap, CCW winding |

---

## 9. Testing

227 xUnit tests:
- **VulkanStructSizeTests** (47): C# struct sizes match C Vulkan headers
- **VulkanEnumValueTests** (40+): sType, format, layout, topology, sync2 flags
- **VertexLayoutTests** (5): Vertex struct size (36B), field offsets
- **ObjLoaderTests** (8): OBJ parsing, face normals, winding
- **ObjLoaderFaceNormalTests** (5): Face normal computation
- **ShadowMapTests** (10): Face directions, FOV, up vectors, far plane
- **ShadowShaderTests** (5): SPIR-V shader existence
- **CameraTests, TransformTests, TimingTests, CameraControllerTests, AiCommandProcessorTests, RenderBackendFactoryTests, MeshMathAndProceduralTests, SceneSerializerTests**: Core functionality
