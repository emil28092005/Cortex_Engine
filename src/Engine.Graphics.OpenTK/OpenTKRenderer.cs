using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Flecs.NET.Core;
using OpenTK.Graphics.OpenGL4;
using OTKMatrix = OpenTK.Mathematics.Matrix4;
using OTKVector3 = OpenTK.Mathematics.Vector3;
using EngineMaterial = Engine.Core.Components.Material;
using EngineMesh = Engine.Core.Components.Mesh;
using EngineTransform = Engine.Core.Components.Transform;

namespace Engine.Graphics.OpenTK;

public sealed class OpenTKRenderer : IRenderer
{
    private const int ShadowMapSize = 2048;

    private int _program;
    private int _shadowProgram;
    private int _shadowFbo;
    private int _shadowTexture;
    private readonly Dictionary<Entity, GLMesh> _meshCache = new();

    private readonly float[] _matrixBuf = new float[16];

    // Uniform locations
    private int _uMVP, _uModel, _uViewPos, _uMaterialColor, _uRoughness, _uMetallic;
    private int _uAmbient, _uLightCount, _uLightDirs, _uLightIntensities, _uLightColors;
    private int _uLightPositions, _uLightTypes, _uLightRanges;
    private int _uLightViewProj, _uShadowMap, _uUseTexture;

    // Light data
    private readonly float[] _lightDirs = new float[12];
    private readonly float[] _lightPositions = new float[12];
    private readonly float[] _lightIntensities = new float[4];
    private readonly float[] _lightColors = new float[12];
    private readonly int[] _lightTypes = new int[4];
    private readonly float[] _lightRanges = new float[4];
    private int _lightCount;

    private int _screenW = 1280, _screenH = 720;
    private bool _disposed;

    public OpenTKRenderer()
    {
        // Compile shaders
        _program = CreateProgram(VertexSrc, FragmentSrc);
        _shadowProgram = CreateProgram(ShadowVertSrc, ShadowFragSrc);

        // Get uniform locations
        _uMVP = GL.GetUniformLocation(_program, "mvp");
        _uModel = GL.GetUniformLocation(_program, "model");
        _uViewPos = GL.GetUniformLocation(_program, "viewPos");
        _uMaterialColor = GL.GetUniformLocation(_program, "materialColor");
        _uRoughness = GL.GetUniformLocation(_program, "roughness");
        _uMetallic = GL.GetUniformLocation(_program, "metallic");
        _uAmbient = GL.GetUniformLocation(_program, "ambientColor");
        _uLightCount = GL.GetUniformLocation(_program, "lightCount");
        _uLightDirs = GL.GetUniformLocation(_program, "lightDirs");
        _uLightIntensities = GL.GetUniformLocation(_program, "lightIntensities");
        _uLightColors = GL.GetUniformLocation(_program, "lightColors");
        _uLightPositions = GL.GetUniformLocation(_program, "lightPositions");
        _uLightTypes = GL.GetUniformLocation(_program, "lightTypes");
        _uLightRanges = GL.GetUniformLocation(_program, "lightRanges");
        _uLightViewProj = GL.GetUniformLocation(_program, "lightViewProj");
        _uShadowMap = GL.GetUniformLocation(_program, "shadowMap");
        _uUseTexture = GL.GetUniformLocation(_program, "useTexture");

        // Shadow FBO
        _shadowFbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);

        _shadowTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _shadowTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
            ShadowMapSize, ShadowMapSize, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, _shadowTexture, 0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // GL state
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
    }

    public void SetScreenSize(int w, int h) { _screenW = w; _screenH = h; }

    public void RequestScreenshot(string path) { }
    public bool IsScreenshotRequested => false;
    public IScreenshotProvider ScreenshotProvider => new DummyScreenshotProvider();

    public void RenderWorld(World world)
    {
        var camera = GetCamera(world);
        CollectLights(world);

        var hasDirLight = _lightCount > 0 && _lightTypes[0] == (int)LightType.Directional;
        OTKMatrix lightVP = OTKMatrix.Identity;

        // === PASS 1: Shadow ===
        if (hasDirLight)
        {
            var lightDir = new Vector3(_lightDirs[0], _lightDirs[1], _lightDirs[2]);
            var center = new Vector3(0, 0.5f, 0);
            var lightPos = center - lightDir * 30f;
            var up = MathF.Abs(Vector3.Dot(lightDir, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

            lightVP = OTKMatrix.CreateOrthographicOffCenter(-15, 15, -15, 15, 1, 80)
                    * OTKMatrix.LookAt(ToV3(lightPos), ToV3(center), ToV3(up));

            GL.Viewport(0, 0, ShadowMapSize, ShadowMapSize);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.UseProgram(_shadowProgram);
            GL.CullFace(CullFaceMode.Front);

            int sMvp = GL.GetUniformLocation(_shadowProgram, "mvp");

            world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform t) =>
            {
                if (e.Name() == "Grid" || e.Name() == "Floor") return;
                var gm = GetOrUploadMesh(e, mesh);
                var model = ToM4(t.GetMatrix());
                var mvp = lightVP * model;
                SetUniformMat4(sMvp, mvp);
                DrawMesh(gm);
            });

            GL.CullFace(CullFaceMode.Back);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // === PASS 2: Main ===
        GL.Viewport(0, 0, _screenW, _screenH);
        GL.ClearColor(0.098f, 0.118f, 0.157f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.UseProgram(_program);

        var view = OTKMatrix.LookAt(ToV3(camera.Position), ToV3(camera.Target), ToV3(camera.Up));
        var proj = OTKMatrix.CreatePerspectiveFieldOfView(camera.FieldOfView, camera.AspectRatio, camera.NearPlane, camera.FarPlane);

        GL.Uniform3(_uViewPos, camera.Position.X, camera.Position.Y, camera.Position.Z);
        GL.Uniform3(_uAmbient, 0.35f, 0.35f, 0.4f);
        GL.Uniform1(_uLightCount, _lightCount);
        GL.Uniform3(_uLightDirs, 4, _lightDirs);
        GL.Uniform1(_uLightIntensities, 4, _lightIntensities);
        GL.Uniform3(_uLightColors, 4, _lightColors);
        GL.Uniform3(_uLightPositions, 4, _lightPositions);
        GL.Uniform1(_uLightTypes, 4, _lightTypes);
        GL.Uniform1(_uLightRanges, 4, _lightRanges);

        if (hasDirLight)
        {
            SetUniformMat4(_uLightViewProj, lightVP);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _shadowTexture);
            GL.Uniform1(_uShadowMap, 1);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform t) =>
        {
            if (e.Name() == "Grid") return;
            var mat = e.Has<EngineMaterial>() ? e.Get<EngineMaterial>() : EngineMaterial.Default;
            var gm = GetOrUploadMesh(e, mesh);
            var model = ToM4(t.GetMatrix());
            var mvp = proj * view * model;

            SetUniformMat4(_uMVP, mvp);
            SetUniformMat4(_uModel, model);
            GL.Uniform4(_uMaterialColor, mat.Albedo.X, mat.Albedo.Y, mat.Albedo.Z, 1f);
            GL.Uniform1(_uRoughness, mat.Roughness);
            GL.Uniform1(_uMetallic, mat.Metallic);
            GL.Uniform1(_uUseTexture, 0);

            DrawMesh(gm);
        });
    }

    private void DrawMesh(GLMesh m)
    {
        GL.BindVertexArray(m.Vao);
        GL.DrawElements(PrimitiveType.Triangles, m.Count, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    private void SetUniformMat4(int loc, OTKMatrix mat)
    {
        // OpenTK Matrix4 is row-major in memory; OpenGL expects column-major with transpose=false.
        // Use transpose=true so OpenGL transposes our row-major data into column-major.
        _matrixBuf[0] = mat.M11; _matrixBuf[1] = mat.M12; _matrixBuf[2] = mat.M13; _matrixBuf[3] = mat.M14;
        _matrixBuf[4] = mat.M21; _matrixBuf[5] = mat.M22; _matrixBuf[6] = mat.M23; _matrixBuf[7] = mat.M24;
        _matrixBuf[8] = mat.M31; _matrixBuf[9] = mat.M32; _matrixBuf[10] = mat.M33; _matrixBuf[11] = mat.M34;
        _matrixBuf[12] = mat.M41; _matrixBuf[13] = mat.M42; _matrixBuf[14] = mat.M43; _matrixBuf[15] = mat.M44;
        GL.UniformMatrix4(loc, 1, true, _matrixBuf);
    }

    private GLMesh GetOrUploadMesh(Entity e, EngineMesh mesh)
    {
        if (_meshCache.TryGetValue(e, out var existing))
            return existing;

        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        // Position
        float[] pos = new float[mesh.Vertices.Length * 3];
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            pos[i*3] = mesh.Vertices[i].Position.X;
            pos[i*3+1] = mesh.Vertices[i].Position.Y;
            pos[i*3+2] = mesh.Vertices[i].Position.Z;
        }
        int vboPos = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboPos);
        GL.BufferData(BufferTarget.ArrayBuffer, pos.Length * sizeof(float), pos, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

        // Normal
        float[] nrm = new float[mesh.Vertices.Length * 3];
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            nrm[i*3] = mesh.Vertices[i].Normal.X;
            nrm[i*3+1] = mesh.Vertices[i].Normal.Y;
            nrm[i*3+2] = mesh.Vertices[i].Normal.Z;
        }
        int vboNrm = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboNrm);
        GL.BufferData(BufferTarget.ArrayBuffer, nrm.Length * sizeof(float), nrm, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);

        // Color
        float[] col = new float[mesh.Vertices.Length * 4];
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            col[i*4] = mesh.Vertices[i].Color.X;
            col[i*4+1] = mesh.Vertices[i].Color.Y;
            col[i*4+2] = mesh.Vertices[i].Color.Z;
            col[i*4+3] = 1f;
        }
        int vboCol = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboCol);
        GL.BufferData(BufferTarget.ArrayBuffer, col.Length * sizeof(float), col, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 0, 0);

        // Indices
        int ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);

        var gm = new GLMesh(vao, mesh.Indices.Length);
        _meshCache[e] = gm;
        return gm;
    }

    private void CollectLights(World world)
    {
        int count = 0;
        world.Each((Entity e, ref Light light) =>
        {
            if (count >= 4) return;
            _lightDirs[count*3] = light.Direction.X;
            _lightDirs[count*3+1] = light.Direction.Y;
            _lightDirs[count*3+2] = light.Direction.Z;
            _lightPositions[count*3] = light.Position.X;
            _lightPositions[count*3+1] = light.Position.Y;
            _lightPositions[count*3+2] = light.Position.Z;
            _lightIntensities[count] = light.Intensity;
            _lightColors[count*3] = light.Color.X;
            _lightColors[count*3+1] = light.Color.Y;
            _lightColors[count*3+2] = light.Color.Z;
            _lightTypes[count] = (int)light.Type;
            _lightRanges[count] = light.Range;
            count++;
        });
        if (count == 0)
        {
            _lightDirs[0] = 0.5f; _lightDirs[1] = -1; _lightDirs[2] = -0.5f;
            _lightIntensities[0] = 1; _lightColors[0] = 1; _lightColors[1] = 0.95f; _lightColors[2] = 0.8f;
            _lightTypes[0] = (int)LightType.Directional; _lightRanges[0] = 20;
            count = 1;
        }
        for (int i = count; i < 4; i++) { _lightIntensities[i] = 0; _lightTypes[i] = 0; }
        _lightCount = count;
    }

    private Camera GetCamera(World world)
    {
        var cam = new Camera(new Vector3(0, 0.75f, -30), new Vector3(0, 0.5f, 0), Vector3.UnitY, MathF.PI/12, 16f/9f, 0.1f, 100f);
        world.Each((Entity e, ref Camera c) => cam = c);
        return cam;
    }

    private static OTKMatrix ToM4(System.Numerics.Matrix4x4 m) => new(
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44);

    private static OTKVector3 ToV3(Vector3 v) => new(v.X, v.Y, v.Z);

    private static int CreateProgram(string vsSrc, string fsSrc)
    {
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vsSrc);
        GL.CompileShader(vs);
        GL.GetShader(vs, ShaderParameter.CompileStatus, out int vsOk);
        if (vsOk == 0) throw new Exception($"VS compile: {GL.GetShaderInfoLog(vs)}");

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fsSrc);
        GL.CompileShader(fs);
        GL.GetShader(fs, ShaderParameter.CompileStatus, out int fsOk);
        if (fsOk == 0) throw new Exception($"FS compile: {GL.GetShaderInfoLog(fs)}");

        int prog = GL.CreateProgram();
        GL.AttachShader(prog, vs);
        GL.AttachShader(prog, fs);
        GL.LinkProgram(prog);
        GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int linkOk);
        if (linkOk == 0) throw new Exception($"Link: {GL.GetProgramInfoLog(prog)}");

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        return prog;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var m in _meshCache.Values) GL.DeleteVertexArray(m.Vao);
        _meshCache.Clear();
        GL.DeleteProgram(_program);
        GL.DeleteProgram(_shadowProgram);
        GL.DeleteFramebuffer(_shadowFbo);
        GL.DeleteTexture(_shadowTexture);
    }

    // === Shaders ===
    private const string ShadowVertSrc = @"#version 330 core
layout(location=0) in vec3 aPos;
uniform mat4 mvp;
void main(){ gl_Position = mvp * vec4(aPos, 1.0); }";

    private const string ShadowFragSrc = @"#version 330 core
void main(){}";

    private const string VertexSrc = @"#version 330 core
layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec4 aColor;
uniform mat4 mvp;
uniform mat4 model;
out vec3 vNormal;
out vec3 vWorldPos;
out vec4 vColor;
void main()
{
    vec4 wp = model * vec4(aPos, 1.0);
    vWorldPos = wp.xyz;
    vNormal = mat3(transpose(inverse(model))) * aNormal;
    vColor = aColor;
    gl_Position = mvp * vec4(aPos, 1.0);
}";

    private const string FragmentSrc = @"#version 330 core
in vec3 vNormal;
in vec3 vWorldPos;
in vec4 vColor;
out vec4 finalColor;
uniform vec4 materialColor;
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
uniform int useTexture;

vec3 ACESFilm(vec3 x)
{
    const float a=2.51, b=0.03, c=2.43, d=0.59, e=0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

float Attenuation(float dist, float range)
{
    float r = max(range, 0.001);
    float d = max(dist, 0.001);
    float x = d/r, x2 = x*x, x4 = x2*x2;
    return clamp(1.0/(1.0+25.0*x4), 0.0, 1.0) * smoothstep(1.0, 0.0, x);
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
            float dpt = texture(shadowMap, uvw.xy + vec2(x, y) * ts).r;
            s += (uvw.z - bias > dpt) ? 0.3 : 1.0;
        }
    }
    return s / 9.0;
}

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 albedo = pow(vColor.rgb * materialColor.rgb, vec3(2.2));
    vec3 viewDir = normalize(viewPos - vWorldPos);
    float rough = clamp(roughness, 0.05, 1.0);
    float metal = clamp(metallic, 0.0, 1.0);

    vec3 skyColor = ambientColor;
    vec3 groundColor = ambientColor * 0.2;
    float hemisphere = 0.5 + 0.5 * normal.y;
    vec3 result = albedo * mix(groundColor, skyColor, hemisphere) * 0.4;

    float shadow = 1.0;
    if (lightCount > 0 && lightTypes[0] == 0)
        shadow = CalculateShadow(vWorldPos);

    vec3 F0 = mix(vec3(0.04), albedo, metal);
    float shininess = mix(8.0, 256.0, 1.0 - rough);

    for (int i = 0; i < lightCount; i++)
    {
        vec3 L;
        float atten = 1.0;
        if (lightTypes[i] == 1) {
            vec3 toLight = lightPositions[i] - vWorldPos;
            float dist = length(toLight);
            L = toLight / max(dist, 0.001);
            atten = Attenuation(dist, lightRanges[i]);
        } else {
            L = normalize(-lightDirs[i]);
        }
        float lightShadow = (i == 0 && lightTypes[0] == 0) ? shadow : 1.0;
        vec3 H = normalize(L + viewDir);
        float NdotL = max(dot(normal, L), 0.0);
        float NdotH = max(dot(normal, H), 0.0);
        float HdotV = max(dot(H, viewDir), 0.0);
        float spec = pow(NdotH, shininess);
        vec3 fresnel = F0 + (1.0 - F0) * pow(1.0 - HdotV, 5.0);
        vec3 specColor = mix(fresnel, albedo * fresnel, metal);
        vec3 diffuse = albedo * lightColors[i] * NdotL * lightIntensities[i] * atten * 1.5 * lightShadow;
        vec3 specular = specColor * spec * lightIntensities[i] * atten * lightShadow;
        diffuse *= (1.0 - fresnel * (1.0 - metal * 0.5));
        result += diffuse + specular;
    }

    result = ACESFilm(result * 1.2);
    result = pow(result, vec3(1.0 / 2.2));
    finalColor = vec4(result, 1.0);
}";

    private readonly record struct GLMesh(int Vao, int Count);

    private sealed class DummyScreenshotProvider : IScreenshotProvider
    {
        public Task<byte[]> CaptureAsync(string outputPath) => Task.FromResult(Array.Empty<byte>());
    }
}
