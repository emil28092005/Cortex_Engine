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
using OTKVector4 = OpenTK.Mathematics.Vector4;
using EngineMaterial = Engine.Core.Components.Material;
using EngineMesh = Engine.Core.Components.Mesh;
using EngineTransform = Engine.Core.Components.Transform;

namespace Engine.Graphics.OpenTK;

/// <summary>
/// OpenTK OpenGL renderer — full control over OpenGL state for shadow mapping.
/// </summary>
public sealed class OpenTKRenderer : IRenderer
{
    private const int ShadowMapSize = 2048;

    private readonly int _program;
    private readonly int _shadowProgram;
    private readonly int _shadowFbo;
    private readonly int _shadowTexture;
    private readonly Dictionary<Entity, GLMesh> _meshCache = new();

    // Uniform locations
    private readonly int _uMVP, _uModel, _uViewPos, _uMaterialColor, _uRoughness, _uMetallic;
    private readonly int _uAmbient, _uLightCount, _uLightDirs, _uLightIntensities, _uLightColors;
    private readonly int _uLightPositions, _uLightTypes, _uLightRanges;
    private readonly int _uLightViewProj, _uShadowMap, _uUseTexture;

    // Light data
    private readonly float[] _lightDirs = new float[12];
    private readonly float[] _lightPositions = new float[12];
    private readonly float[] _lightIntensities = new float[4];
    private readonly float[] _lightColors = new float[12];
    private readonly int[] _lightTypes = new int[4];
    private readonly float[] _lightRanges = new float[4];
    private int _lightCount;

    private int _screenWidth = 1280;
    private int _screenHeight = 720;
    private bool _disposed;

    public OpenTKRenderer()
    {
        GL.Enable(EnableCap.DepthTest);

        _program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _shadowProgram = CreateProgram(ShadowVertexSource, ShadowFragmentSource);

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

        // Shadow FBO with depth texture
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
    }

    public void SetScreenSize(int w, int h) { _screenWidth = w; _screenHeight = h; }

    public void RequestScreenshot(string outputPath) { }
    public bool IsScreenshotRequested => false;
    public IScreenshotProvider ScreenshotProvider => new OpenTKScreenshotProvider();

    public void RenderWorld(World world)
    {
        var camera = GetCamera(world);
        CollectLights(world);

        var hasDirLight = _lightCount > 0 && _lightTypes[0] == (int)LightType.Directional;
        OTKMatrix lightViewProj = OTKMatrix.Identity;

        // === PASS 1: Shadow map ===
        if (hasDirLight)
        {
            var lightDir = new System.Numerics.Vector3(_lightDirs[0], _lightDirs[1], _lightDirs[2]);
            var sceneCenter = new System.Numerics.Vector3(0, 0.5f, 0);
            var lightPos = sceneCenter - lightDir * 30f;
            var up = MathF.Abs(System.Numerics.Vector3.Dot(lightDir, System.Numerics.Vector3.UnitY)) > 0.99f
                ? System.Numerics.Vector3.UnitZ : System.Numerics.Vector3.UnitY;

            lightViewProj = OTKMatrix.CreateOrthographicOffCenter(-15, 15, -15, 15, 1, 80)
                          * OTKMatrix.LookAt(ToOTK(lightPos), ToOTK(sceneCenter), ToOTK(up));

            GL.Viewport(0, 0, ShadowMapSize, ShadowMapSize);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.UseProgram(_shadowProgram);
            GL.CullFace(CullFaceMode.Front);

            var shadowMvpLoc = GL.GetUniformLocation(_shadowProgram, "mvp");

            world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
            {
                if (e.Name() == "Grid" || e.Name() == "Floor") return;
                var glMesh = GetOrUploadMesh(e, mesh);
                var model = ToOTK(transform.GetMatrix());
                var mvp = lightViewProj * model;
                GL.UniformMatrix4(shadowMvpLoc, false, ref mvp);
                DrawMeshImmediate(glMesh);
            });

            GL.CullFace(CullFaceMode.Back);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // === PASS 2: Main render ===
        GL.Viewport(0, 0, _screenWidth, _screenHeight);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.ClearColor(0.098f, 0.118f, 0.157f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.UseProgram(_program);

        var view = OTKMatrix.LookAt(ToOTK(camera.Position), ToOTK(camera.Target), ToOTK(camera.Up));
        var proj = OTKMatrix.CreatePerspectiveFieldOfView(camera.FieldOfView, camera.AspectRatio, camera.NearPlane, camera.FarPlane);

        // Frame uniforms
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
            GL.UniformMatrix4(_uLightViewProj, false, ref lightViewProj);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _shadowTexture);
            GL.Uniform1(_uShadowMap, 1);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
        {
            if (e.Name() == "Grid") return;
            var material = e.Has<EngineMaterial>() ? e.Get<EngineMaterial>() : EngineMaterial.Default;
            var glMesh = GetOrUploadMesh(e, mesh);
            var model = ToOTK(transform.GetMatrix());
            var mvp = proj * view * model;

            GL.UniformMatrix4(_uMVP, false, ref mvp);
            GL.UniformMatrix4(_uModel, false, ref model);
            GL.Uniform4(_uMaterialColor, material.Albedo.X, material.Albedo.Y, material.Albedo.Z, 1.0f);
            GL.Uniform1(_uRoughness, material.Roughness);
            GL.Uniform1(_uMetallic, material.Metallic);
            GL.Uniform1(_uUseTexture, 0);

            DrawMeshImmediate(glMesh);
        });
    }

    private void DrawMeshImmediate(GLMesh mesh)
    {
        GL.BindVertexArray(mesh.Vao);
        GL.DrawElements(PrimitiveType.Triangles, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    private GLMesh GetOrUploadMesh(Entity e, EngineMesh mesh)
    {
        if (_meshCache.TryGetValue(e, out var existing))
            return existing;

        var vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        // Position (location 0)
        var positions = new float[mesh.Vertices.Length * 3];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            positions[i * 3] = mesh.Vertices[i].Position.X;
            positions[i * 3 + 1] = mesh.Vertices[i].Position.Y;
            positions[i * 3 + 2] = mesh.Vertices[i].Position.Z;
        }
        var posVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, posVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, positions.Length * sizeof(float), positions, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        // Normal (location 1)
        var normals = new float[mesh.Vertices.Length * 3];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            normals[i * 3] = mesh.Vertices[i].Normal.X;
            normals[i * 3 + 1] = mesh.Vertices[i].Normal.Y;
            normals[i * 3 + 2] = mesh.Vertices[i].Normal.Z;
        }
        var nrmVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, nrmVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * sizeof(float), normals, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        // Color (location 2)
        var colors = new float[mesh.Vertices.Length * 4];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            colors[i * 4] = mesh.Vertices[i].Color.X;
            colors[i * 4 + 1] = mesh.Vertices[i].Color.Y;
            colors[i * 4 + 2] = mesh.Vertices[i].Color.Z;
            colors[i * 4 + 3] = 1.0f;
        }
        var colVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, colVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, colors.Length * sizeof(float), colors, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

        // Indices
        var ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);

        var glMesh = new GLMesh(vao, mesh.Indices.Length);
        _meshCache[e] = glMesh;
        return glMesh;
    }

    private void CollectLights(World world)
    {
        var count = 0;
        world.Each((Entity e, ref Light light) =>
        {
            if (count >= 4) return;
            _lightDirs[count * 3] = light.Direction.X;
            _lightDirs[count * 3 + 1] = light.Direction.Y;
            _lightDirs[count * 3 + 2] = light.Direction.Z;
            _lightPositions[count * 3] = light.Position.X;
            _lightPositions[count * 3 + 1] = light.Position.Y;
            _lightPositions[count * 3 + 2] = light.Position.Z;
            _lightIntensities[count] = light.Intensity;
            _lightColors[count * 3] = light.Color.X;
            _lightColors[count * 3 + 1] = light.Color.Y;
            _lightColors[count * 3 + 2] = light.Color.Z;
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
        for (var i = count; i < 4; i++) { _lightIntensities[i] = 0; _lightTypes[i] = 0; }
        _lightCount = count;
    }

    private Camera GetCamera(World world)
    {
        var cam = new Camera(new System.Numerics.Vector3(0, 0.75f, -30),
            new System.Numerics.Vector3(0, 0.5f, 0), System.Numerics.Vector3.UnitY,
            MathF.PI / 12, 16f / 9f, 0.1f, 100f);
        world.Each((Entity e, ref Camera c) => cam = c);
        return cam;
    }

    private static OTKMatrix ToOTK(System.Numerics.Matrix4x4 m) => new(
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44);

    private static OTKVector3 ToOTK(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    private static int CreateProgram(string vs, string fs)
    {
        var vertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertex, vs);
        GL.CompileShader(vertex);
        GL.GetShader(vertex, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus == 0) throw new Exception($"VS: {GL.GetShaderInfoLog(vertex)}");

        var fragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragment, fs);
        GL.CompileShader(fragment);
        GL.GetShader(fragment, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus == 0) throw new Exception($"FS: {GL.GetShaderInfoLog(fragment)}");

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int lStatus);
        if (lStatus == 0) throw new Exception($"Link: {GL.GetProgramInfoLog(program)}");

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        return program;
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

    // Shaders (copied from Silk.NET OpenGL backend — backend-agnostic GLSL 330 core)
    private const string ShadowVertexSource = @"#version 330 core
layout(location=0) in vec3 aPos;
uniform mat4 mvp;
void main(){gl_Position=mvp*vec4(aPos,1.0);}";
    private const string ShadowFragmentSource = @"#version 330 core
void main(){}";

    private const string VertexShaderSource = @"#version 330 core
layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec4 aColor;
uniform mat4 mvp;
uniform mat4 model;
out vec3 vNormal;
out vec3 vWorldPos;
out vec4 vColor;
void main(){
    vec4 wp=model*vec4(aPos,1.0);
    vWorldPos=wp.xyz;
    vNormal=mat3(transpose(inverse(model)))*aNormal;
    vColor=aColor;
    gl_Position=mvp*vec4(aPos,1.0);
}";

    private const string FragmentShaderSource = @"#version 330 core
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
vec3 ACESFilm(vec3 x){const float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14;return clamp((x*(a*x+b))/(x*(c*x+d)+e),0.0,1.0);}
float Attenuation(float dist,float range){float r=max(range,0.001),d=max(dist,0.001);float x=d/r,x2=x*x,x4=x2*x2;return clamp(1.0/(1.0+25.0*x4),0.0,1.0)*smoothstep(1.0,0.0,x);}
float CalculateShadow(vec3 worldPos){vec4 lp=lightViewProj*vec4(worldPos,1.0);vec3 ndc=lp.xyz/lp.w;vec3 uvw=ndc*0.5+0.5;if(uvw.x<0.0||uvw.x>1.0||uvw.y<0.0||uvw.y>1.0||uvw.z>1.0)return 1.0;float bias=0.005;vec2 ts=vec2(1.0/2048.0);float s=0.0;for(int x=-1;x<=1;x++){for(int y=-1;y<=1;y++){float d=texture(shadowMap,uvw.xy+vec2(x,y)*ts).r;s+=(uvw.z-bias>d)?0.3:1.0;}}return s/9.0;}
void main(){
    vec3 normal=normalize(vNormal);
    vec3 albedo=pow(vColor.rgb*materialColor.rgb,vec3(2.2));
    vec3 viewDir=normalize(viewPos-vWorldPos);
    float rough=clamp(roughness,0.05,1.0);
    float metal=clamp(metallic,0.0,1.0);
    vec3 skyColor=ambientColor;
    vec3 groundColor=ambientColor*0.2;
    float hemisphere=0.5+0.5*normal.y;
    vec3 result=albedo*mix(groundColor,skyColor,hemisphere)*0.4;
    float shadow=1.0;
    if(lightCount>0&&lightTypes[0]==0)shadow=CalculateShadow(vWorldPos);
    vec3 F0=mix(vec3(0.04),albedo,metal);
    float shininess=mix(8.0,256.0,1.0-rough);
    for(int i=0;i<lightCount;i++){
        vec3 L;float atten=1.0;
        if(lightTypes[i]==1){vec3 toLight=lightPositions[i]-vWorldPos;float dist=length(toLight);L=toLight/max(dist,0.001);atten=Attenuation(dist,lightRanges[i]);}
        else{L=normalize(-lightDirs[i]);}
        float lightShadow=(i==0&&lightTypes[0]==0)?shadow:1.0;
        vec3 H=normalize(L+viewDir);
        float NdotL=max(dot(normal,L),0.0);
        float NdotH=max(dot(normal,H),0.0);
        float HdotV=max(dot(H,viewDir),0.0);
        float spec=pow(NdotH,shininess);
        vec3 fresnel=F0+(1.0-F0)*pow(1.0-HdotV,5.0);
        vec3 specColor=mix(fresnel,albedo*fresnel,metal);
        vec3 diffuse=albedo*lightColors[i]*NdotL*lightIntensities[i]*atten*1.5*lightShadow;
        vec3 specular=specColor*spec*lightIntensities[i]*atten*lightShadow;
        diffuse*=(1.0-fresnel*(1.0-metal*0.5));
        result+=diffuse+specular;
    }
    result=ACESFilm(result*1.2);
    result=pow(result,vec3(1.0/2.2));
    finalColor=vec4(result,1.0);
}";

    private readonly record struct GLMesh(int Vao, int IndexCount);

    private sealed class OpenTKScreenshotProvider : IScreenshotProvider
    {
        public Task<byte[]> CaptureAsync(string outputPath) => Task.FromResult(Array.Empty<byte>());
    }
}
