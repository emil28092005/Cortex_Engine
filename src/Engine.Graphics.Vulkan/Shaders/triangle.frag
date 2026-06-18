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
    vec3 N = normalize(fragNormal);
    vec3 toLight = pointLightPos.xyz - fragWorldPos;
    float dist = length(toLight);
    vec3 L = normalize(toLight);

    float NdotL = max(dot(N, L), 0.0);
    float atten = 1.0 / (1.0 + dist * dist * 0.1);
    float intensity = pointLightPos.w;

    vec3 color = fragAlbedo * pointLightColor.xyz * NdotL * intensity * atten;

    outColor = vec4(color, 1.0);
}
