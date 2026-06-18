#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(location = 0) out vec3 fragColor;

layout(row_major, set = 0, binding = 0) uniform CameraUBO {
    mat4 vp;
};

layout(row_major, push_constant) uniform PC {
    mat4 model;
} pc;

void main() {
    gl_Position = vp * pc.model * vec4(inPosition, 1.0);
    fragColor = inColor;
}
