using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Engine.Core;
using Engine.Core.Components;
using EngineMaterial = Engine.Core.Components.Material;
using EngineMesh = Engine.Core.Components.Mesh;
using EngineTransform = Engine.Core.Components.Transform;
using Flecs.NET.Core;
using Raylib_cs;

namespace Engine.Graphics.RaylibBackend;

/// <summary>
/// Raylib implementation of the ECS world renderer.
/// Renders Mesh + Transform + Material entities with up to four directional lights.
/// </summary>
public sealed class RaylibRenderer : IRenderer
{
    private const int ShadowMapSize = 2048;

    private readonly Shader _shader;
    private readonly Shader _shadowShader;
    private readonly uint _shadowFbo;
    private readonly uint _shadowDepthTex;
    private readonly Dictionary<Entity, Raylib_cs.Model> _modelCache = new();
    private readonly Dictionary<string, Texture2D> _textureCache = new();
    private readonly int _materialColorLoc;
    private readonly int _useTextureLoc;
    private readonly int _roughnessLoc;
    private readonly int _metallicLoc;
    private readonly int _ambientLoc;
    private readonly int _viewPosLoc;
    private readonly int _lightCountLoc;
    private readonly int _lightDirLoc;
    private readonly int _lightIntensityLoc;
    private readonly int _lightColorLoc;
    private readonly int _lightPosLoc;
    private readonly int _lightTypeLoc;
    private readonly int _lightRangeLoc;
    private readonly int _lightViewProjLoc;
    private readonly int _shadowMapLoc;
    private readonly float[] _lightDirs = new float[12];
    private readonly float[] _lightPositions = new float[12];
    private readonly float[] _lightRanges = new float[4];
    private readonly int[] _lightTypes = new int[4];
    private readonly float[] _lightIntensities = new float[4];
    private readonly float[] _lightColors = new float[12];
    private int _lightCount;

    private ScreenshotRequest? _pendingScreenshot;
    private int _frameCount;
    private bool _disposed;

    /// <summary>
    /// ImGui editor layer. Accessible so the app can feed world/timing data.
    /// </summary>
    public ImGuiLayer? ImGuiLayer { get; set; }

    public RaylibRenderer()
    {
        _shader = LoadShader();
        _shadowShader = LoadShadowShader();

        // Create depth-only FBO for shadow mapping (per official raylib example)
        _shadowFbo = Rlgl.LoadFramebuffer();
        _shadowDepthTex = Rlgl.LoadTextureDepth(ShadowMapSize, ShadowMapSize, false);
        Rlgl.FramebufferAttach(_shadowFbo, _shadowDepthTex, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);
        Rlgl.FramebufferComplete(_shadowFbo);

        // Main shader uniform locations
        _materialColorLoc = Raylib.GetShaderLocation(_shader, "materialColor");
        _useTextureLoc = Raylib.GetShaderLocation(_shader, "useTexture");
        _roughnessLoc = Raylib.GetShaderLocation(_shader, "roughness");
        _metallicLoc = Raylib.GetShaderLocation(_shader, "metallic");
        _ambientLoc = Raylib.GetShaderLocation(_shader, "ambientColor");
        _viewPosLoc = Raylib.GetShaderLocation(_shader, "viewPos");
        _lightCountLoc = Raylib.GetShaderLocation(_shader, "lightCount");
        _lightDirLoc = Raylib.GetShaderLocation(_shader, "lightDirs");
        _lightIntensityLoc = Raylib.GetShaderLocation(_shader, "lightIntensities");
        _lightColorLoc = Raylib.GetShaderLocation(_shader, "lightColors");
        _lightPosLoc = Raylib.GetShaderLocation(_shader, "lightPositions");
        _lightTypeLoc = Raylib.GetShaderLocation(_shader, "lightTypes");
        _lightRangeLoc = Raylib.GetShaderLocation(_shader, "lightRanges");
        _lightViewProjLoc = Raylib.GetShaderLocation(_shader, "lightViewProj");
        _shadowMapLoc = Raylib.GetShaderLocation(_shader, "shadowMap");
    }

    public void RequestScreenshot(string outputPath)
    {
        _pendingScreenshot = new ScreenshotRequest(outputPath, null);
    }

    public bool IsScreenshotRequested => _pendingScreenshot != null;

    public IScreenshotProvider ScreenshotProvider => new RaylibScreenshotProvider(this);

    public void RenderWorld(World world)
    {
        var camera = GetCamera(world);
        CollectLights(world);

        var hasDirectionalLight = _lightCount > 0 && _lightTypes[0] == (int)LightType.Directional;
        Matrix4x4 lightViewProj = Matrix4x4.Identity;

        // === PASS 1: Shadow map (render depth from light's POV) ===
        if (hasDirectionalLight)
        {
            var lightDir = new Vector3(_lightDirs[0], _lightDirs[1], _lightDirs[2]);
            var sceneCenter = new Vector3(0, 0.5f, 0);
            var lightPos = sceneCenter - lightDir * 30f;
            var up = MathF.Abs(Vector3.Dot(lightDir, Vector3.UnitY)) > 0.99f
                ? Vector3.UnitZ : Vector3.UnitY;

            var shadowCamera = new Camera3D
            {
                Position = lightPos,
                Target = sceneCenter,
                Up = up,
                FovY = 0,
                Projection = CameraProjection.Orthographic
            };

            // Use BeginTextureMode with our custom depth FBO
            Rlgl.EnableFramebuffer(_shadowFbo);
            Rlgl.Viewport(0, 0, ShadowMapSize, ShadowMapSize);
            Rlgl.ClearColor(255, 255, 255, 255);
            Rlgl.ClearScreenBuffers();

            Raylib.BeginMode3D(shadowCamera);

            // Grab light view/proj matrices AFTER BeginMode3D (like official example)
            // Raylib matrices are row-major, need to transpose for OpenGL (column-major)
            var lv = Rlgl.GetMatrixModelview();
            var lp = Rlgl.GetMatrixProjection();
            lightViewProj = Matrix4x4.Transpose(lv * lp);

            // Draw all shadow casters with the simple shadow shader
            world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
            {
                if (e.Name() == "Grid" || e.Name() == "Floor")
                    return;

                var model = GetOrUploadModel(e, mesh);
                var modelMatrix = transform.GetMatrix();

                if (Matrix4x4.Decompose(modelMatrix, out var scale, out var rotation, out var position))
                {
                    var (axis, angle) = QuaternionToAxisAngle(rotation);
                    unsafe
                    {
                        var origShader = model.Materials[0].Shader;
                        model.Materials[0].Shader = _shadowShader;
                        Raylib.DrawModelEx(model, position, axis, angle * 180.0f / MathF.PI, scale, Color.White);
                        model.Materials[0].Shader = origShader;
                    }
                }
            });

            Raylib.EndMode3D();
            Rlgl.DisableFramebuffer();
            Rlgl.Viewport(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }

        // === PASS 2: Main render with shadow sampling ===
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(25, 30, 40, 255));
        Raylib.BeginMode3D(ToRaylib(camera));

        CollectLights(world);
        SetFrameLights();
        Raylib.SetShaderValue(_shader, _viewPosLoc, new float[] { camera.Position.X, camera.Position.Y, camera.Position.Z }, ShaderUniformDataType.Vec3);

        // Set shadow uniforms on the main shader
        if (hasDirectionalLight && _lightViewProjLoc >= 0)
        {
            Raylib.SetShaderValueMatrix(_shader, _lightViewProjLoc, lightViewProj);
        }

        Rlgl.DisableBackfaceCulling();

        var shadowTex = new Texture2D { Id = _shadowDepthTex, Width = ShadowMapSize, Height = ShadowMapSize, Mipmaps = 1, Format = PixelFormat.UncompressedR16 };

        world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
        {
            if (e.Name() == "Grid")
                return;

            var material = e.Has<EngineMaterial>() ? e.Get<EngineMaterial>() : EngineMaterial.Default;
            var model = GetOrUploadModel(e, mesh);
            var modelMatrix = transform.GetMatrix();

            if (Matrix4x4.Decompose(modelMatrix, out var scale, out var rotation, out var position))
            {
                var (axis, angle) = QuaternionToAxisAngle(rotation);
                SetMaterialUniforms(material, model);

                // Bind shadow map on Emission slot (texture unit 1) for each entity
                if (hasDirectionalLight && _shadowMapLoc >= 0)
                {
                    unsafe { Raylib.SetMaterialTexture(ref model.Materials[0], MaterialMapIndex.Emission, shadowTex); }
                }

                Raylib.DrawModelEx(model, position, axis, angle * 180.0f / MathF.PI, scale, Color.White);

                // Re-bind shadow map on unit 1 after DrawModelEx (it may have reset texture state)
                if (hasDirectionalLight && _shadowMapLoc >= 0)
                {
                    Rlgl.ActiveTextureSlot(1);
                    Rlgl.EnableTexture(_shadowDepthTex);
                    Raylib.SetShaderValue(_shader, _shadowMapLoc, 1, ShaderUniformDataType.Int);
                    Rlgl.ActiveTextureSlot(0);
                }
            }
        });

        // Reset texture unit 0 after shadow binding
        Rlgl.ActiveTextureSlot(0);

        Rlgl.EnableBackfaceCulling();

        Raylib.DrawGrid(20, 1.0f);

        Raylib.EndMode3D();


        // ImGui renders on top of the 3D scene, before EndDrawing.
        if (ImGuiLayer != null)
        {
            ImGuiLayer.Begin();
            ImGuiLayer.RenderImGuiUI();
            ImGuiLayer.End();
        }

        Raylib.EndDrawing();

        // Defer the first screenshot by a few frames. Raylib may return a blank image
        // if the window/GPU has not finished presenting the first frame.
        if (_pendingScreenshot is { } request && _frameCount >= 10)
        {
            CaptureScreenshot(request);
            _pendingScreenshot = null;
        }

        _frameCount++;
    }

    private static (Vector3 axis, float angle) QuaternionToAxisAngle(Quaternion q)
    {
        if (MathF.Abs(q.W) > 0.9999999f)
            return (Vector3.UnitY, 0.0f);

        var angle = 2.0f * MathF.Acos(Math.Clamp(q.W, -1.0f, 1.0f));
        var s = MathF.Sqrt(1.0f - q.W * q.W);
        var axis = s > 0.0001f
            ? new Vector3(q.X / s, q.Y / s, q.Z / s)
            : new Vector3(q.X, q.Y, q.Z);
        return (axis, angle);
    }

    private Camera3D ToRaylib(Camera camera)
    {
        return new Camera3D
        {
            Position = camera.Position,
            Target = camera.Target,
            Up = camera.Up,
            FovY = camera.FieldOfView * 180.0f / MathF.PI,
            Projection = CameraProjection.Perspective
        };
    }

    private Camera GetCamera(World world)
    {
        var width = Raylib.GetScreenWidth();
        var height = Raylib.GetScreenHeight();
        var aspect = height > 0 ? (float)width / height : 16f / 9f;

        var camera = new Camera(
            new Vector3(0.0f, 0.75f, -30.0f),
            new Vector3(0.0f, 0.5f, 0.0f),
            Vector3.UnitY,
            MathF.PI / 12.0f,
            aspect,
            0.1f,
            100.0f);

        world.Each((Entity e, ref Camera cam) =>
        {
            camera = cam;
        });

        camera.AspectRatio = aspect;
        return camera;
    }

    private void CollectLights(World world)
    {
        var count = 0;
        world.Each((Entity e, ref Light light) =>
        {
            if (count >= 4)
                return;
            _lightDirs[count * 3 + 0] = light.Direction.X;
            _lightDirs[count * 3 + 1] = light.Direction.Y;
            _lightDirs[count * 3 + 2] = light.Direction.Z;
            _lightPositions[count * 3 + 0] = light.Position.X;
            _lightPositions[count * 3 + 1] = light.Position.Y;
            _lightPositions[count * 3 + 2] = light.Position.Z;
            _lightIntensities[count] = light.Intensity;
            _lightColors[count * 3 + 0] = light.Color.X;
            _lightColors[count * 3 + 1] = light.Color.Y;
            _lightColors[count * 3 + 2] = light.Color.Z;
            _lightTypes[count] = (int)light.Type;
            _lightRanges[count] = light.Range;
            count++;
        });

        if (count == 0)
        {
            _lightDirs[0] = 0.5f; _lightDirs[1] = -1.0f; _lightDirs[2] = -0.5f;
            _lightIntensities[0] = 1.0f;
            _lightColors[0] = 1.0f; _lightColors[1] = 0.95f; _lightColors[2] = 0.8f;
            _lightTypes[0] = (int)LightType.Directional;
            _lightRanges[0] = 20f;
            count = 1;
        }

        for (var i = count; i < 4; i++)
        {
            _lightDirs[i * 3 + 0] = 0;
            _lightDirs[i * 3 + 1] = 0;
            _lightDirs[i * 3 + 2] = 0;
            _lightPositions[i * 3 + 0] = 0;
            _lightPositions[i * 3 + 1] = 0;
            _lightPositions[i * 3 + 2] = 0;
            _lightIntensities[i] = 0.0f;
            _lightColors[i * 3 + 0] = 0;
            _lightColors[i * 3 + 1] = 0;
            _lightColors[i * 3 + 2] = 0;
            _lightTypes[i] = 0;
            _lightRanges[i] = 0;
        }

        _lightCount = count;
        Raylib.SetShaderValue(_shader, _lightCountLoc, count, ShaderUniformDataType.Int);
        Raylib.SetShaderValueV(_shader, _lightDirLoc, _lightDirs, ShaderUniformDataType.Vec3, 4);
        Raylib.SetShaderValueV(_shader, _lightIntensityLoc, _lightIntensities, ShaderUniformDataType.Float, 4);
        Raylib.SetShaderValueV(_shader, _lightColorLoc, _lightColors, ShaderUniformDataType.Vec3, 4);
        Raylib.SetShaderValueV(_shader, _lightPosLoc, _lightPositions, ShaderUniformDataType.Vec3, 4);
        Raylib.SetShaderValueV(_shader, _lightTypeLoc, _lightTypes, ShaderUniformDataType.Int, 4);
        Raylib.SetShaderValueV(_shader, _lightRangeLoc, _lightRanges, ShaderUniformDataType.Float, 4);
    }

    private void SetFrameLights()
    {
        Raylib.SetShaderValue(_shader, _ambientLoc, new float[] { 0.35f, 0.35f, 0.4f }, ShaderUniformDataType.Vec3);
    }

    private unsafe void SetMaterialUniforms(EngineMaterial material, Raylib_cs.Model model)
    {
        Raylib.SetShaderValue(_shader, _materialColorLoc, new float[] { material.Albedo.X, material.Albedo.Y, material.Albedo.Z, 1.0f }, ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(_shader, _roughnessLoc, material.Roughness, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(_shader, _metallicLoc, material.Metallic, ShaderUniformDataType.Float);

        if (material.HasTexture && File.Exists(material.TexturePath!))
        {
            Raylib.SetShaderValue(_shader, _useTextureLoc, 1, ShaderUniformDataType.Int);
            var texture = GetOrLoadTexture(material.TexturePath!);
            Raylib.SetMaterialTexture(ref model.Materials[0], MaterialMapIndex.Albedo, texture);
        }
        else
        {
            Raylib.SetShaderValue(_shader, _useTextureLoc, 0, ShaderUniformDataType.Int);
        }
    }

    private unsafe Raylib_cs.Model GetOrUploadModel(Entity e, EngineMesh mesh)
    {
        if (_modelCache.TryGetValue(e, out var model))
            return model;

        // Use Raylib's native mesh generation when possible — the manual UploadMesh
        // + LoadModelFromMesh path is unreliable for larger meshes because
        // LoadModelFromMesh reads CPU-side vertex pointers after UploadMesh.
        // For custom meshes (from OBJ/GLTF loaders), keep the CPU data alive.
        var raylibMesh = UploadRaylibMesh(mesh);
        model = Raylib.LoadModelFromMesh(raylibMesh);

        for (var i = 0; i < model.MaterialCount; i++)
        {
            model.Materials[i].Shader = _shader;
        }
        _modelCache[e] = model;
        return model;
    }

    private unsafe Raylib_cs.Mesh UploadRaylibMesh(EngineMesh mesh)
    {
        var vertexCount = mesh.Vertices.Length;
        var triangleCount = mesh.Indices.Length / 3;

        var raylibMesh = new Raylib_cs.Mesh
        {
            VertexCount = vertexCount,
            TriangleCount = triangleCount
        };

        var positionSize = vertexCount * 3 * sizeof(float);
        var normalSize = vertexCount * 3 * sizeof(float);
        var colorSize = vertexCount * 4;
        var texcoordSize = vertexCount * 2 * sizeof(float);
        var indexSize = mesh.Indices.Length * sizeof(ushort);

        // Use NativeMemory.Alloc so Raylib's UnloadMesh can free with RL_FREE (free).
        var positionPtr = (float*)NativeMemory.Alloc((nuint)positionSize, 4);
        var normalPtr = (float*)NativeMemory.Alloc((nuint)normalSize, 4);
        var colorPtr = (byte*)NativeMemory.Alloc((nuint)colorSize, 1);
        var texcoordPtr = (float*)NativeMemory.Alloc((nuint)texcoordSize, 4);
        var indexPtr = (ushort*)NativeMemory.Alloc((nuint)indexSize, 2);

        for (var i = 0; i < vertexCount; i++)
        {
            var v = mesh.Vertices[i];
            positionPtr[i * 3 + 0] = v.Position.X;
            positionPtr[i * 3 + 1] = v.Position.Y;
            positionPtr[i * 3 + 2] = v.Position.Z;

            normalPtr[i * 3 + 0] = v.Normal.X;
            normalPtr[i * 3 + 1] = v.Normal.Y;
            normalPtr[i * 3 + 2] = v.Normal.Z;

            colorPtr[i * 4 + 0] = (byte)Math.Clamp(v.Color.X * 255.0f, 0.0f, 255.0f);
            colorPtr[i * 4 + 1] = (byte)Math.Clamp(v.Color.Y * 255.0f, 0.0f, 255.0f);
            colorPtr[i * 4 + 2] = (byte)Math.Clamp(v.Color.Z * 255.0f, 0.0f, 255.0f);
            colorPtr[i * 4 + 3] = 255;

            texcoordPtr[i * 2 + 0] = v.Position.X;
            texcoordPtr[i * 2 + 1] = v.Position.Z;
        }

        for (var i = 0; i < mesh.Indices.Length; i++)
            indexPtr[i] = (ushort)mesh.Indices[i];

        raylibMesh.Vertices = positionPtr;
        raylibMesh.Normals = normalPtr;
        raylibMesh.Colors = colorPtr;
        raylibMesh.TexCoords = texcoordPtr;
        raylibMesh.Indices = indexPtr;

        Raylib.UploadMesh(ref raylibMesh, false);

        // Keep CPU-side data alive — LoadModelFromMesh reads these pointers
        // to compute the bounding box. They will be freed when the model is unloaded.
        return raylibMesh;
    }

    private Texture2D GetOrLoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var texture))
            return texture;

        texture = Raylib.LoadTexture(path);
        Raylib.SetTextureWrap(texture, TextureWrap.Repeat);
        Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);
        _textureCache[path] = texture;
        return texture;
    }

    private unsafe void CaptureScreenshot(ScreenshotRequest request)
    {
        var image = Raylib.LoadImageFromScreen();
        try
        {
            var directory = Path.GetDirectoryName(request.Path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            Raylib.ExportImage(image, request.Path);

            if (request.Tcs != null)
            {
                var size = 0;
                var fileType = stackalloc byte[] { (byte)'.', (byte)'p', (byte)'n', (byte)'g', 0 };
                var data = Raylib.ExportImageToMemory(image, (sbyte*)fileType, &size);
                var bytes = new byte[size];
                fixed (byte* p = bytes)
                {
                    Buffer.MemoryCopy(data, p, size, size);
                }
                Raylib.MemFree(data);
                request.Tcs.TrySetResult(bytes);
            }

            Console.WriteLine($"Screenshot saved: {request.Path}");
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    private Task<byte[]> CaptureAsync(string outputPath)
    {
        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingScreenshot = new ScreenshotRequest(outputPath, tcs);
        return tcs.Task;
    }






    private static Shader LoadShadowShader()
    {
        const string VertexSource = @"#version 330 core
in vec3 vertexPosition;
uniform mat4 mvp;
void main()
{
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

        const string FragmentSource = @"#version 330 core
void main() {}";

        return Raylib.LoadShaderFromMemory(VertexSource, FragmentSource);
    }

    private static Shader LoadShader()
    {
        const string VertexSource = @"#version 330 core
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
uniform mat4 mvp;
uniform mat4 matModel;
out vec3 vNormal;
out vec3 vWorldPos;
out vec4 vColor;
out vec2 vTexCoord;
void main()
{
    vec4 worldPos = matModel * vec4(vertexPosition, 1.0);
    vWorldPos = worldPos.xyz;
    vNormal = mat3(transpose(inverse(matModel))) * vertexNormal;
    vColor = vertexColor;
    vTexCoord = vertexTexCoord;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

        const string FragmentSource = @"#version 330 core
in vec3 vNormal;
in vec3 vWorldPos;
in vec4 vColor;
in vec2 vTexCoord;
out vec4 finalColor;
uniform vec4 materialColor;
uniform int useTexture;
uniform sampler2D texture0;
uniform float roughness;
uniform float metallic;
uniform vec3 viewPos;
uniform vec3 ambientColor;
uniform int lightCount;
uniform vec3 lightDirs[4];
uniform vec3 lightPositions[4];
uniform float lightIntensities[4];
uniform vec3 lightColors[4];
uniform int lightTypes[4];
uniform float lightRanges[4];
uniform mat4 lightViewProj;
uniform sampler2D shadowMap;

vec3 ACESFilm(vec3 x)
{
    const float a = 2.51; const float b = 0.03; const float c = 2.43; const float d = 0.59; const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

float Attenuation(float dist, float range)
{
    float r = max(range, 0.001);
    float d = max(dist, 0.001);
    float x = d / r;
    float x2 = x * x;
    float x4 = x2 * x2;
    return clamp(1.0 / (1.0 + 25.0 * x4), 0.0, 1.0) * smoothstep(1.0, 0.0, x);
}

float CalculateShadow(vec3 worldPos)
{
    vec4 lp = lightViewProj * vec4(worldPos, 1.0);
    vec3 ndc = lp.xyz / lp.w;
    vec3 uvw = ndc * 0.5 + 0.5;
    if (uvw.x < 0.0 || uvw.x > 1.0 || uvw.y < 0.0 || uvw.y > 1.0 || uvw.z > 1.0)
        return 1.0;
    float bias = 0.005;
    vec2 ts = vec2(1.0 / 2048.0);
    float s = 0.0;
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            float d = texture(shadowMap, uvw.xy + vec2(x, y) * ts).r;
            s += (uvw.z - bias > d) ? 0.3 : 1.0;
        }
    }
    return s / 9.0;
}

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 albedo = pow(vColor.rgb * materialColor.rgb, vec3(2.2));
    if (useTexture != 0)
    {
        vec2 uv = vTexCoord * 4.0;
        vec3 texColor = pow(texture(texture0, uv).rgb, vec3(2.2));
        albedo *= texColor;
    }

    vec3 viewDir = normalize(viewPos - vWorldPos);
    float rough = clamp(roughness, 0.05, 1.0);
    float metal = clamp(metallic, 0.0, 1.0);

    vec3 skyColor = ambientColor;
    vec3 groundColor = ambientColor * 0.2;
    float hemisphere = 0.5 + 0.5 * normal.y;
    vec3 result = albedo * mix(groundColor, skyColor, hemisphere) * 0.4;

    // Shadow factor for first directional light
    float shadow = 1.0;
    if (lightCount > 0 && lightTypes[0] == 0)
        shadow = CalculateShadow(vWorldPos);

    vec3 F0 = mix(vec3(0.04), albedo, metal);
    float shininess = mix(8.0, 256.0, 1.0 - rough);

    for (int i = 0; i < lightCount; i++)
    {
        vec3 L;
        float atten = 1.0;

        if (lightTypes[i] == 1)
        {
            vec3 toLight = lightPositions[i] - vWorldPos;
            float dist = length(toLight);
            L = toLight / max(dist, 0.001);
            atten = Attenuation(dist, lightRanges[i]);
        }
        else
        {
            L = normalize(-lightDirs[i]);
        }

        float lightShadow = (i == 0 && lightTypes[0] == 0) ? shadow : 1.0;

        vec3 H = normalize(L + viewDir);
        float NdotL = max(dot(normal, L), 0.0);
        float NdotH = max(dot(normal, H), 0.0);
        float HdotV = max(dot(H, viewDir), 0.0);
        float diff = NdotL;
        float spec = pow(NdotH, shininess);
        vec3 fresnel = F0 + (1.0 - F0) * pow(1.0 - HdotV, 5.0);
        vec3 specularColor = mix(fresnel, albedo * fresnel, metal);
        vec3 diffuse = albedo * lightColors[i] * diff * lightIntensities[i] * atten * 1.5 * lightShadow;
        vec3 specular = specularColor * spec * lightIntensities[i] * atten * lightShadow;
        diffuse *= (1.0 - fresnel * (1.0 - metal * 0.5));
        result += diffuse + specular;
    }

    result = ACESFilm(result * 1.2);
    result = pow(result, vec3(1.0 / 2.2));
    finalColor = vec4(result, 1.0);
}";

        return Raylib.LoadShaderFromMemory(VertexSource, FragmentSource);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var model in _modelCache.Values)
            Raylib.UnloadModel(model);
        _modelCache.Clear();

        foreach (var texture in _textureCache.Values)
            Raylib.UnloadTexture(texture);
        _textureCache.Clear();

        Raylib.UnloadShader(_shadowShader);
        Rlgl.UnloadTexture(_shadowDepthTex);
        Rlgl.UnloadFramebuffer(_shadowFbo);
        Raylib.UnloadShader(_shader);
    }

    private readonly record struct ScreenshotRequest(string Path, TaskCompletionSource<byte[]>? Tcs);

    private sealed class RaylibScreenshotProvider : IScreenshotProvider
    {
        private readonly RaylibRenderer _renderer;

        public RaylibScreenshotProvider(RaylibRenderer renderer)
        {
            _renderer = renderer;
        }

        public Task<byte[]> CaptureAsync(string outputPath) => _renderer.CaptureAsync(outputPath);
    }
}
