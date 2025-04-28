#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec3 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uSideColor;
uniform float uIsCenter;

out vec4 outCol;
out vec3 outNormal;
out vec3 outWorldPosition;

void main()
{
    if (uIsCenter > 0.5 && dot(vNormal, vec3(0.0, 1.0, 0.0)) > 0.9)
    {
        outCol = vec4(uSideColor, 1.0); // side color for top faces of center cube only
    }
    else
    {
        outCol = vCol; // sefault for other faces
    }

    outNormal = uNormal * vNormal;
    outWorldPosition = vec3(uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0));
    gl_Position = uProjection * uView * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
}