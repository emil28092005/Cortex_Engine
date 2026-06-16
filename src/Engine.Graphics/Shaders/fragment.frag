#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragWorldPos;

layout(location = 0) out vec4 outColor;

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
    vec3 normal = normalize(fragNormal);
    vec3 lightDir = normalize(-push.lightDirection);

    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = push.lightColor * diff;
    vec3 ambient = push.ambientColor;

    vec3 result = (ambient + diffuse) * fragColor;
    outColor = vec4(result, 1.0);
}
