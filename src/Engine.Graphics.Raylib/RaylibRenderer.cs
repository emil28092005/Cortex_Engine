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
    private readonly Shader _shader;
    private readonly Shader _shadowShader;
    private readonly RenderTexture2D _shadowMapRT;
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
    private readonly int _lightSpaceMatrixLoc;
    private readonly int _useShadowLoc;
    private readonly float[] _lightDirs = new float[12];
    private readonly float[] _lightIntensities = new float[4];
    private readonly float[] _lightColors = new float[12];
    private readonly float[] _lightSpaceMatrixData = new float[16];

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
        _shadowMapRT = Raylib.LoadRenderTexture(1024, 1024);
        Raylib.SetTextureFilter(_shadowMapRT.Texture, TextureFilter.Trilinear);

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
        _lightSpaceMatrixLoc = Raylib.GetShaderLocation(_shader, "lightSpaceMatrix");
        _useShadowLoc = Raylib.GetShaderLocation(_shader, "useShadow");
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

        Raylib.BeginDrawing();

        // --- Shadow pass disabled — needs multi-texture-unit support ---
        // RenderShadowPass(world);

        // --- Main pass (render to screen) ---
        Raylib.ClearBackground(new Color(25, 30, 40, 255));
        Raylib.BeginMode3D(ToRaylib(camera));

        Rlgl.DisableBackfaceCulling();

        CollectLights(world);
        SetFrameLights();
        Raylib.SetShaderValue(_shader, _viewPosLoc, new float[] { camera.Position.X, camera.Position.Y, camera.Position.Z }, ShaderUniformDataType.Vec3);

        // Enable shadows
        // Shadows disabled — requires multi-texture-unit support not available through Raylib's DrawModelEx
        Raylib.SetShaderValue(_shader, _useShadowLoc, 0, ShaderUniformDataType.Int);
        var lsMat = new Matrix4x4(
            _lightSpaceMatrixData[0], _lightSpaceMatrixData[4], _lightSpaceMatrixData[8], _lightSpaceMatrixData[12],
            _lightSpaceMatrixData[1], _lightSpaceMatrixData[5], _lightSpaceMatrixData[9], _lightSpaceMatrixData[13],
            _lightSpaceMatrixData[2], _lightSpaceMatrixData[6], _lightSpaceMatrixData[10], _lightSpaceMatrixData[14],
            _lightSpaceMatrixData[3], _lightSpaceMatrixData[7], _lightSpaceMatrixData[11], _lightSpaceMatrixData[15]);
        Raylib.SetShaderValueMatrix(_shader, _lightSpaceMatrixLoc, lsMat);

        world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
        {
            if (e.Name() == "Grid")
                return;

            var material = e.Has<EngineMaterial>() ? e.Get<EngineMaterial>() : EngineMaterial.Default;
            var model = GetOrUploadModel(e, mesh);
            var modelMatrix = transform.GetMatrix();

            if (Matrix4x4.Decompose(modelMatrix, out var scale, out var rotation, out var position))
            {
                var axis = Vector3.UnitY;
                var angle = 0.0f;
                var q = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
                if (MathF.Abs(q.W) < 0.9999999f)
                {
                    angle = 2.0f * MathF.Acos(Math.Clamp(q.W, -1.0f, 1.0f));
                    var s = MathF.Sqrt(1.0f - q.W * q.W);
                    if (s > 0.0001f)
                        axis = new Vector3(q.X / s, q.Y / s, q.Z / s);
                    else
                        axis = new Vector3(q.X, q.Y, q.Z);
                }

                SetMaterialUniforms(material, model);
                Raylib.DrawModelEx(model, position, axis, angle * 180.0f / MathF.PI, scale, Color.White);
            }
        });

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
            _lightIntensities[count] = light.Intensity;
            _lightColors[count * 3 + 0] = light.Color.X;
            _lightColors[count * 3 + 1] = light.Color.Y;
            _lightColors[count * 3 + 2] = light.Color.Z;
            count++;
        });

        if (count == 0)
        {
            _lightDirs[0] = 0.5f; _lightDirs[1] = -1.0f; _lightDirs[2] = -0.5f;
            _lightIntensities[0] = 1.0f;
            _lightColors[0] = 1.0f; _lightColors[1] = 0.95f; _lightColors[2] = 0.8f;
            count = 1;
        }

        for (var i = count; i < 4; i++)
        {
            _lightDirs[i * 3 + 0] = 0;
            _lightDirs[i * 3 + 1] = 0;
            _lightDirs[i * 3 + 2] = 0;
            _lightIntensities[i] = 0.0f;
            _lightColors[i * 3 + 0] = 0;
            _lightColors[i * 3 + 1] = 0;
            _lightColors[i * 3 + 2] = 0;
        }

        Raylib.SetShaderValue(_shader, _lightCountLoc, count, ShaderUniformDataType.Int);
        Raylib.SetShaderValueV(_shader, _lightDirLoc, _lightDirs, ShaderUniformDataType.Vec3, 4);
        Raylib.SetShaderValueV(_shader, _lightIntensityLoc, _lightIntensities, ShaderUniformDataType.Float, 4);
        Raylib.SetShaderValueV(_shader, _lightColorLoc, _lightColors, ShaderUniformDataType.Vec3, 4);
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

    private void RenderShadowPass(World world)
    {
        if (_lightDirs[0] == 0 && _lightDirs[1] == 0 && _lightDirs[2] == 0)
            return;

        var lightDir = new Vector3(_lightDirs[0], _lightDirs[1], _lightDirs[2]);
        var sceneCenter = new Vector3(0, 0.5f, 0);
        var lightPos = sceneCenter - lightDir * 30f;

        var lightView = Matrix4x4.CreateLookAt(lightPos, sceneCenter, Vector3.UnitY);
        var lightProj = Matrix4x4.CreateOrthographic(25f, 25f, 1f, 80f);
        var lightSpace = lightProj * lightView;

        // Store for main pass (column-major for OpenGL)
        _lightSpaceMatrixData[0] = lightSpace.M11;  _lightSpaceMatrixData[4] = lightSpace.M12;  _lightSpaceMatrixData[8]  = lightSpace.M13;  _lightSpaceMatrixData[12] = lightSpace.M14;
        _lightSpaceMatrixData[1] = lightSpace.M21;  _lightSpaceMatrixData[5] = lightSpace.M22;  _lightSpaceMatrixData[9]  = lightSpace.M23;  _lightSpaceMatrixData[13] = lightSpace.M24;
        _lightSpaceMatrixData[2] = lightSpace.M31;  _lightSpaceMatrixData[6] = lightSpace.M32;  _lightSpaceMatrixData[10] = lightSpace.M33;  _lightSpaceMatrixData[14] = lightSpace.M34;
        _lightSpaceMatrixData[3] = lightSpace.M41;  _lightSpaceMatrixData[7] = lightSpace.M42;  _lightSpaceMatrixData[11] = lightSpace.M43;  _lightSpaceMatrixData[15] = lightSpace.M44;

        var shadowCamera = new Camera3D
        {
            Position = lightPos,
            Target = sceneCenter,
            Up = Vector3.UnitY,
            FovY = 0,
            Projection = CameraProjection.Orthographic
        };

        Raylib.BeginTextureMode(_shadowMapRT);
        Raylib.ClearBackground(new Color(255, 255, 255, 255));
        Raylib.BeginMode3D(shadowCamera);

        world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
        {
            if (e.Name() == "Grid")
                return;

            var model = GetOrUploadModel(e, mesh);
            var modelMatrix = transform.GetMatrix();

            if (Matrix4x4.Decompose(modelMatrix, out var scale, out var rotation, out var position))
            {
                var axis = Vector3.UnitY;
                var angle = 0.0f;
                var q = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
                if (MathF.Abs(q.W) < 0.9999999f)
                {
                    angle = 2.0f * MathF.Acos(Math.Clamp(q.W, -1.0f, 1.0f));
                    var s = MathF.Sqrt(1.0f - q.W * q.W);
                    if (s > 0.0001f)
                        axis = new Vector3(q.X / s, q.Y / s, q.Z / s);
                    else
                        axis = new Vector3(q.X, q.Y, q.Z);
                }

                // Swap to shadow shader, draw, swap back
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
        Raylib.EndTextureMode();
    }

    private static Shader LoadShadowShader()
    {
        const string VertexSource = @"#version 330 core
in vec3 vertexPosition;
uniform mat4 mvp;
uniform mat4 matModel;
out vec3 vWorldPos;
void main()
{
    vec4 worldPos = matModel * vec4(vertexPosition, 1.0);
    vWorldPos = worldPos.xyz;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

        const string FragmentSource = @"#version 330 core
out vec4 fragColor;
void main()
{
    fragColor = vec4(vec3(gl_FragCoord.z), 1.0);
}";

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
uniform float lightIntensities[4];
uniform vec3 lightColors[4];

vec3 ACESFilm(vec3 x)
{
    const float a = 2.51; const float b = 0.03; const float c = 2.43; const float d = 0.59; const float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 albedo = vColor.rgb * materialColor.rgb;
    if (useTexture != 0)
    {
        vec2 uv = vTexCoord * 4.0;
        albedo *= texture(texture0, uv).rgb;
    }

    vec3 viewDir = normalize(viewPos - vWorldPos);
    float rough = clamp(roughness, 0.05, 1.0);
    float metal = clamp(metallic, 0.0, 1.0);

    vec3 skyColor = ambientColor;
    vec3 groundColor = ambientColor * 0.2;
    float hemisphere = 0.5 + 0.5 * normal.y;
    vec3 result = albedo * mix(groundColor, skyColor, hemisphere) * 0.4;

    vec3 F0 = mix(vec3(0.04), albedo, metal);
    float shininess = mix(8.0, 256.0, 1.0 - rough);

    for (int i = 0; i < lightCount; i++)
    {
        vec3 L = normalize(-lightDirs[i]);
        vec3 H = normalize(L + viewDir);
        float NdotL = max(dot(normal, L), 0.0);
        float NdotH = max(dot(normal, H), 0.0);
        float HdotV = max(dot(H, viewDir), 0.0);
        float diff = NdotL;
        float spec = pow(NdotH, shininess);
        float fresnel = F0.x + (1.0 - F0.x) * pow(1.0 - HdotV, 5.0);
        vec3 specularColor = mix(vec3(fresnel), albedo * fresnel, metal);
        vec3 diffuse = albedo * lightColors[i] * diff * lightIntensities[i] * 1.5;
        vec3 specular = specularColor * spec * lightIntensities[i];
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

        Raylib.UnloadRenderTexture(_shadowMapRT);
        Raylib.UnloadShader(_shadowShader);
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
