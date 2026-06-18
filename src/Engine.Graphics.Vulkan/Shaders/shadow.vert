#version 450

layout(location = 0) in vec3 inPosition;

layout(push_constant) uniform PC {
    mat4 model;
    vec4 lightPos;
    vec4 lightColor;
    mat4 lightViewProj;
} pc;

void main() {
    gl_Position = pc.lightViewProj * pc.model * vec4(inPosition, 1.0);
}
