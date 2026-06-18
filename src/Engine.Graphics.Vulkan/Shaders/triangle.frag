#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec3 fragNormal;
layout(location = 0) out vec4 outColor;

void main() {
    vec3 N = normalize(fragNormal);
    vec3 L = normalize(vec3(0.5, 0.8, 0.3));
    float diff = max(dot(N, L), 0.0);
    float ambient = 0.25;
    vec3 color = fragColor * (ambient + diff * 0.75);
    outColor = vec4(color, 1.0);
}
