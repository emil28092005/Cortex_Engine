#version 450

layout(location = 0) in vec3 inPosition;

layout(push_constant) uniform PC {
    mat4 model;
    mat4 lightViewProj;
    vec4 lightPos;
    vec4 shadowParams;
} pc;

layout(location = 0) out vec3 fragPos;

void main() {
    vec4 worldPos = pc.model * vec4(inPosition, 1.0);
    fragPos = worldPos.xyz;
    gl_Position = pc.lightViewProj * worldPos;
}
