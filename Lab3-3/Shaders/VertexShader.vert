#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec3 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uSliceRotation;

out vec4 outCol;
out vec3 outNormal;
out vec3 outWorldPosition;
        
void main()
{
    outCol = vCol;
    vec4 worldPos = uSliceRotation * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
    outWorldPosition = vec3(worldPos);
    outNormal = uNormal * vNormal;
    gl_Position = uProjection * uView * worldPos;
}