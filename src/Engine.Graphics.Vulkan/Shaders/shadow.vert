#version 450

layout(location = 0) in vec3 inPosition;

layout(push_constant) uniform PC {
    mat4 model;
    vec4 lightPos;
    vec4 lightColor;
    mat4 lightViewProj;
} pc;

layout(location = 0) out vec3 fragPos;

void main() {
    vec4 worldPos = pc.model * vec4(inPosition, 1.0);
    fragPos = worldPos.xyz;
    gl_Position = pc.lightViewProj * worldPos;
}
