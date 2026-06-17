#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

layout(set = 0, binding = 0) uniform FrameUBO {
    vec3 cameraPosition;
    uint lightCount;
    vec3 ambientColor;
    float pad0;
    vec4 lightData[16];
} frame;

layout(push_constant) uniform PushConstants {
    mat4 mvp;
    mat4 model;
    vec4 material;
} pc;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec3 fragNormal;
layout(location = 2) out vec3 fragWorldPos;
layout(location = 3) out vec3 fragViewDir;

void main() {
    vec4 worldPos = pc.model * vec4(inPosition, 1.0);
    gl_Position = pc.mvp * vec4(inPosition, 1.0);

    // Vulkan clip space: Y-down, Z [0,1] — convert from OpenGL Y-up, Z [-1,1]
    gl_Position.y = -gl_Position.y;
    gl_Position.z = (gl_Position.z + gl_Position.w) * 0.5;

    fragColor = inColor;
    fragNormal = normalize(mat3(pc.model) * inNormal);
    fragWorldPos = worldPos.xyz;
    fragViewDir = normalize(frame.cameraPosition - worldPos.xyz);
}
