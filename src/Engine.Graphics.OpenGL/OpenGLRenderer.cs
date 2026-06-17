using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Flecs.NET.Core;
using Silk.NET.OpenGL;
using EngineMaterial = Engine.Core.Components.Material;
using EngineMesh = Engine.Core.Components.Mesh;
using EngineTransform = Engine.Core.Components.Transform;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLRenderer : IRenderer
{
    private readonly GL _gl;
    private readonly uint _program;
    private readonly uint _shadowProgram;
    private readonly uint _shadowFbo;
    private readonly uint _shadowTexture;
    private const int ShadowMapSize = 2048;

    private readonly Dictionary<Entity, GLMesh> _meshCache = new();

    private readonly int _uMVP, _uModel, _uViewPos, _uMaterialColor, _uRoughness, _uMetallic;
    private readonly int _uAmbient, _uLightCount, _uLightDirs, _uLightIntensities, _uLightColors;
    private readonly int _uLightPositions, _uLightTypes, _uLightRanges;
    private readonly int _uLightViewProj, _uShadowMap, _uUseTexture;

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
    private readonly float[] _matrixBuffer = new float[16];

    public OpenGLRenderer(GL gl)
    {
        _gl = gl;
        _gl.Enable(GLEnum.DepthTest);

        _program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _shadowProgram = CreateProgram(ShadowVertexSource, ShadowFragmentSource);

        _uMVP = _gl.GetUniformLocation(_program, "mvp");
        _uModel = _gl.GetUniformLocation(_program, "model");
        _uViewPos = _gl.GetUniformLocation(_program, "viewPos");
        _uMaterialColor = _gl.GetUniformLocation(_program, "materialColor");
        _uRoughness = _gl.GetUniformLocation(_program, "roughness");
        _uMetallic = _gl.GetUniformLocation(_program, "metallic");
        _uAmbient = _gl.GetUniformLocation(_program, "ambientColor");
        _uLightCount = _gl.GetUniformLocation(_program, "lightCount");
        _uLightDirs = _gl.GetUniformLocation(_program, "lightDirs");
        _uLightIntensities = _gl.GetUniformLocation(_program, "lightIntensities");
        _uLightColors = _gl.GetUniformLocation(_program, "lightColors");
        _uLightPositions = _gl.GetUniformLocation(_program, "lightPositions");
        _uLightTypes = _gl.GetUniformLocation(_program, "lightTypes");
        _uLightRanges = _gl.GetUniformLocation(_program, "lightRanges");
        _uLightViewProj = _gl.GetUniformLocation(_program, "lightViewProj");
        _uShadowMap = _gl.GetUniformLocation(_program, "shadowMap");
        _uUseTexture = _gl.GetUniformLocation(_program, "useTexture");

        // Shadow FBO with depth texture
        _shadowFbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(GLEnum.Framebuffer, _shadowFbo);
        _shadowTexture = _gl.GenTexture();
        _gl.BindTexture(GLEnum.Texture2D, _shadowTexture);
        unsafe
        {
            _gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.DepthComponent, (uint)ShadowMapSize, (uint)ShadowMapSize, 0, GLEnum.DepthComponent, GLEnum.Float, null);
        }
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.DepthAttachment, GLEnum.Texture2D, _shadowTexture, 0);
        _gl.DrawBuffer(DrawBufferMode.None);
        _gl.ReadBuffer(ReadBufferMode.None);
        _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
    }

    public void RequestScreenshot(string outputPath) { }
    public bool IsScreenshotRequested => false;
    public IScreenshotProvider ScreenshotProvider => new OpenGLScreenshotProvider();

    public void SetScreenSize(int w, int h) { _screenWidth = w; _screenHeight = h; }

    public void RenderWorld(World world)
    {
        var camera = GetCamera(world);
        CollectLights(world);

        var hasDirLight = _lightCount > 0 && _lightTypes[0] == (int)LightType.Directional;
        Matrix4x4 lightViewProj = Matrix4x4.Identity;

        // === PASS 1: Shadow map ===
        if (hasDirLight)
        {
            var lightDir = new Vector3(_lightDirs[0], _lightDirs[1], _lightDirs[2]);
            var sceneCenter = new Vector3(0, 0.5f, 0);
            var lightPos = sceneCenter - lightDir * 30f;
            var up = MathF.Abs(Vector3.Dot(lightDir, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

            lightViewProj = Matrix4x4.CreateOrthographicOffCenter(-15, 15, -15, 15, 1, 80)
                          * Matrix4x4.CreateLookAt(lightPos, sceneCenter, up);

            _gl.Viewport(0, 0, (uint)ShadowMapSize, (uint)ShadowMapSize);
            _gl.BindFramebuffer(GLEnum.Framebuffer, _shadowFbo);
            _gl.Clear((uint)GLEnum.DepthBufferBit);
            _gl.UseProgram(_shadowProgram);
            _gl.CullFace(GLEnum.Front);

            var shadowMvpLoc = _gl.GetUniformLocation(_shadowProgram, "mvp");

            world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
            {
                if (e.Name() == "Grid" || e.Name() == "Floor") return;
                var glMesh = GetOrUploadMesh(e, mesh);
                var model = transform.GetMatrix();
                var mvp = lightViewProj * model;
                CopyMatrixToBuffer(mvp);
                unsafe
                {
                    fixed (float* p = _matrixBuffer)
                        _gl.UniformMatrix4(shadowMvpLoc, 1, false, p);
                }
                DrawMeshImmediate(glMesh);
            });

            _gl.CullFace(GLEnum.Back);
            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        }

        // === PASS 2: Main render ===
        _gl.Viewport(0, 0, (uint)_screenWidth, (uint)_screenHeight);
        _gl.Enable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.CullFace);
        _gl.ClearColor(0.098f, 0.118f, 0.157f, 1);
        _gl.Clear((uint)(GLEnum.ColorBufferBit | GLEnum.DepthBufferBit));
        _gl.UseProgram(_program);

        var view = Matrix4x4.CreateLookAt(camera.Position, camera.Target, camera.Up);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(camera.FieldOfView, camera.AspectRatio, camera.NearPlane, camera.FarPlane);

        // Frame uniforms
        _gl.Uniform3(_uViewPos, camera.Position.X, camera.Position.Y, camera.Position.Z);
        _gl.Uniform3(_uAmbient, 0.35f, 0.35f, 0.4f);
        _gl.Uniform1(_uLightCount, _lightCount);
        unsafe
        {
            fixed (float* p = _lightDirs) _gl.Uniform3(_uLightDirs, 4, p);
            fixed (float* p = _lightIntensities) _gl.Uniform1(_uLightIntensities, 4, p);
            fixed (float* p = _lightColors) _gl.Uniform3(_uLightColors, 4, p);
            fixed (float* p = _lightPositions) _gl.Uniform3(_uLightPositions, 4, p);
            fixed (int* p = _lightTypes) _gl.Uniform1(_uLightTypes, 4, p);
            fixed (float* p = _lightRanges) _gl.Uniform1(_uLightRanges, 4, p);
        }

        if (hasDirLight)
        {
            CopyMatrixToBuffer(lightViewProj);
            unsafe
            {
                fixed (float* p = _matrixBuffer)
                    _gl.UniformMatrix4(_uLightViewProj, 1, false, p);
            }
            _gl.ActiveTexture(GLEnum.Texture1);
            _gl.BindTexture(GLEnum.Texture2D, _shadowTexture);
            _gl.Uniform1(_uShadowMap, 1);
            _gl.ActiveTexture(GLEnum.Texture0);
        }

        world.Each((Entity e, ref EngineMesh mesh, ref EngineTransform transform) =>
        {
            if (e.Name() == "Grid") return;
            var material = e.Has<EngineMaterial>() ? e.Get<EngineMaterial>() : EngineMaterial.Default;
            var glMesh = GetOrUploadMesh(e, mesh);
            var model = transform.GetMatrix();
            var mvp = proj * view * model;

            CopyMatrixToBuffer(mvp);
            unsafe { fixed (float* pmvp = _matrixBuffer) _gl.UniformMatrix4(_uMVP, 1, false, pmvp); }
            CopyMatrixToBuffer(model);
            unsafe { fixed (float* pmodel = _matrixBuffer) _gl.UniformMatrix4(_uModel, 1, false, pmodel); }
            _gl.Uniform4(_uMaterialColor, material.Albedo.X, material.Albedo.Y, material.Albedo.Z, 1.0f);
            _gl.Uniform1(_uRoughness, material.Roughness);
            _gl.Uniform1(_uMetallic, material.Metallic);
            _gl.Uniform1(_uUseTexture, 0);

            DrawMeshImmediate(glMesh);
        });
    }

    private void DrawMeshImmediate(GLMesh mesh)
    {
        _gl.BindVertexArray(mesh.Vao);
        unsafe { _gl.DrawElements(GLEnum.Triangles, (uint)mesh.IndexCount, GLEnum.UnsignedInt, null); }
        _gl.BindVertexArray(0);
    }

    private GLMesh GetOrUploadMesh(Entity e, EngineMesh mesh)
    {
        if (_meshCache.TryGetValue(e, out var existing))
            return existing;

        var vao = _gl.GenVertexArray();
        _gl.BindVertexArray(vao);

        // Position (location 0)
        var positions = new float[mesh.Vertices.Length * 3];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            positions[i * 3] = mesh.Vertices[i].Position.X;
            positions[i * 3 + 1] = mesh.Vertices[i].Position.Y;
            positions[i * 3 + 2] = mesh.Vertices[i].Position.Z;
        }
        var posVbo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ArrayBuffer, posVbo);
        UploadBufferData(positions);
        _gl.EnableVertexAttribArray(0);
        unsafe { _gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 3 * sizeof(float), (void*)0); }

        // Normal (location 1)
        var normals = new float[mesh.Vertices.Length * 3];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            normals[i * 3] = mesh.Vertices[i].Normal.X;
            normals[i * 3 + 1] = mesh.Vertices[i].Normal.Y;
            normals[i * 3 + 2] = mesh.Vertices[i].Normal.Z;
        }
        var nrmVbo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ArrayBuffer, nrmVbo);
        UploadBufferData(normals);
        _gl.EnableVertexAttribArray(1);
        unsafe { _gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 3 * sizeof(float), (void*)0); }

        // Color (location 2)
        var colors = new float[mesh.Vertices.Length * 4];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            colors[i * 4] = mesh.Vertices[i].Color.X;
            colors[i * 4 + 1] = mesh.Vertices[i].Color.Y;
            colors[i * 4 + 2] = mesh.Vertices[i].Color.Z;
            colors[i * 4 + 3] = 1.0f;
        }
        var colVbo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ArrayBuffer, colVbo);
        UploadBufferData(colors);
        _gl.EnableVertexAttribArray(2);
        unsafe { _gl.VertexAttribPointer(2, 4, GLEnum.Float, false, 4 * sizeof(float), (void*)0); }

        // Indices — use pinned GCHandle
        var ebo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
        var indexHandle = System.Runtime.InteropServices.GCHandle.Alloc(mesh.Indices, System.Runtime.InteropServices.GCHandleType.Pinned);
        unsafe
        {
            _gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(mesh.Indices.Length * sizeof(uint)), (void*)indexHandle.AddrOfPinnedObject(), GLEnum.StaticDraw);
        }
        indexHandle.Free();

        _gl.BindVertexArray(0);

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
        var cam = new Camera(new Vector3(0, 0.75f, -30), new Vector3(0, 0.5f, 0), Vector3.UnitY, MathF.PI / 12, 16f / 9f, 0.1f, 100f);
        world.Each((Entity e, ref Camera c) => cam = c);
        return cam;
    }

    private uint CreateProgram(string vs, string fs)
    {
        var vertex = _gl.CreateShader(GLEnum.VertexShader);
        _gl.ShaderSource(vertex, vs);
        _gl.CompileShader(vertex);
        _gl.GetShader(vertex, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus == 0) throw new Exception($"VS: {_gl.GetShaderInfoLog(vertex)}");

        var fragment = _gl.CreateShader(GLEnum.FragmentShader);
        _gl.ShaderSource(fragment, fs);
        _gl.CompileShader(fragment);
        _gl.GetShader(fragment, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus == 0) throw new Exception($"FS: {_gl.GetShaderInfoLog(fragment)}");

        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus == 0) throw new Exception($"Link: {_gl.GetProgramInfoLog(program)}");

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        return program;
    }

    private unsafe void UploadBufferData(float[] data)
    {
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            _gl.BufferData(GLEnum.ArrayBuffer, (nuint)(data.Length * sizeof(float)), (void*)handle.AddrOfPinnedObject(), GLEnum.StaticDraw);
        }
        finally
        {
            handle.Free();
        }
    }

    private void CopyMatrixToBuffer(Matrix4x4 m)
    {
        // OpenGL expects column-major; System.Numerics is row-major, so transpose during copy
        _matrixBuffer[0] = m.M11; _matrixBuffer[1] = m.M21; _matrixBuffer[2] = m.M31; _matrixBuffer[3] = m.M41;
        _matrixBuffer[4] = m.M12; _matrixBuffer[5] = m.M22; _matrixBuffer[6] = m.M32; _matrixBuffer[7] = m.M42;
        _matrixBuffer[8] = m.M13; _matrixBuffer[9] = m.M23; _matrixBuffer[10] = m.M33; _matrixBuffer[11] = m.M43;
        _matrixBuffer[12] = m.M14; _matrixBuffer[13] = m.M24; _matrixBuffer[14] = m.M34; _matrixBuffer[15] = m.M44;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var m in _meshCache.Values) _gl.DeleteVertexArray(m.Vao);
        _meshCache.Clear();
        _gl.DeleteProgram(_program);
        _gl.DeleteProgram(_shadowProgram);
        _gl.DeleteFramebuffer(_shadowFbo);
        _gl.DeleteTexture(_shadowTexture);
    }

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

vec3 ACESFilm(vec3 x){
    const float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e),0.0,1.0);
}
float Attenuation(float dist,float range){
    float r=max(range,0.001),d=max(dist,0.001);
    float x=d/r,x2=x*x,x4=x2*x2;
    return clamp(1.0/(1.0+25.0*x4),0.0,1.0)*smoothstep(1.0,0.0,x);
}
float CalculateShadow(vec3 worldPos){
    vec4 lp=lightViewProj*vec4(worldPos,1.0);
    vec3 ndc=lp.xyz/lp.w;
    vec3 uvw=ndc*0.5+0.5;
    if(uvw.x<0.0||uvw.x>1.0||uvw.y<0.0||uvw.y>1.0||uvw.z>1.0) return 1.0;
    float bias=0.005;
    vec2 ts=vec2(1.0/2048.0);
    float s=0.0;
    for(int x=-1;x<=1;x++){
        for(int y=-1;y<=1;y++){
            float d=texture(shadowMap,uvw.xy+vec2(x,y)*ts).r;
            s+=(uvw.z-bias>d)?0.3:1.0;
        }
    }
    return s/9.0;
}
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
    if(lightCount>0&&lightTypes[0]==0) shadow=CalculateShadow(vWorldPos);
    vec3 F0=mix(vec3(0.04),albedo,metal);
    float shininess=mix(8.0,256.0,1.0-rough);
    for(int i=0;i<lightCount;i++){
        vec3 L; float atten=1.0;
        if(lightTypes[i]==1){
            vec3 toLight=lightPositions[i]-vWorldPos;
            float dist=length(toLight);
            L=toLight/max(dist,0.001);
            atten=Attenuation(dist,lightRanges[i]);
        } else { L=normalize(-lightDirs[i]); }
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

    private readonly record struct GLMesh(uint Vao, int IndexCount);

    private sealed class OpenGLScreenshotProvider : IScreenshotProvider
    {
        public Task<byte[]> CaptureAsync(string outputPath) => Task.FromResult(Array.Empty<byte>());
    }
}
