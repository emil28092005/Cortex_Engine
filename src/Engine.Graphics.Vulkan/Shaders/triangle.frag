#version 450

layout(location = 0) in vec3 fragWorldPos;
layout(location = 1) in vec3 fragNormal;
layout(location = 2) in vec3 fragAlbedo;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 0) uniform CameraUBO {
    mat4 vp;
};

layout(set = 0, binding = 1) uniform samplerCube shadowCube;

layout(push_constant) uniform PC {
    mat4 model;
    vec4 lightPos;
    vec4 lightColor;
    mat4 lightViewProj;
} pc;

const vec3 AMBIENT = vec3(0.01, 0.01, 0.02);
const float PI = 3.14159265359;
const float FAR_PLANE = 60.0;

// 16-tap Poisson disk samples (normalized offsets on unit sphere tangent)
const vec3 POISSON_DISK[16] = vec3[16](
    vec3( 0.0000,  0.0000,  0.0000),
    vec3( 0.1376,  0.0000,  0.0000),
    vec3( 0.0971,  0.0971,  0.0000),
    vec3( 0.0000,  0.1376,  0.0000),
    vec3(-0.0971,  0.0971,  0.0000),
    vec3(-0.1376,  0.0000,  0.0000),
    vec3(-0.0971, -0.0971,  0.0000),
    vec3( 0.0000, -0.1376,  0.0000),
    vec3( 0.0971, -0.0971,  0.0000),
    vec3( 0.2693,  0.0000,  0.0000),
    vec3( 0.1903,  0.1903,  0.0000),
    vec3( 0.0000,  0.2693,  0.0000),
    vec3(-0.1903,  0.1903,  0.0000),
    vec3(-0.2693,  0.0000,  0.0000),
    vec3(-0.1903, -0.1903,  0.0000),
    vec3( 0.1903, -0.1903,  0.0000)
);

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

// Build a tangent-space basis for the sampling direction
mat3 buildTangentBasis(vec3 dir)
{
    vec3 absDir = abs(dir);
    vec3 up = absDir.x < 0.999 ? vec3(1, 0, 0) : vec3(0, 1, 0);
    vec3 tangent = normalize(cross(up, dir));
    vec3 bitangent = cross(dir, tangent);
    return mat3(tangent, bitangent, dir);
}

float calcShadow(vec3 worldPos, vec3 lightPos, vec3 N, vec3 L)
{
    vec3 dir = worldPos - lightPos;
    float dist = length(dir);
    vec3 dirNorm = normalize(dir);

    // Base shadow check (center sample)
    float closestDepth = texture(shadowCube, dirNorm).r;
    float mappedDepth = closestDepth * FAR_PLANE;
    float bias = 0.3 + 0.5 * (1.0 - max(dot(N, L), 0.0));
    float baseResult = dist - bias < mappedDepth ? 1.0 : 0.0;

    // If clearly lit or clearly shadowed, skip PCF
    if (dist - bias * 3.0 < mappedDepth) return 1.0;
    if (dist + bias * 3.0 > mappedDepth && baseResult > 0.5) return 1.0;

    // Poisson disk PCF — 16 samples
    float shadow = 0.0;
    mat3 basis = buildTangentBasis(dirNorm);

    // Sample radius scales with distance — softer shadows further from light
    float sampleRadius = 0.02 * (dist / 20.0);
    sampleRadius = clamp(sampleRadius, 0.005, 0.08);

    for (int i = 0; i < 16; i++)
    {
        vec3 sampleDir = normalize(dirNorm + basis * (POISSON_DISK[i] * sampleRadius));
        float sampleDepth = texture(shadowCube, sampleDir).r;
        float sampleMapped = sampleDepth * FAR_PLANE;
        shadow += (dist - bias < sampleMapped) ? 1.0 : 0.0;
    }
    shadow /= 16.0;

    return shadow;
}

void main()
{
    vec3 N = normalize(fragNormal);
    vec3 V = normalize(-fragWorldPos);
    vec3 albedo = fragAlbedo;
    float roughness = 0.5;
    float metallic = 0.1;

    vec3 lightPos = pc.lightPos.xyz;
    float lightIntensity = pc.lightPos.w;
    vec3 lightColor = pc.lightColor.xyz;
    float lightRange = pc.lightColor.w;

    vec3 toLight = lightPos - fragWorldPos;
    float dist = length(toLight);
    vec3 L = toLight / max(dist, 0.001);

    float attenuation = pow(clamp(1.0 - dist / max(lightRange, 0.001), 0.0, 1.0), 2.0);
    vec3 radiance = lightColor * lightIntensity * attenuation;

    float shadow = calcShadow(fragWorldPos, lightPos, N, L);

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
    vec3 lighting = (kD * albedo / PI + specular) * radiance * NdotL * shadow;

    vec3 color = AMBIENT * albedo + lighting;

    color = acesTonemap(color);
    color = pow(color, vec3(1.0 / 2.2));

    outColor = vec4(color, 1.0);
}
