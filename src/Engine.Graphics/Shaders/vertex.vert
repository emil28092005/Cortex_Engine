#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec3 fragNormal;
layout(location = 2) out vec3 fragWorldPos;

layout(push_constant) uniform PushConstants
{
    mat4 mvp;
    vec3 lightDirection;
    float pad1;
    vec3 lightColor;
    float pad2;
    vec3 ambientColor;
    float pad3;
} push;

void main()
{
    gl_Position = push.mvp * vec4(inPosition, 1.0);
    fragColor = inColor;
    fragNormal = inNormal;
    fragWorldPos = inPosition;
}
