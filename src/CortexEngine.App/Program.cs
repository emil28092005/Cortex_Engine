using System.Diagnostics;
using System.Numerics;
using Engine.AI;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;
using Engine.Graphics.Vulkan;
using Engine.Physics;
using Flecs.NET.Core;
using ImGuiNET;
#if !RELEASE_AOT
using Engine.AI.Mcp;
using Microsoft.AspNetCore.Builder;
#endif

namespace CortexEngine.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Cortex Engine — Vulkan + Physics + AI/MCP (pure P/Invoke)...");

        try
        {
            var mcpPort = ParseMcpPort(args);

            using var world = World.Create();
            using var physicsWorld = new PhysicsWorld();

            VulkanBackendRegistrar.EnsureRegistered();
            using var renderContext = RenderBackendFactory.Create("vulkan", 1280, 720, enableValidation: true);
            var window = renderContext.Window;
            var input = window.Input;
            using var renderer = renderContext.CreateRenderer();

            var processor = new AiCommandProcessor(world, p => LoadMesh(p, new Vector3(0.7f, 0.6f, 0.5f)), path => renderer.RequestScreenshot(path));
            var queue = new AiCommandQueue(processor, new DummyScreenshotProvider());

#if !RELEASE_AOT
            WebApplication? mcpApp = null;
            var mcpStarted = false;
            var frameCount = 0;
#endif

            var cameraEntity = world.Entity("Camera")
                .Set(new Camera(
                    new Vector3(0, 5, -15),
                    new Vector3(0, 0.5f, 0),
                    Vector3.UnitY,
                    MathF.PI / 4f,
                    1280f / 720f,
                    0.1f,
                    100f));

            var cameraController = new FreeFlyCameraController(cameraEntity);
            Console.WriteLine("Camera: FreeFly (WASD + right-click mouse look, Q/E up/down, Shift boost)");

            CreateScene(world);

            var hasImGui = renderer.GetType().Name == "VulkanRenderer";
            VulkanRenderer? vkRenderer = hasImGui ? (VulkanRenderer)renderer : null;
            if (hasImGui)
            {
                ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(window.Width, window.Height);
                Console.WriteLine("[App] ImGui initialized.");
            }

            var lastWidth = window.Width;
            var lastHeight = window.Height;
            var frames = 0;
            var lastFpsTime = 0.0;
            var timing = new Timing();
            var totalTime = 0.0f;

            bool isRecording = false;
            Process? ffmpegProcess = null;
            string? recordingPath = null;
            int recordedFrames = 0;

            const double renderFps = 60.0;
            var renderFrameTime = 1.0 / renderFps;
            var renderTimer = 0.0;

            while (!window.ShouldClose)
            {
                timing.Tick();
                renderTimer += timing.DeltaTime;

                // Physics + input run at full speed (uncapped)
                window.PumpEvents();
                input.BeginFrame();

#if !RELEASE_AOT
                frameCount++;
                if (mcpPort > 0 && !mcpStarted && frameCount > 10)
                {
                    mcpStarted = true;
                    var port = mcpPort;
                    var q = queue;
                    Task.Run(() =>
                    {
                        try
                        {
                            mcpApp = McpEngineServerHost.Create(Array.Empty<string>(), q, port: port);
                            Console.WriteLine($"[App] MCP HTTP server listening on http://localhost:{port}/ (SSE)");
                            mcpApp.Run();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[App] MCP server error: {ex.Message}");
                        }
                    });
                }
#endif

                if (window.Width != lastWidth || window.Height != lastHeight)
                {
                    lastWidth = window.Width;
                    lastHeight = window.Height;
                    renderContext.Resize(lastWidth, lastHeight);
                    ref var cam = ref cameraEntity.Ensure<Camera>();
                    cam.AspectRatio = (float)lastWidth / lastHeight;
                    cameraEntity.Set(cam);
                }

                cameraController.Update(input, (float)timing.DeltaTime);
                totalTime += (float)timing.DeltaTime;

                var toInit = new List<(Entity, RigidBody, Transform)>();
                world.Each((Entity e, ref RigidBody rb, ref Transform t) =>
                {
                    if (!rb.IsInitialized)
                        toInit.Add((e, rb, t));
                });

                foreach (var (e, rb, t) in toInit)
                {
                    var rbInit = rb;
                    rbInit.IsInitialized = true;
                    e.Set(rbInit);
                    physicsWorld.CreateBody(e, rbInit, t);
                }

                physicsWorld.Update((float)timing.DeltaTime);
                physicsWorld.SyncTransforms(world);

                // Move lights in chaotic circles
                var mainLight = world.Lookup("MainLight");
                if ((ulong)mainLight.Id != 0)
                {
                    var t = totalTime;
                    var lx = MathF.Sin(t * 0.7f) * 8f + MathF.Cos(t * 0.3f) * 3f;
                    var ly = 12f + MathF.Sin(t * 0.5f) * 5f;
                    var lz = MathF.Cos(t * 0.6f) * 8f + MathF.Sin(t * 0.4f) * 3f;
                    var lt = mainLight.Get<Transform>();
                    lt.Position = new Vector3(lx, ly, lz);
                    mainLight.Set(lt);
                }

                var secondLight = world.Lookup("SecondLight");
                if ((ulong)secondLight.Id != 0)
                {
                    var t = totalTime + MathF.PI;
                    var lx = MathF.Cos(t * 0.5f) * 10f + MathF.Sin(t * 0.2f) * 4f;
                    var ly = 8f + MathF.Cos(t * 0.4f) * 4f;
                    var lz = MathF.Sin(t * 0.6f) * 10f + MathF.Cos(t * 0.3f) * 3f;
                    var lt = secondLight.Get<Transform>();
                    lt.Position = new Vector3(lx, ly, lz);
                    secondLight.Set(lt);
                }

                var thirdLight = world.Lookup("ThirdLight");
                if ((ulong)thirdLight.Id != 0)
                {
                    var t = totalTime + MathF.PI * 1.5f;
                    var lx = MathF.Sin(t * 0.4f) * 12f;
                    var ly = 10f + MathF.Sin(t * 0.3f) * 3f;
                    var lz = MathF.Cos(t * 0.5f) * 12f;
                    var lt = thirdLight.Get<Transform>();
                    lt.Position = new Vector3(lx, ly, lz);
                    thirdLight.Set(lt);
                }

                var processed = queue.ProcessPending();
                if (processed > 0)
                    Console.WriteLine($"[App] Processed {processed} AI command(s)");

                if (hasImGui)
                {
                    var io = ImGui.GetIO();
                    io.DisplaySize = new System.Numerics.Vector2(window.Width, window.Height);
                    io.MousePos = new System.Numerics.Vector2(input.MouseX, input.MouseY);
                    io.MouseDown[0] = input.MouseLeft;
                    io.MouseDown[1] = input.MouseRight;
                    io.MouseDown[2] = input.MouseMiddle;

                    renderer.BeginImGuiFrame();

                    ImGui.Begin("Cortex Engine Debug");
                    ImGui.Text($"FPS: {frames}");
                    ImGui.Text($"Delta: {timing.DeltaTime * 1000.0:F2} ms");

                    var cam = cameraEntity.Get<Camera>();
                    ImGui.Separator();
                    ImGui.Text($"Camera Pos: ({cam.Position.X:F2}, {cam.Position.Y:F2}, {cam.Position.Z:F2})");
                    ImGui.Text($"Camera Target: ({cam.Target.X:F2}, {cam.Target.Y:F2}, {cam.Target.Z:F2})");
                    ImGui.Separator();

                    var entityCount = 0;
                    world.Each((Entity e, ref Transform _) => entityCount++);
                    ImGui.Text($"Entities: {entityCount}");
                    ImGui.Text($"MCP: {(mcpPort > 0 ? $"port {mcpPort}" : "disabled")}");
                    ImGui.Text("WASD: move | RMB: look | Q/E: up/down | Shift: boost");
                    ImGui.End();

                    // Shadow parameters panel
                    ImGui.Begin("Shadow & Light Parameters");

                    // Main light controls
                    var mainLight2 = world.Lookup("MainLight");
                    if ((ulong)mainLight2.Id != 0)
                    {
                        var lc1 = mainLight2.Get<Light>();
                        var i1 = lc1.Intensity; var r1 = lc1.Range;
                        var cr1 = lc1.Color.X; var cg1 = lc1.Color.Y; var cb1 = lc1.Color.Z;
                        ImGui.Text("Main Light (warm)");
                        ImGui.SliderFloat("1 Intensity", ref i1, 0.0f, 50.0f, "%.1f");
                        ImGui.SliderFloat("1 Range", ref r1, 5.0f, 100.0f, "%.1f");
                        ImGui.SliderFloat("1 R", ref cr1, 0.0f, 1.0f, "%.2f");
                        ImGui.SliderFloat("1 G", ref cg1, 0.0f, 1.0f, "%.2f");
                        ImGui.SliderFloat("1 B", ref cb1, 0.0f, 1.0f, "%.2f");
                        lc1.Intensity = i1; lc1.Range = r1;
                        lc1.Color = new Vector3(cr1, cg1, cb1);
                        mainLight2.Set(lc1);
                    }

                    ImGui.Separator();

                    // Second light controls
                    var secondLight2 = world.Lookup("SecondLight");
                    if ((ulong)secondLight2.Id != 0)
                    {
                        var lc2 = secondLight2.Get<Light>();
                        var i2 = lc2.Intensity; var r2 = lc2.Range;
                        var cr2 = lc2.Color.X; var cg2 = lc2.Color.Y; var cb2 = lc2.Color.Z;
                        ImGui.Text("Second Light (cool)");
                        ImGui.SliderFloat("2 Intensity", ref i2, 0.0f, 50.0f, "%.1f");
                        ImGui.SliderFloat("2 Range", ref r2, 5.0f, 100.0f, "%.1f");
                        ImGui.SliderFloat("2 R", ref cr2, 0.0f, 1.0f, "%.2f");
                        ImGui.SliderFloat("2 G", ref cg2, 0.0f, 1.0f, "%.2f");
                        ImGui.SliderFloat("2 B", ref cb2, 0.0f, 1.0f, "%.2f");
                        lc2.Intensity = i2; lc2.Range = r2;
                        lc2.Color = new Vector3(cr2, cg2, cb2);
                        secondLight2.Set(lc2);
                    }

                    ImGui.Separator();

                    // Third light controls
                    var thirdLight2 = world.Lookup("ThirdLight");
                    if ((ulong)thirdLight2.Id != 0)
                    {
                        var lc3 = thirdLight2.Get<Light>();
                        var i3 = lc3.Intensity; var r3 = lc3.Range;
                        var cr3 = lc3.Color.X; var cg3 = lc3.Color.Y; var cb3 = lc3.Color.Z;
                        ImGui.Text("Third Light (red)");
                        ImGui.SliderFloat("3 Intensity", ref i3, 0.0f, 50.0f, "%.1f");
                        ImGui.SliderFloat("3 Range", ref r3, 5.0f, 100.0f, "%.1f");
                        ImGui.SliderFloat("3 R", ref cr3, 0.0f, 1.0f, "%.2f");
                        ImGui.SliderFloat("3 G", ref cg3, 0.0f, 1.0f, "%.2f");
                        ImGui.SliderFloat("3 B", ref cb3, 0.0f, 1.0f, "%.2f");
                        lc3.Intensity = i3; lc3.Range = r3;
                        lc3.Color = new Vector3(cr3, cg3, cb3);
                        thirdLight2.Set(lc3);
                    }

                    ImGui.Separator();

                    var bias = vkRenderer.ShadowBias;
                    var sampleRadius = vkRenderer.ShadowSampleRadius;
                    var farPlane = vkRenderer.ShadowFarPlane;

                    ImGui.SliderFloat("Shadow Bias", ref bias, 0.0001f, 0.1f, "%.4f");
                    ImGui.SliderFloat("Sample Radius", ref sampleRadius, 0.001f, 0.1f, "%.4f");
                    ImGui.SliderFloat("Far Plane", ref farPlane, 10.0f, 120.0f, "%.1f");

                    vkRenderer.ShadowBias = bias;
                    vkRenderer.ShadowSampleRadius = sampleRadius;
                    vkRenderer.ShadowFarPlane = farPlane;

                    ImGui.Text($"Current: bias={bias:F4} radius={sampleRadius:F4} far={farPlane:F1}");

                    if (ImGui.Button("Copy Parameters to Clipboard"))
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"shadowBias={bias:F4}");
                        sb.AppendLine($"shadowSampleRadius={sampleRadius:F4}");
                        sb.AppendLine($"shadowFarPlane={farPlane:F1}");
                        sb.AppendLine($"shadowMapSize=1024");
                        sb.AppendLine($"numLights=3");

                        for (int li = 1; li <= 3; li++)
                        {
                            var le = world.Lookup(li == 1 ? "MainLight" : li == 2 ? "SecondLight" : "ThirdLight");
                            if ((ulong)le.Id != 0)
                            {
                                var lc = le.Get<Light>();
                                sb.AppendLine($"light{li}Intensity={lc.Intensity:F1}");
                                sb.AppendLine($"light{li}Range={lc.Range:F1}");
                                sb.AppendLine($"light{li}Color=({lc.Color.X:F2},{lc.Color.Y:F2},{lc.Color.Z:F2})");
                            }
                        }

                        var paramsText = sb.ToString();
                        ImGui.SetClipboardText(paramsText);
                        Console.WriteLine("[App] Parameters copied to clipboard:");
                        Console.WriteLine(paramsText);
                    }

                    ImGui.End();

                    // Video recording panel
                    ImGui.Begin("Video Recording");

                    if (!isRecording)
                    {
                        if (ImGui.Button("Start Recording"))
                        {
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            recordingPath = $"Videos/cortex_{timestamp}.mp4";
                            Directory.CreateDirectory("Videos");

                            var w = window.Width;
                            var h = window.Height;
                            var ffmpegArgs = $"-y -f rawvideo -pixel_format bgra -video_size {w}x{h} -framerate 60 -i - -c:v libx264 -preset fast -crf 23 -pix_fmt yuv420p -vf fps=30 {recordingPath}";

                            var psi = new ProcessStartInfo
                            {
                                FileName = "ffmpeg",
                                Arguments = ffmpegArgs,
                                UseShellExecute = false,
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true,
                            };

                            try
                            {
                                ffmpegProcess = Process.Start(psi);
                                isRecording = true;
                                recordedFrames = 0;
                                Console.WriteLine($"[App] Recording started: {recordingPath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[App] Failed to start FFmpeg: {ex.Message}");
                                Console.WriteLine("[App] Install FFmpeg: sudo apt install ffmpeg");
                            }
                        }
                    }
                    else
                    {
                        if (ImGui.Button("Stop Recording"))
                        {
                            isRecording = false;
                            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                            {
                                ffmpegProcess.StandardInput.BaseStream.Flush();
                                ffmpegProcess.StandardInput.BaseStream.Close();
                                ffmpegProcess.WaitForExit(5000);
                                if (!ffmpegProcess.HasExited)
                                    ffmpegProcess.Kill();
                            }
                            ffmpegProcess?.Dispose();
                            ffmpegProcess = null;
                            Console.WriteLine($"[App] Recording stopped: {recordingPath} ({recordedFrames} frames)");
                        }

                        ImGui.SameLine();
                        ImGui.Text($"Recording... {recordedFrames} frames");
                    }

                    ImGui.End();

                    renderer.EndImGuiFrame();
                }

                // Fixed render rate: 60 FPS
                if (renderTimer >= renderFrameTime)
                {
                    renderTimer -= renderFrameTime;
                    if (renderTimer > renderFrameTime) renderTimer = 0; // prevent spiral of death

                    // Set recording flag before render so renderer can capture
                    vkRenderer!.IsRecording = isRecording;

                    renderer.RenderWorld(world);
                    queue.CompletePendingScreenshots();

                    // If recording, get captured frame and write to FFmpeg
                    if (isRecording && ffmpegProcess != null && !ffmpegProcess.HasExited)
                    {
                        var frameData = vkRenderer.CapturedFrame;
                        if (frameData != null && frameData.Length > 0)
                        {
                            try
                            {
                                ffmpegProcess.StandardInput.BaseStream.Write(frameData, 0, frameData.Length);
                                ffmpegProcess.StandardInput.BaseStream.Flush();
                                recordedFrames++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[App] FFmpeg write error: {ex.Message}");
                                isRecording = false;
                            }
                        }
                    }

                    frames++;
                }
                if (timing.TotalTime - lastFpsTime >= 1.0)
                {
                    Console.WriteLine($"FPS: {frames}, Delta: {timing.DeltaTime * 1000.0:F2} ms");
                    frames = 0;
                    lastFpsTime = timing.TotalTime;
                }
            }

            Console.WriteLine("Shutting down...");
#if !RELEASE_AOT
            if (mcpApp != null)
                await mcpApp.StopAsync();
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }

        await Task.CompletedTask;
    }

    static int ParseMcpPort(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mcp-port" && i + 1 < args.Length && int.TryParse(args[i + 1], out var port))
                return port;
        }
        return 0;
    }

    static void CreateScene(World world)
    {
        // Floor
        world.Entity("Floor")
            .Set(new Transform(new Vector3(0, -0.5f, 0), Quaternion.Identity, new Vector3(20, 0.5f, 20)))
            .Set(LoadMesh("Content/cube.obj", new Vector3(0.3f, 0.3f, 0.35f)))
            .Set(RigidBody.StaticBox(new Vector3(20, 0.5f, 20)));

        // Central torus knot (floating, no physics)
        var torusKnot = LoadMesh("Content/torusknot.obj", new Vector3(0.9f, 0.7f, 0.3f));
        world.Entity("TorusKnot")
            .Set(new Transform(new Vector3(0, 8, 0), Quaternion.Identity, new Vector3(1.5f)))
            .Set(torusKnot);

        // Sphere pyramid (7 layers, physics)
        var sphereMesh = LoadMesh("Content/sphere.obj", new Vector3(0.5f, 0.6f, 0.9f));
        for (var layer = 0; layer < 7; layer++)
        {
            var count = 7 - layer;
            var y = layer * 1.2f + 1;
            for (var i = 0; i < count; i++)
            {
                var x = (i - (count - 1) * 0.5f) * 1.2f;
                world.Entity($"Pyramid_{layer}_{i}")
                    .Set(new Transform(new Vector3(x, y, 0), Quaternion.Identity, new Vector3(0.8f)))
                    .Set(sphereMesh)
                    .Set(RigidBody.DynamicSphere(0.4f, mass: 0.5f));
            }
        }

        // Diamond rain (30 octahedrons, physics)
        var diamondMesh = LoadMesh("Content/diamond.obj", new Vector3(0.9f, 0.1f, 0.9f));
        var rand = new Random(123);
        for (var i = 0; i < 30; i++)
        {
            var x = (float)(rand.NextDouble() * 20 - 10);
            var y = (float)(rand.NextDouble() * 15 + 15);
            var z = (float)(rand.NextDouble() * 20 - 10);
            world.Entity($"Diamond{i}")
                .Set(new Transform(new Vector3(x, y, z), Quaternion.Identity, new Vector3(0.8f)))
                .Set(diamondMesh)
                .Set(RigidBody.DynamicBox(new Vector3(0.4f), mass: 0.6f));
        }

        // Floating torus rings (decorative, no physics)
        var torusMesh = LoadMesh("Content/torus.obj", new Vector3(0.2f, 0.8f, 0.3f));
        for (var i = 0; i < 8; i++)
        {
            var angle = i * MathF.PI * 2 / 8;
            var x = MathF.Cos(angle) * 8;
            var z = MathF.Sin(angle) * 8;
            var y = (i % 3) + 3;
            world.Entity($"Torus{i}")
                .Set(new Transform(new Vector3(x, y, z), Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle), new Vector3(2f)))
                .Set(torusMesh);
        }

        // Pyramid decorations (4 corners, static)
        var pyramidMesh = LoadMesh("Content/pyramid.obj", new Vector3(0.8f, 0.5f, 0.2f));
        for (var i = 0; i < 4; i++)
        {
            var angle = i * MathF.PI * 2 / 4;
            var x = MathF.Cos(angle) * 12;
            var z = MathF.Sin(angle) * 12;
            world.Entity($"Pyramid{i}")
                .Set(new Transform(new Vector3(x, 0, z), Quaternion.Identity, new Vector3(2.5f)))
                .Set(pyramidMesh);
        }

        // Cone pillars (4 diagonals, static)
        var coneMesh = LoadMesh("Content/cone.obj", new Vector3(0.2f, 0.3f, 0.8f));
        for (var i = 0; i < 4; i++)
        {
            var angle = i * MathF.PI * 2 / 4 + MathF.PI / 4;
            var x = MathF.Cos(angle) * 10;
            var z = MathF.Sin(angle) * 10;
            world.Entity($"Cone{i}")
                .Set(new Transform(new Vector3(x, 0, z), Quaternion.Identity, new Vector3(2f, 3f, 2f)))
                .Set(coneMesh);
        }

        // Lights (3 dynamic point lights, all cast shadows)
        world.Entity("MainLight")
            .Set(new Transform(new Vector3(0, 20, 0), Quaternion.Identity, Vector3.One))
            .Set(Light.Point(new Vector3(0, 20, 0), new Vector3(1.0f, 0.95f, 0.85f), intensity: 8.0f, range: 15.5f));

        world.Entity("SecondLight")
            .Set(new Transform(new Vector3(-12, 10, 8), Quaternion.Identity, Vector3.One))
            .Set(Light.Point(new Vector3(-12, 10, 8), new Vector3(0.3f, 0.6f, 1.0f), intensity: 8.0f, range: 15.5f));

        world.Entity("ThirdLight")
            .Set(new Transform(new Vector3(10, 8, -10), Quaternion.Identity, Vector3.One))
            .Set(Light.Point(new Vector3(10, 8, -10), new Vector3(0.9f, 0.2f, 0.3f), intensity: 8.0f, range: 15.5f));

        var entityCount = 0;
        world.Each((Entity e, ref Transform _) => entityCount++);
        Console.WriteLine($"[Scene] {entityCount} entities: sphere pyramid + diamond rain + 8 torus rings + 4 pyramids + 4 cones + floor + grid + light");
    }

    static Mesh LoadMesh(string path, Vector3 color)
    {
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, path);
            if (!File.Exists(altPath))
            {
                altPath = Path.Combine(AppContext.BaseDirectory, "Content", Path.GetFileName(path));
                if (!File.Exists(altPath))
                    throw new FileNotFoundException($"Mesh file not found: {path}");
            }
            return ObjLoader.Load(altPath, color);
        }
        return ObjLoader.Load(path, color);
    }

    class DummyScreenshotProvider : Engine.Core.IScreenshotProvider
    {
        public Task<byte[]> CaptureAsync(string outputPath) => Task.FromResult(Array.Empty<byte>());
    }
}
