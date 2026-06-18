#version 450

layout(location = 0) in vec3 fragWorldPos;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragAlbedo;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 0) uniform CameraUBO {
    mat4 vp;
    vec4 pointLightPos;     // xyz = position, w = intensity
    vec4 pointLightColor;   // xyz = color, w = range
};

const vec3 DIR_LIGHT_DIR = normalize(vec3(0.5, 0.8, 0.3));
const vec3 DIR_LIGHT_COLOR = vec3(0.4, 0.38, 0.33);
const vec3 AMBIENT = vec3(0.08, 0.09, 0.12);

const float PI = 3.14159265359;

float distributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float num = a2;
    float denom = NdotH2 * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;
    return num / denom;
}

float geometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotV / (NdotV * (1.0 - k) + k);
}

float geometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return geometrySchlickGGX(NdotV, roughness) * geometrySchlickGGX(NdotL, roughness);
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 acesTonemap(vec3 color)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0, 1.0);
}

vec3 calcLight(vec3 N, vec3 V, vec3 L, vec3 radiance, vec3 albedo, float roughness, float metallic)
{
    vec3 H = normalize(V + L);
    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    float NDF = distributionGGX(N, H, roughness);
    float G = geometrySmith(N, V, L, roughness);
    vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
    vec3 specular = numerator / denominator;

    vec3 kS = F;
    vec3 kD = (vec3(1.0) - kS) * (1.0 - metallic);

    float NdotL = max(dot(N, L), 0.0);
    return (kD * albedo / PI + specular) * radiance * NdotL;
}

void main()
{
    vec3 N = normalize(fragNormal);
    vec3 V = normalize(-fragWorldPos);

    vec3 albedo = fragAlbedo;
    float roughness = 0.5;
    float metallic = 0.1;

    // Directional light (sun)
    vec3 color = calcLight(N, V, DIR_LIGHT_DIR, DIR_LIGHT_COLOR, albedo, roughness, metallic);

    // Point light
    vec3 lightPos = pointLightPos.xyz;
    float lightIntensity = pointLightPos.w;
    vec3 lightColor = pointLightColor.xyz;
    float lightRange = pointLightColor.w;

    vec3 toLight = lightPos - fragWorldPos;
    float dist = length(toLight);
    vec3 L = toLight / max(dist, 0.001);

    // Unity-style attenuation: smooth falloff at range edge
    float attenuation = pow(clamp(1.0 - dist / lightRange, 0.0, 1.0), 2.0);
    vec3 radiance = lightColor * lightIntensity * attenuation;

    if (lightIntensity > 0.0 && lightRange > 0.0)
    {
        color += calcLight(N, V, L, radiance, albedo, roughness, metallic);
    }

    // Ambient
    color += AMBIENT * albedo;

    // Tonemap + gamma
    color = acesTonemap(color);
    color = pow(color, vec3(1.0 / 2.2));

    outColor = vec4(color, 1.0);
}
