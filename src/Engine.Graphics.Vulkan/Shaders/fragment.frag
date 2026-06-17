#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragWorldPos;
layout(location = 3) in vec3 fragViewDir;

layout(row_major, set = 0, binding = 0) uniform FrameUBO {
    vec3 cameraPosition;
    uint lightCount;
    vec3 ambientColor;
    float pad0;
    vec4 lightData[16];
} frame;

layout(push_constant) uniform PushConstants {
    mat4 mvp;
    mat4 model;
    vec4 material;
} pc;

layout(location = 0) out vec4 outColor;

vec3 ACESFilm(vec3 x) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
    vec3 albedo = fragColor * pc.material.rgb;
    float roughness = clamp(pc.material.a, 0.05, 1.0);

    vec3 N = normalize(fragNormal);
    vec3 V = normalize(fragViewDir);

    vec3 finalColor = frame.ambientColor * albedo;

    for (uint i = 0u; i < frame.lightCount && i < 16u; i++) {
        vec4 dirIntensity = frame.lightData[i * 2];
        vec4 colorRange = frame.lightData[i * 2 + 1];

        vec3 lightDir;
        float attenuation;

        if (dirIntensity.w < 0.0) {
            // Directional light: direction stored as xyz, w = intensity (negative marks directional)
            lightDir = normalize(-dirIntensity.xyz);
            attenuation = abs(dirIntensity.w);
        } else {
            // Point light: position stored as xyz, w = intensity (positive marks point)
            vec3 toLight = dirIntensity.xyz - fragWorldPos;
            float dist = length(toLight);
            lightDir = toLight / max(dist, 0.001);
            float range = max(colorRange.w, 0.001);
            attenuation = dirIntensity.w * max(0.0, 1.0 - dist / range);
            attenuation /= max(dist * dist * 0.01, 0.01);
        }

        vec3 H = normalize(V + lightDir);
        float NdotL = max(dot(N, lightDir), 0.0);
        float NdotH = max(dot(N, H), 0.0);
        float VdotH = max(dot(V, H), 0.0);

        float specPower = mix(128.0, 4.0, roughness);
        float specIntensity = pow(NdotH, specPower);

        vec3 specular = vec3(specIntensity) * colorRange.rgb;
        vec3 diffuse = albedo * NdotL * colorRange.rgb;

        finalColor += (diffuse + specular) * attenuation;
    }

    finalColor = ACESFilm(finalColor);
    finalColor = pow(finalColor, vec3(1.0 / 2.2));

    outColor = vec4(finalColor, 1.0);
}
