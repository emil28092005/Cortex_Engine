#version 450

layout(location = 0) in vec3 fragWorldPos;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragAlbedo;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 0) uniform CameraUBO {
    mat4 vp;
    vec4 pointLightPos;
    vec4 pointLightColor;
};

void main()
{
    // Just output albedo — tests if vp matrix in 128B UBO works
    outColor = vec4(fragAlbedo, 1.0);
}
