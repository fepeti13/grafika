#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec3 vNormal;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uSliceRotation;

// Lighting uniforms
uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform float uShininess;
uniform vec3 uAmbientStrength;
uniform vec3 uDiffuseStrength;
uniform vec3 uSpecularStrength;

out vec4 outCol;

void main()
{
    outCol = vCol;
    vec4 worldPos = uSliceRotation * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
    vec3 worldNormal = normalize(uNormal * vNormal);
    vec3 worldPosition = vec3(worldPos);
    
    // Ambient component
    vec3 ambient = uAmbientStrength * uLightColor;

    // Diffuse component
    vec3 norm = normalize(worldNormal);
    vec3 lightDir = normalize(uLightPos - worldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor * uDiffuseStrength;

    // Specular component
    vec3 viewDir = normalize(uViewPos - worldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    vec3 specular = uSpecularStrength * spec * uLightColor;

    // Combine all lighting components
    vec3 result = (ambient + diffuse + specular) * outCol.rgb;
    outCol = vec4(result, outCol.w);

    gl_Position = uProjection * uView * worldPos;
} 