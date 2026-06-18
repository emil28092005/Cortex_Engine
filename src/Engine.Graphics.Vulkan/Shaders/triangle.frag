#version 450

layout(location = 0) in vec3 fragWorldPos;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragAlbedo;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 0) uniform CameraUBO {
    mat4 vp;
};

void main()
{
    vec3 N = normalize(fragNormal);
    
    // Hardcoded light at (0, 10, 0) — test if lighting works at all
    vec3 lightPos = vec3(0.0, 10.0, 0.0);
    vec3 toLight = lightPos - fragWorldPos;
    float dist = length(toLight);
    vec3 L = normalize(toLight);

    float NdotL = max(dot(N, L), 0.0);
    float atten = 1.0 / (1.0 + dist * dist * 0.05);
    float intensity = 30.0;

    vec3 color = fragAlbedo * vec3(1.0, 0.9, 0.7) * NdotL * intensity * atten;
    color += fragAlbedo * 0.02;

    outColor = vec4(color, 1.0);
}
