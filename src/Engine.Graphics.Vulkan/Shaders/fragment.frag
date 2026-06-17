#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragWorldPos;
layout(location = 3) in vec2 fragUv;

layout(location = 0) out vec4 outColor;

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

layout(set = 1, binding = 0) uniform sampler2D albedoTexture;

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
    vec3 normal = normalize(fragNormal);
    vec3 viewDir = normalize(frame.cameraPosition - fragWorldPos);
    vec3 albedo = fragColor * push.materialAlbedo;
    if (push.useTexture != 0u)
    {
        albedo *= texture(albedoTexture, fragUv).rgb;
    }
    float roughness = clamp(push.materialRoughness, 0.05, 1.0);
    float metallic = clamp(push.materialMetallic, 0.0, 1.0);

    vec3 result = frame.ambientColor * albedo;

    for (uint i = 0u; i < frame.lightCount; i++)
    {
        vec3 lightDir = normalize(-frame.lights[i].direction);
        vec3 halfDir = normalize(lightDir + viewDir);
        float diff = max(dot(normal, lightDir), 0.0);
        float spec = pow(max(dot(normal, halfDir), 0.0), mix(8.0, 128.0, 1.0 - roughness)) * mix(0.5, 1.0, metallic);

        vec3 diffuse = frame.lights[i].color * diff * frame.lights[i].intensity;
        vec3 specular = frame.lights[i].color * spec * frame.lights[i].intensity;

        result += diffuse * albedo + specular;
    }

    outColor = vec4(result, 1.0);
}
