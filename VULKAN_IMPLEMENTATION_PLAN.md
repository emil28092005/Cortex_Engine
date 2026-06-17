# CORTEX ENGINE — VULKAN RENDERER IMPLEMENTATION PLAN

## Project State (June 2026)

### What Exists
- **Engine.Core** — Sdl3Window (SDL3, Vulkan surface ready), IWindow, IInputState, Key enum, InputMapping,
  camera controllers (FreeFly, Orbit), components (Transform, Mesh, Material, Light, Camera, RigidBody),
  Vertex struct (Position, Color, Normal — 9 floats), Timing, IScreenshotProvider
- **Engine.Graphics** — Restored minimal interfaces: IRenderContext, IRenderer, RenderBackendFactory,
  IScreenshotProvider, SceneSerializer, MeshMath, ProceduralMesh, Loaders/ObjLoader
- **Engine.Physics** — JoltPhysicsSharp 2.21.0, PhysicsWorld wrapper, RigidBody component
- **Engine.AI** — AiCommandProcessor (7 commands), MCP HTTP + stdio servers, AiCommandQueue
- **CortexEngine.App** — main loop (broken, references deleted graphics projects)
- **tests/Engine.Tests** — 66 tests (broken, reference Engine.Graphics)
- **Content/** — cube.obj, torusknot.obj, checker.png

### What Was Deleted
- Engine.Graphics.Raylib
- Engine.Graphics.OpenTK
- Engine.Graphics.Vulkan (Silk.NET version — all previous PBR/ImGui/mesh/screenshot code gone)

### Environment
- .NET 9 SDK at `$HOME/.dotnet`
- Vulkan 1.4.329, NVIDIA RTX 2080 Ti, validation layers available
- SDL3 (ppy.SDL3-CS 2026.520.0) — window + Vulkan surface
- glslangValidator: check availability (`glslangValidator --version`); fallback: `glslc`
- Linux (X11), cross-platform target (Windows: `vulkan-1.dll`, Linux: `libvulkan.so.1`)

---

## Key Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Vulkan version | **1.3** | Dynamic rendering (no VkRenderPass/VkFramebuffer), synchronization2, extended dynamic state. All modern GPUs (2022+) support it. |
| Wrapper libraries | **None** | Pure P/Invoke to `libvulkan.so.1` / `vulkan-1.dll`. No Silk.NET, Vortice, OpenTK. |
| Windowing | **SDL3** (ppy.SDL3-CS) | Already integrated, Vulkan surface support built in. |
| Type organisation | **Multiple files** | `VulkanHandles.cs`, `VulkanEnums.cs`, `VulkanStructs.cs` — easier to maintain. |
| Debug | **Full debug messenger** | `VK_EXT_debug_utils` with callback printing validation messages to console (Debug only). |
| Memory | **Staging buffer from start** | Staging buffer (HOST_VISIBLE) → command buffer copy → device-local vertex buffer. Correct pattern from day one. |
| Frame loop | **Re-record every frame** | Vulkan Guide recommends fresh command buffers per frame over reuse. Simpler, no cache invalidation logic. |
| Semaphore indexing | **Per-swapchain-image for submit** | Critical: submit semaphores indexed by swapchain image index, NOT frame-in-flight index. (Vulkan Guide §swapchain_semaphore_reuse) |
| Render pass | **Dynamic rendering** | `vkCmdBeginRendering` / `vkCmdEndRendering` (Vulkan 1.3). No VkRenderPass or VkFramebuffer objects. |
| Synchronisation API | **synchronization2** | `VkImageMemoryBarrier2`, `vkCmdPipelineBarrier2` — cleaner, 64-bit flags. (Vulkan 1.3) |

---

## Implementation Phases

### Phase 1: Vulkan P/Invoke Foundation

Create `src/Engine.Graphics.Vulkan/` with the following files:

#### 1.1 `VulkanNative.cs`
- Load `libvulkan.so.1` (Linux) / `vulkan-1.dll` (Windows) via `NativeLibrary.Load()`
- Export `vkGetInstanceProcAddr` delegate — the only directly-loaded function
- Helper: `GetExport<T>(string name)` for static exports
- Helper: `ToUtf8Terminated(string)` for passing string names to Vulkan

#### 1.2 `VulkanHandles.cs`
Opaque pointer handles (all are `nint` / `ulong`):
```
VkInstance, VkPhysicalDevice, VkDevice, VkQueue,
VkCommandPool, VkCommandBuffer,
VkSwapchainKHR, VkSurfaceKHR,
VkImage, VkImageView,
VkBuffer, VkDeviceMemory,
VkShaderModule, VkPipelineLayout, VkPipeline,
VkSemaphore, VkFence,
VkDebugUtilsMessengerEXT,
VkDescriptorSetLayout, VkDescriptorPool, VkDescriptorSet
```
Each defined as `struct VkXxx { public nint Handle; }` or `using VkXxx = System.IntPtr;`

#### 1.3 `VulkanEnums.cs`
All enums needed for triangle + future expansion:
- `VkResult` — Success=0, NotReady, Timeout, Incomplete, ErrorOutOfDateKHR, SuboptimalKHR, ErrorSurfaceLostKHR, ...
- `VkStructureType` — ApplicationInfo=0, InstanceCreateInfo=1, DeviceQueueCreateInfo=2, DeviceCreateInfo=3, ...
- `VkFormat` — Undefined=0, R8G8B8A8Unorm=37, B8G8R8A8Unorm=44, R8G8B8A8Srgb=43, B8G8R8A8Srgb=50, R32G32Sfloat=103, R32G32B32Sfloat=106, R32G32B32A32Sfloat=109, D32Sfloat=126, ...
- `VkColorSpaceKHR` — SrgbNonlinear=0
- `VkPresentModeKHR` — Immediate=0, Mailbox=1, Fifo=2, FifoRelaxed=3
- `VkImageUsageFlags` — TransferSrc, TransferDst, ColorAttachment, ...
- `VkImageLayout` — Undefined=0, General=1, ColorAttachmentOptimal=2, TransferSrcOptimal=6, TransferDstOptimal=7, PresentSrcKHR=1000001002, ...
- `VkImageAspectFlags` — Color=1, Depth=2
- `VkAttachmentLoadOp` — Load=0, Clear=1, DontCare=2
- `VkAttachmentStoreOp` — Store=0, DontCare=1
- `VkSharingMode` — Exclusive=0, Concurrent=1
- `VkCompositeAlphaFlagsKHR` — Opaque=1, ...
- `VkSurfaceTransformFlagsKHR` — Identity=1, ...
- `VkPrimitiveTopology` — PointList=0, LineList=1, TriangleList=3, ...
- `VkPolygonMode` — Fill=0, Line=1, Point=2
- `VkCullModeFlags` — None=0, Front=1, Back=2, FrontAndBack=3
- `VkFrontFace` — CounterClockwise=0, Clockwise=1
- `VkBlendFactor` — Zero=0, One=1, SrcAlpha=6, OneMinusSrcAlpha=7, ...
- `VkBlendOp` — Add=0, ...
- `VkColorComponentFlags` — R=1, G=2, B=4, A=8
- `VkShaderStageFlags` — Vertex=1, Fragment=0x10, AllGraphics=0x1F
- `VkPipelineStageFlags2` — None=0, TopOfPipe=1, ColorAttachmentOutput=0x400, AllGraphics=0x8000, Transfer=0x10000, ...
- `VkAccessFlags2` — None=0, ColorAttachmentWrite=0x400, TransferWrite=0x1000, ...
- `VkDynamicState` — Viewport=0, Scissor=1, ...
- `VkCommandBufferLevel` — Primary=0, Secondary=1
- `VkCommandBufferUsageFlags` — OneTimeSubmit=1, ...
- `VkFenceCreateFlags` — Signaled=1
- `VkMemoryPropertyFlags` — DeviceLocal=1, HostVisible=2, HostCoherent=4, HostCached=8
- `VkBufferUsageFlags` — TransferSrc=1, TransferDst=2, VertexBuffer=0x80, IndexBuffer=0x40, UniformBuffer=0x10
- `VkQueueFlags` — Graphics=1, Compute=2, Transfer=4
- `VkPhysicalDeviceType` — Other=0, IntegratedGpu=1, DiscreteGpu=2, ...
- `VkSampleCountFlags` — Count1=1
- `VkImageViewType` — Type2D=1
- `VkComponentSwizzle` — Identity=0, ...
- `VkBool32` — False=0, True=1
- `VkRenderingFlags` — None=0, ContentsSecondaryCommandBuffers=1
- `VkPipelineBindPoint` — Graphics=0, Compute=1
- `VkDescriptorType` — UniformBuffer=6, StorageBuffer=7, CombinedImageSampler=0, ...
- `VkDescriptorPoolCreateFlags` — FreeDescriptorSet=1, ...

#### 1.4 `VulkanStructs.cs`
All structs with `LayoutKind.Sequential`:
- `VkApplicationInfo` — sType, pNext, pApplicationName, applicationVersion, pEngineName, engineVersion, apiVersion
- `VkInstanceCreateInfo` — sType, pNext, flags, pApplicationInfo, enabledLayerCount, ppEnabledLayerNames, enabledExtensionCount, ppEnabledExtensionNames
- `VkDebugUtilsMessengerCreateInfoEXT` — sType, pNext, flags, messageSeverity, messageType, pfnUserCallback, pUserData
- `VkDeviceQueueCreateInfo` — sType, pNext, flags, queueFamilyIndex, queueCount, pQueuePriorities
- `VkDeviceCreateInfo` — sType, pNext, flags, queueCreateInfoCount, pQueueCreateInfos, enabledLayerCount, ppEnabledLayerNames, enabledExtensionCount, ppEnabledExtensionNames, pEnabledFeatures
- `VkPhysicalDeviceFeatures` — all VkBool32 (can be zeroed for triangle)
- `VkPhysicalDeviceDynamicRenderingFeatures` — sType, pNext, dynamicRendering (VkBool32) — needed to enable dynamic rendering
- `VkSwapchainCreateInfoKHR` — sType, pNext, flags, surface, minImageCount, imageFormat, imageColorSpace, imageExtent, imageArrayLayers, imageUsage, imageSharingMode, queueFamilyIndexCount, pQueueFamilyIndices, preTransform, compositeAlpha, presentMode, clipped, oldSwapchain
- `VkImageViewCreateInfo` — sType, pNext, flags, image, viewType, format, components, subresourceRange
- `VkComponentMapping` — r, g, b, a (VkComponentSwizzle)
- `VkImageSubresourceRange` — aspectMask, baseMipLevel, levelCount, baseArrayLayer, layerCount
- `VkExtent2D` — width, height
- `VkExtent3D` — width, height, depth
- `VkOffset2D` — x, y
- `VkOffset3D` — x, y, z
- `VkRect2D` — offset, extent
- `VkViewport` — x, y, width, height, minDepth, maxDepth
- `VkShaderModuleCreateInfo` — sType, pNext, flags, codeSize, pCode
- `VkPipelineShaderStageCreateInfo` — sType, pNext, flags, stage, module, pName, pSpecializationInfo
- `VkPipelineVertexInputStateCreateInfo` — sType, pNext, flags, vertexBindingDescriptionCount, pVertexBindingDescriptions, vertexAttributeDescriptionCount, pVertexAttributeDescriptions
- `VkVertexInputBindingDescription` — binding, stride, inputRate
- `VkVertexInputAttributeDescription` — location, binding, format, offset
- `VkPipelineInputAssemblyStateCreateInfo` — sType, pNext, flags, topology, primitiveRestartEnable
- `VkPipelineViewportStateCreateInfo` — sType, pNext, flags, viewportCount, pViewports, scissorCount, pScissors
- `VkPipelineRasterizationStateCreateInfo` — sType, pNext, flags, depthClampEnable, rasterizerDiscardEnable, polygonMode, cullMode, frontFace, depthBiasEnable, depthBiasConstantFactor, depthBiasClamp, depthBiasSlopeFactor, lineWidth
- `VkPipelineMultisampleStateCreateInfo` — sType, pNext, flags, rasterizationSamples, sampleShadingEnable, minSampleShading, pSampleMask, alphaToCoverageEnable, alphaToOneEnable
- `VkPipelineColorBlendAttachmentState` — blendEnable, srcColorBlendFactor, dstColorBlendFactor, colorBlendOp, srcAlphaBlendFactor, dstAlphaBlendFactor, alphaBlendOp, colorWriteMask
- `VkPipelineColorBlendStateCreateInfo` — sType, pNext, flags, logicOpEnable, logicOp, attachmentCount, pAttachments, blendConstants[4]
- `VkPipelineDynamicStateCreateInfo` — sType, pNext, flags, dynamicStateCount, pDynamicStates
- `VkPipelineLayoutCreateInfo` — sType, pNext, flags, setLayoutCount, pSetLayouts, pushConstantRangeCount, pPushConstantRanges
- `VkGraphicsPipelineCreateInfo` — sType, pNext, flags, stageCount, pStages, pVertexInputState, pInputAssemblyState, pViewportState, pRasterizationState, pMultisampleState, pDepthStencilState, pColorBlendState, pDynamicState, layout, renderPass, subpass, basePipelineHandle, basePipelineIndex
- `VkCommandPoolCreateInfo` — sType, pNext, flags, queueFamilyIndex
- `VkCommandBufferAllocateInfo` — sType, pNext, commandPool, level, commandBufferCount
- `VkCommandBufferBeginInfo` — sType, pNext, flags, pInheritanceInfo
- `VkSemaphoreCreateInfo` — sType, pNext, flags
- `VkFenceCreateInfo` — sType, pNext, flags
- `VkBufferCreateInfo` — sType, pNext, flags, size, usage, sharingMode, queueFamilyIndexCount, pQueueFamilyIndices
- `VkMemoryAllocateInfo` — sType, pNext, allocationSize, memoryTypeIndex
- `VkMemoryRequirements` — size, alignment, memoryTypeBits
- `VkPhysicalDeviceMemoryProperties` — memoryTypeCount, memoryTypes[32], memoryHeapCount, memoryHeaps[16]
- `VkMemoryType` — propertyFlags, heapIndex
- `VkMemoryHeap` — size, flags
- `VkQueueFamilyProperties` — queueFlags, queueCount, timestampValidBits, minImageTransferGranularity
- `VkSurfaceCapabilitiesKHR` — minImageCount, maxImageCount, currentExtent, minImageExtent, maxImageExtent, maxImageArrayLayers, supportedTransforms, currentTransform, supportedCompositeAlpha, supportedUsageFlags
- `VkSurfaceFormatKHR` — format, colorSpace
- `VkPhysicalDeviceProperties` — apiVersion, driverVersion, vendorID, deviceID, deviceType, deviceName[256], ...
- `VkSubmitInfo` — sType, pNext, waitSemaphoreCount, pWaitSemaphores, pWaitDstStageMask, commandBufferCount, pCommandBuffers, signalSemaphoreCount, pSignalSemaphores
- `VkSubmitInfo2` — sType, pNext, flags, waitSemaphoreInfoCount, pWaitSemaphoreInfos, commandBufferInfoCount, pCommandBufferInfos, signalSemaphoreInfoCount, pSignalSemaphoreInfos (sync2)
- `VkSemaphoreSubmitInfo` — sType, pNext, semaphore, value, stageMask, deviceIndex (sync2)
- `VkCommandBufferSubmitInfo` — sType, pNext, commandBuffer, deviceMask (sync2)
- `VkPresentInfoKHR` — sType, pNext, waitSemaphoreCount, pWaitSemaphores, swapchainCount, pSwapchains, pImageIndices, pResults
- `VkClearValue` — union: VkClearColorValue color / VkClearDepthStencilValue depthStencil
- `VkClearColorValue` — union: float[4] / int[4] / uint[4]
- `VkClearDepthStencilValue` — depth, stencil
- `VkRenderingAttachmentInfo` — sType, pNext, imageView, imageLayout, resolveMode, resolveImageView, resolveImageLayout, loadOp, storeOp, clearValue
- `VkRenderingInfo` — sType, pNext, flags, renderArea, layerCount, viewMask, colorAttachmentCount, pColorAttachments, pDepthAttachment, pStencilAttachment
- `VkImageMemoryBarrier2` — sType, pNext, srcStageMask, srcAccessMask, dstStageMask, dstAccessMask, oldLayout, newLayout, srcQueueFamilyIndex, dstQueueFamilyIndex, image, subresourceRange
- `VkBufferMemoryBarrier2` — sType, pNext, srcStageMask, srcAccessMask, dstStageMask, dstAccessMask, srcQueueFamilyIndex, dstQueueFamilyIndex, buffer, offset, size
- `VkDependencyInfo` — sType, pNext, dependencyFlags, memoryBarrierCount, pMemoryBarriers, bufferMemoryBarrierCount, pBufferMemoryBarriers, imageMemoryBarrierCount, pImageMemoryBarriers
- `VkBufferCopy` — srcOffset, dstOffset, size
- `VkDebugUtilsMessengerCallbackDataEXT` — sType, pNext, messageId, pMessageIdName, messageSeverity, messageType, pMessage, queueLabelCount, pQueueLabels, cmdBufLabelCount, pCmdBufLabels, objectCount, pObjects
- `VkDebugUtilsObjectNameInfoEXT` — sType, pNext, objectType, objectHandle, pObjectName

#### 1.5 `Vk.cs`
Function delegate types + loaded function pointers:

**Instance-level functions** (loaded via `vkGetInstanceProcAddr`):
- `vkCreateInstance`, `vkDestroyInstance`
- `vkEnumeratePhysicalDevices`, `vkGetPhysicalDeviceProperties`, `vkGetPhysicalDeviceMemoryProperties`
- `vkGetPhysicalDeviceQueueFamilyProperties`
- `vkGetPhysicalDeviceSurfaceSupportKHR`
- `vkGetPhysicalDeviceSurfaceCapabilitiesKHR`, `vkGetPhysicalDeviceSurfaceFormatsKHR`, `vkGetPhysicalDeviceSurfacePresentModesKHR`
- `vkCreateDevice`, `vkDestroyDevice`
- `vkDestroySurfaceKHR`
- `vkCreateDebugUtilsMessengerEXT`, `vkDestroyDebugUtilsMessengerEXT` (extension — via getInstanceProcAddr)
- `vkGetDeviceProcAddr`

**Device-level functions** (loaded via `vkGetDeviceProcAddr` for best performance):
- `vkGetDeviceQueue`
- `vkCreateSwapchainKHR`, `vkDestroySwapchainKHR`, `vkGetSwapchainImagesKHR`
- `vkCreateImageView`, `vkDestroyImageView`
- `vkCreateShaderModule`, `vkDestroyShaderModule`
- `vkCreatePipelineLayout`, `vkDestroyPipelineLayout`
- `vkCreateGraphicsPipelines`, `vkDestroyPipeline`
- `vkCreateCommandPool`, `vkDestroyCommandPool`
- `vkAllocateCommandBuffers`, `vkFreeCommandBuffers`
- `vkBeginCommandBuffer`, `vkEndCommandBuffer`, `vkResetCommandBuffer`
- `vkCreateSemaphore`, `vkDestroySemaphore`
- `vkCreateFence`, `vkDestroyFence`, `vkResetFences`, `vkWaitForFences`, `vkGetFenceStatus`
- `vkCreateBuffer`, `vkDestroyBuffer`
- `vkAllocateMemory`, `vkFreeMemory`
- `vkBindBufferMemory`
- `vkGetBufferMemoryRequirements`
- `vkMapMemory`, `vkUnmapMemory`
- `vkCmdBindPipeline`
- `vkCmdSetViewport`, `vkCmdSetScissor`
- `vkCmdBindVertexBuffers`
- `vkCmdDraw`
- `vkCmdBeginRendering`, `vkCmdEndRendering` (Vulkan 1.3 dynamic rendering)
- `vkCmdPipelineBarrier2` (sync2)
- `vkCmdCopyBuffer`
- `vkCmdBindIndexBuffer`, `vkCmdDrawIndexed` (for future)
- `vkAcquireNextImageKHR`
- `vkQueueSubmit2` (sync2)
- `vkQueuePresentKHR`
- `vkDeviceWaitIdle`
- `vkQueueWaitIdle`

### Phase 2: Vulkan Context

#### 2.1 `VulkanContext.cs`
- **CreateInstance:**
  - `VkApplicationInfo` with `apiVersion = VK_API_VERSION_1_3`
  - Instance extensions from SDL3: `SDL_GetVulkanInstanceExtensions()`
  - Add `VK_EXT_debug_utils` in Debug
  - Layers: `VK_LAYER_KHRONOS_validation` in Debug
  - Chain `VkDebugUtilsMessengerCreateInfoEXT` in `pNext` for early validation
  - Debug callback: prints `pMessage` to stderr/console

- **PickPhysicalDevice:**
  - Enumerate all physical devices
  - Prefer `VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU`
  - Find queue family with `VK_QUEUE_GRAPHICS_BIT` + surface support (`vkGetPhysicalDeviceSurfaceSupportKHR`)

- **CreateLogicalDevice:**
  - Enable `VK_KHR_swapchain` device extension
  - Chain `VkPhysicalDeviceDynamicRenderingFeatures` in `pNext` with `dynamicRendering = VK_TRUE`
  - Single queue from selected family, priority 1.0

- **CreateSurface:**
  - Call SDL3 `SDL_Vulkan_CreateSurface(window, instance, ...)` via Sdl3Window
  - Store `VkSurfaceKHR`

- **Debug Messenger:**
  - `vkCreateDebugUtilsMessengerEXT` with callback
  - Severity: Verbose | Warning | Error
  - Type: General | Validation | Performance

### Phase 3: Swapchain

#### 3.1 `VulkanSwapchain.cs`
- **Query surface:**
  - `vkGetPhysicalDeviceSurfaceCapabilitiesKHR` → min/max image count, current extent
  - `vkGetPhysicalDeviceSurfaceFormatsKHR` → prefer `B8G8R8A8_UNORM` + `SrgbNonlinear`, fallback first format
  - `vkGetPhysicalDeviceSurfacePresentModesKHR` → prefer `MAILBOX`, fallback `FIFO` (guaranteed)

- **Create swapchain:**
  - `minImageCount = max(minImageCount + 1, maxImageCount)` (clamped)
  - `imageUsage = COLOR_ATTACHMENT_BIT | TRANSFER_DST_BIT` (for future screenshots)
  - `preTransform = currentTransform` (no pre-rotation on desktop)
  - `compositeAlpha = OPAQUE_BIT`
  - `clipped = VK_TRUE`
  - `oldSwapchain = VK_NULL_HANDLE` (on first create)

- **Get swapchain images:**
  - `vkGetSwapchainImagesKHR` → array of `VkImage`
  - Create `VkImageView` for each (`TYPE_2D`, same format, `COLOR_BIT` aspect)

- **Recreate:**
  - `vkDeviceWaitIdle`
  - Destroy old image views + swapchain
  - Create new swapchain with `oldSwapchain` = old handle
  - Create new image views

### Phase 4: Pipeline

#### 4.1 `VulkanPipeline.cs`
- **Shader modules:**
  - Load `triangle.vert.spv` and `triangle.frag.spv` from embedded resources or filesystem
  - `vkCreateShaderModule` for each

- **Vertex input:**
  - Binding 0: stride = sizeof(Vertex) = 36 bytes, `VERTEX_INPUT_RATE_VERTEX`
  - Attribute 0: `R32G32B32_SFLOAT` @ offset 0 (Position, location 0)
  - Attribute 1: `R32G32B32_SFLOAT` @ offset 12 (Color, location 1)
  - Attribute 2: `R32G32B32_SFLOAT` @ offset 24 (Normal, location 2)

- **Pipeline state:**
  - Input assembly: `TRIANGLE_LIST`
  - Viewport state: viewportCount=1, scissorCount=1 (dynamic values)
  - Rasterization: `FILL`, cull `NONE`, `COUNTER_CLOCKWISE`, lineWidth=1.0
  - Multisample: `COUNT_1_BIT`, no sample shading
  - Color blend: 1 attachment, blend disabled, write RGBA
  - Dynamic state: `VIEWPORT`, `SCISSOR`
  - Pipeline layout: no descriptor sets, no push constants (triangle only)

- **Dynamic rendering integration:**
  - `VkGraphicsPipelineCreateInfo::renderPass = VK_NULL_HANDLE` (Vulkan 1.3 dynamic rendering)
  - Set `pNext` to `VkPipelineRenderingCreateInfo` with `colorAttachmentCount=1`, `pColorAttachmentFormats = {swapchainFormat}`

### Phase 5: Frame Resources

#### 5.1 `VulkanFrameResources.cs`
- **Constants:**
  - `MAX_FRAMES_IN_FLIGHT = 2`

- **Per-frame-in-flight resources** (indexed 0..MAX_FRAMES_IN_FLIGHT-1):
  - `VkCommandBuffer` — primary, from shared command pool
  - `VkFence` — signaled on submit, waited at frame start (created with `SIGNALED` flag)
  - `VkSemaphore` — acquire semaphore (signaled by `vkAcquireNextImageKHR`)

- **Per-swapchain-image resources** (indexed 0..swapchainImageCount-1):
  - `VkSemaphore` — submit/render-finished semaphore (signaled by `vkQueueSubmit2`, waited by `vkQueuePresentKHR`)
  - **CRITICAL:** These are indexed by swapchain image index, NOT frame-in-flight index.
    This is the correct pattern from the Vulkan Guide (§swapchain_semaphore_reuse).
    Waiting on the acquire semaphore/fence for a given image index guarantees the previous
    present operation using that image has completed, making the submit semaphore safe to reuse.

- **Command pool:**
  - `vkCreateCommandPool` with `RESET_COMMAND_BUFFER_BIT` flag
  - Allocate `MAX_FRAMES_IN_FLIGHT` primary command buffers

### Phase 6: Vertex Buffer

#### 6.1 `VulkanVertexBuffer.cs`
- **Staging buffer pattern (correct from start):**
  1. Create staging buffer: `usage = TRANSFER_SRC_BIT`, memory = `HOST_VISIBLE | HOST_COHERENT`
  2. `vkMapMemory` → `memcpy` vertex data → `vkUnmapMemory`
  3. Create vertex buffer: `usage = TRANSFER_DST_BIT | VERTEX_BUFFER_BIT`, memory = `DEVICE_LOCAL`
  4. Allocate + record one-time command buffer
  5. `vkCmdCopyBuffer(staging, vertex, size)`
  6. Submit + wait on fence
  7. Destroy staging buffer + free its memory + free one-time command buffer

- **Triangle data:**
  ```
  Vertex[3] = {
    { Position: ( 0.0, -0.5, 0.0), Color: (1, 0, 0), Normal: (0, 0, 1) },
    { Position: ( 0.5,  0.5, 0.0), Color: (0, 1, 0), Normal: (0, 0, 1) },
    { Position: (-0.5,  0.5, 0.0), Color: (0, 0, 1), Normal: (0, 0, 1) },
  }
  ```

- **Memory type selection:**
  - `vkGetPhysicalDeviceMemoryProperties` → iterate `memoryTypes[]`
  - Find type where `(memoryTypeBits >> i) & 1` and `propertyFlags` matches desired flags
  - Helper: `FindMemoryType(memoryTypeBits, desiredFlags)`

### Phase 7: Renderer

#### 7.1 `VulkanRenderer.cs` (implements `IRenderer`)
- **Constructor:**
  - Create swapchain, pipeline, frame resources, vertex buffer
  - Store reference to `VulkanContext` (instance, device, queue, surface)

- **Frame loop (`Render()` method):**
  ```
  1. vkWaitForFences(frameFences[frameIndex])
  2. vkResetFences(frameFences[frameIndex])
  3. vkAcquireNextImageKHR(swapchain, acquireSemaphores[frameIndex], imageIndex)
  4. vkResetCommandBuffer(commandBuffers[frameIndex])
  5. vkBeginCommandBuffer(commandBuffers[frameIndex], ONE_TIME_SUBMIT)
  6. Image layout transition (sync2 barrier):
     UNDEFINED → COLOR_ATTACHMENT_OPTIMAL
     (srcStageMask: NONE, dstStageMask: COLOR_ATTACHMENT_OUTPUT)
  7. vkCmdBeginRendering(renderingInfo):
     - colorAttachment: swapchainImageViews[imageIndex], COLOR_ATTACHMENT_OPTIMAL
     - loadOp: CLEAR (black), storeOp: STORE
     - renderArea: full extent
  8. vkCmdBindPipeline(GRAPHICS, pipeline)
  9. vkCmdSetViewport(0, 1, {0, 0, extent.width, extent.height, 0, 1})
  10. vkCmdSetScissor(0, 1, {{0,0}, extent})
  11. vkCmdBindVertexBuffers(0, 1, {vertexBuffer}, {0})
  12. vkCmdDraw(3, 1, 0, 0)
  13. vkCmdEndRendering()
  14. Image layout transition (sync2 barrier):
      COLOR_ATTACHMENT_OPTIMAL → PRESENT_SRC_KHR
      (srcStageMask: COLOR_ATTACHMENT_OUTPUT, dstStageMask: ALL_GRAPHICS)
  15. vkEndCommandBuffer()
  16. vkQueueSubmit2(queue, submitInfo2):
      - wait: acquireSemaphores[frameIndex] @ COLOR_ATTACHMENT_OUTPUT
      - commandBuffer: commandBuffers[frameIndex]
      - signal: submitSemaphores[imageIndex]
      - fence: frameFences[frameIndex]
  17. vkQueuePresentKHR(presentInfo):
      - wait: submitSemaphores[imageIndex]
      - swapchain, imageIndex
  18. frameIndex = (frameIndex + 1) % MAX_FRAMES_IN_FLIGHT
  ```

- **Resize handling:**
  - If `vkAcquireNextImageKHR` returns `ERROR_OUT_OF_DATE_KHR` or `SuboptimalKHR`:
    - `vkDeviceWaitIdle`
    - Recreate swapchain
    - Continue frame

- **Dispose:**
  - `vkDeviceWaitIdle`
  - Destroy vertex buffer + memory
  - Destroy semaphores (acquire + submit), fences
  - Destroy command pool
  - Destroy pipeline, pipeline layout, shader modules
  - Destroy swapchain + image views
  - Destroy debug messenger
  - Destroy device, surface, instance

#### 7.2 `VulkanRenderContext.cs` (implements `IRenderContext`)
- Exposes `Window` (from Sdl3Window)
- `CreateRenderer()` → returns `VulkanRenderer`
- `Resize()` → triggers swapchain recreation
- `Dispose()` → destroys context

#### 7.3 `VulkanBackendRegistrar.cs`
- Static constructor registers `"vulkan"` in `RenderBackendFactory`
- Factory creates `VulkanRenderContext` with `Sdl3Window`

### Phase 8: Shaders

#### 8.1 `Shaders/triangle.vert`
```glsl
#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(location = 0) out vec3 fragColor;

void main() {
    gl_Position = vec4(inPosition, 1.0);
    fragColor = inColor;
}
```

#### 8.2 `Shaders/triangle.frag`
```glsl
#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fragColor, 1.0);
}
```

#### 8.3 Compilation
```bash
glslangValidator -V triangle.vert -o triangle.vert.spv
glslangValidator -V triangle.frag -o triangle.frag.spv
```
- Embed `.spv` files as embedded resources in csproj, or copy to output directory
- Load at runtime via `Assembly.GetManifestResourceStream()` or `File.ReadAllBytes()`

### Phase 9: App Integration

- Fix `CortexEngine.App.csproj`:
  - Remove deleted project references
  - Add `Engine.Graphics` + `Engine.Graphics.Vulkan`
- Fix `Program.cs`:
  - Simplify to triangle-only rendering
  - `RenderBackendFactory.Create("vulkan", 1280, 720, validation: true)`
  - Main loop: poll events → render → present
  - Keep: Sdl3Window, basic event handling
  - Remove: ECS scene, physics, AI, camera tour (add back later)

### Phase 10: Fix Tests

- Update `Engine.Tests.csproj` — reference restored `Engine.Graphics`
- Tests referencing Engine.Graphics: ObjLoaderTests, RenderBackendFactoryTests,
  SceneSerializerTests, MeshMathAndProceduralTests
- All tests should pass after Engine.Graphics is restored

---

## File Layout

```
src/
├── Engine.Core/              (exists, unchanged)
├── Engine.Graphics/          (exists, restored minimal interfaces)
│   ├── Engine.Graphics.csproj
│   ├── IRenderContext.cs
│   ├── IRenderer.cs
│   ├── IScreenshotProvider.cs
│   ├── RenderBackendFactory.cs
│   ├── MeshMath.cs
│   ├── ProceduralMesh.cs
│   ├── SceneSerializer.cs
│   └── Loaders/
│       └── ObjLoader.cs
├── Engine.Graphics.Vulkan/   (new — pure P/Invoke, Vulkan 1.3)
│   ├── Engine.Graphics.Vulkan.csproj
│   ├── VulkanNative.cs          — library loading, vkGetInstanceProcAddr
│   ├── VulkanHandles.cs         — opaque pointer types
│   ├── VulkanEnums.cs           — all Vulkan enums/flags
│   ├── VulkanStructs.cs         — all Vulkan structs (LayoutKind.Sequential)
│   ├── Vk.cs                    — function delegates + loaded pointers
│   ├── VulkanContext.cs         — instance, device, queue, surface, debug
│   ├── VulkanSwapchain.cs       — swapchain, image views, recreate
│   ├── VulkanPipeline.cs        — shader modules, pipeline layout, graphics pipeline
│   ├── VulkanFrameResources.cs  — command buffers, fences, semaphores (correct indexing)
│   ├── VulkanVertexBuffer.cs    — staging buffer → device-local vertex buffer
│   ├── VulkanRenderer.cs        — IRenderer: frame loop with dynamic rendering
│   ├── VulkanRenderContext.cs   — IRenderContext implementation
│   ├── VulkanBackendRegistrar.cs— registration in RenderBackendFactory
│   └── Shaders/
│       ├── triangle.vert
│       ├── triangle.frag
│       ├── triangle.vert.spv
│       └── triangle.frag.spv
├── Engine.Physics/           (exists, unchanged)
├── Engine.AI/                (exists, unchanged)
└── CortexEngine.App/         (fix references, simplify to triangle)
```

---

## csproj: Engine.Graphics.Vulkan

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Engine.Core\Engine.Core.csproj" />
    <ProjectReference Include="..\Engine.Graphics\Engine.Graphics.csproj" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="Shaders\*.spv" />
  </ItemGroup>
</Project>
```

No NuGet packages for Vulkan. Pure P/Invoke.

---

## Cross-Platform Notes

- **Library name:** `vulkan-1.dll` (Windows) vs `libvulkan.so.1` (Linux) — handled in `VulkanNative.cs`
- **Surface creation:** SDL3 abstracts platform differences (`SDL_Vulkan_CreateSurface`)
- **SPIR-V:** Binary format, identical on all platforms
- **.NET 9:** `NativeLibrary.Load()` for dynamic resolution

---

## Key Technical Details

### Semaphore Indexing (CRITICAL)

```
                    Indexed by frame-in-flight (0..1)        Indexed by swapchain image (0..N-1)
                    ─────────────────────────────────       ──────────────────────────────────
Acquire semaphore   ✓
Command buffer      ✓
Frame fence         ✓
Submit semaphore                                            ✓
```

Rationale: `vkQueuePresentKHR` cannot signal a fence/semaphore. The only way to know
a submit semaphore is safe to reuse is to acquire the same swapchain image index again
(which guarantees the previous present using that image has completed).
Indexing submit semaphores by frame-in-flight is a common bug that violates the spec.

### Dynamic Rendering (Vulkan 1.3)

No `VkRenderPass` or `VkFramebuffer` objects needed:
```csharp
// Instead of vkCmdBeginRenderPass:
VkRenderingAttachmentInfo colorAttachment = new() {
    sType = VK_STRUCTURE_TYPE_RENDERING_ATTACHMENT_INFO,
    imageView = swapchainImageViews[imageIndex],
    imageLayout = COLOR_ATTACHMENT_OPTIMAL,
    loadOp = CLEAR,
    storeOp = STORE,
    clearValue = new() { color = { 0, 0, 0, 1 } }
};

VkRenderingInfo renderingInfo = new() {
    sType = VK_STRUCTURE_TYPE_RENDERING_INFO,
    renderArea = { {0,0}, extent },
    layerCount = 1,
    colorAttachmentCount = 1,
    pColorAttachments = &colorAttachment
};

vkCmdBeginRendering(commandBuffer, &renderingInfo);
// draw commands...
vkCmdEndRendering(commandBuffer);
```

Pipeline must include `VkPipelineRenderingCreateInfo` in `pNext`:
```csharp
VkPipelineRenderingCreateInfo renderingInfo = new() {
    sType = VK_STRUCTURE_TYPE_PIPELINE_RENDERING_CREATE_INFO,
    colorAttachmentCount = 1,
    pColorAttachmentFormats = &swapchainFormat
};
// Chain in VkGraphicsPipelineCreateInfo.pNext
```

### Sync2 Image Layout Transitions

Using `vkCmdPipelineBarrier2` with `VkImageMemoryBarrier2`:
```csharp
// UNDEFINED → COLOR_ATTACHMENT_OPTIMAL (before rendering)
VkImageMemoryBarrier2 toColor = new() {
    sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER_2,
    srcStageMask = PIPELINE_STAGE_2_NONE,
    srcAccessMask = ACCESS_2_NONE,
    dstStageMask = PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT,
    dstAccessMask = ACCESS_2_COLOR_ATTACHMENT_WRITE,
    oldLayout = UNDEFINED,
    newLayout = COLOR_ATTACHMENT_OPTIMAL,
    image = swapchainImages[imageIndex],
    subresourceRange = { COLOR_BIT, 0, 1, 0, 1 }
};

// COLOR_ATTACHMENT_OPTIMAL → PRESENT_SRC_KHR (after rendering)
VkImageMemoryBarrier2 toPresent = new() {
    sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER_2,
    srcStageMask = PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT,
    srcAccessMask = ACCESS_2_COLOR_ATTACHMENT_WRITE,
    dstStageMask = PIPELINE_STAGE_2_ALL_GRAPHICS,
    dstAccessMask = ACCESS_2_NONE,
    oldLayout = COLOR_ATTACHMENT_OPTIMAL,
    newLayout = PRESENT_SRC_KHR,
    image = swapchainImages[imageIndex],
    subresourceRange = { COLOR_BIT, 0, 1, 0, 1 }
};

VkDependencyInfo depInfo = new() {
    sType = VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
    imageMemoryBarrierCount = 1,
    pImageMemoryBarriers = &barrier
};
vkCmdPipelineBarrier2(commandBuffer, &depInfo);
```

### Queue Submit (Sync2)

Using `vkQueueSubmit2` with `VkSubmitInfo2`:
```csharp
VkSemaphoreSubmitInfo waitInfo = new() {
    sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
    semaphore = acquireSemaphores[frameIndex],
    stageMask = PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT
};

VkCommandBufferSubmitInfo cmdInfo = new() {
    sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
    commandBuffer = commandBuffers[frameIndex]
};

VkSemaphoreSubmitInfo signalInfo = new() {
    sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
    semaphore = submitSemaphores[imageIndex],
    stageMask = PIPELINE_STAGE_2_ALL_GRAPHICS
};

VkSubmitInfo2 submitInfo = new() {
    sType = VK_STRUCTURE_TYPE_SUBMIT_INFO_2,
    waitSemaphoreInfoCount = 1,
    pWaitSemaphoreInfos = &waitInfo,
    commandBufferInfoCount = 1,
    pCommandBufferInfos = &cmdInfo,
    signalSemaphoreInfoCount = 1,
    pSignalSemaphoreInfos = &signalInfo
};

vkQueueSubmit2(queue, 1, &submitInfo, frameFences[frameIndex]);
```

### Vertex Layout

```
Vertex struct (9 floats, 36 bytes):
  Position: vec3 (offset 0,  format R32G32B32_SFLOAT, location 0)
  Color:    vec3 (offset 12, format R32G32B32_SFLOAT, location 1)
  Normal:   vec3 (offset 24, format R32G32B32_SFLOAT, location 2)
```

### Validation Layers

```csharp
string[] layers = enableValidation
    ? new[] { "VK_LAYER_KHRONOS_validation" }
    : Array.Empty<string>();

string[] instanceExtensions = enableValidation
    ? [.. sdlExtensions, "VK_EXT_debug_utils"]
    : sdlExtensions;
```

Debug callback (C#):
```csharp
static uint DebugCallback(
    nint instance, uint messageSeverity, uint messageTypes,
    nint pCallbackData, nint pUserData)
{
    var data = Marshal.PtrToStructure<VkDebugUtilsMessengerCallbackDataEXT>(pCallbackData);
    Console.Error.WriteLine($"[Vulkan] {data.pMessage}");
    return 0; // VK_FALSE — don't abort
}
```

---

## Future Phases (Not in This Plan)

- **Phase 11:** ImGui integration (ImGui.NET + Vulkan backend)
- **Phase 12:** Mesh rendering (OBJ loading, index buffers, descriptor sets, UBO for camera)
- **Phase 13:** PBR shading (Fresnel, ACES tonemap, gamma correction, directional + point lights)
- **Phase 14:** Shadow mapping (depth-only render pass from light POV, PCF sampling)
- **Phase 15:** Screenshot capture (copy swapchain image to staging buffer → PNG)
- **Phase 16:** VMA (Vulkan Memory Allocator) for sub-allocation
- **Phase 17:** Multi-threaded command buffer recording
