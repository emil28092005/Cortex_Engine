#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(location = 0) out vec3 fragWorldPos;
layout(location = 1) out vec3 fragNormal;
layout(location = 2) out vec3 fragAlbedo;

struct LightData {
    vec4 posAndIntensity;   // xyz = position, w = intensity
    vec4 colorAndRange;     // xyz = color, w = range
};

layout(set = 0, binding = 0) uniform SceneUBO {
    mat4 vp;
    int numLights;
    int numShadowLights;
    vec2 padding;
    LightData lights[8];
    vec4 shadowParams[4];   // x=bias, y=sampleRadius, z=farPlane, w=unused
} scene;

layout(push_constant) uniform PC {
    mat4 model;
} pc;

void main() {
    vec4 worldPos = pc.model * vec4(inPosition, 1.0);
    gl_Position = scene.vp * worldPos;
    fragWorldPos = worldPos.xyz;
    fragNormal = mat3(pc.model) * inNormal;
    fragAlbedo = inColor;
}
