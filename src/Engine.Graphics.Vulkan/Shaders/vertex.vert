#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec3 fragNormal;
layout(location = 2) out vec3 fragWorldPos;
layout(location = 3) out vec2 fragUv;

struct Light
{
    vec3 direction;
    float intensity;
    vec3 color;
    float _pad;
};

layout(set = 0, binding = 0) uniform FrameConstants
{
    vec3 cameraPosition;
    uint lightCount;
    vec3 ambientColor;
    float _pad;
    Light lights[4];
} frame;

layout(push_constant) uniform PushConstants
{
    mat4 mvp;
    vec3 materialAlbedo;
    float materialRoughness;
    float materialMetallic;
    uint useTexture;
    uint textureIndex;
    uint _pad0;
    uint _pad1;
} push;

void main()
{
    gl_Position = push.mvp * vec4(inPosition, 1.0);
    fragColor = inColor;
    fragNormal = inNormal;
    fragWorldPos = inPosition;
    fragUv = inPosition.xz * 0.5 + 0.5;
}
