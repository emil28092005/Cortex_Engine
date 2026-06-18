#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(location = 0) out vec3 fragColor;

layout(row_major, set = 0, binding = 0) uniform CameraUBO {
    mat4 vp;
};

layout(push_constant) uniform PC {
    float angle;
} pc;

void main() {
    float c = cos(pc.angle);
    float s = sin(pc.angle);
    vec3 rotated = vec3(
        inPosition.x * c - inPosition.z * s,
        inPosition.y,
        inPosition.x * s + inPosition.z * c
    );
    gl_Position = vp * vec4(rotated, 1.0);
    fragColor = inColor;
}
