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
    // Debug: just output the light position as color
    vec3 lp = pointLightPos.xyz;
    float intensity = pointLightPos.w;
    
    // Map position to visible range
    vec3 debugColor = lp * 0.1 + vec3(0.5);
    outColor = vec4(debugColor * fragAlbedo, 1.0);
}
