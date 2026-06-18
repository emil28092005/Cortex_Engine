#version 450

layout(location = 0) in vec3 fragPos;
layout(location = 0) out float outDepth;

layout(push_constant) uniform PC {
    mat4 model;
    vec4 lightPos;
    vec4 lightColor;
    mat4 lightViewProj;
    vec4 shadowParams;
} pc;

void main() {
    vec3 toLight = fragPos - pc.lightPos.xyz;
    float dist = length(toLight);
    outDepth = dist / pc.shadowParams.z;
}
