﻿#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform float uShininess;
uniform vec3 uAmbientStrength;
uniform vec3 uDiffuseStrength;
uniform vec3 uSpecularStrength;

in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;

void main()
{
    // Ambient component
    vec3 ambient = uAmbientStrength * uLightColor;

    // Diffuse component
    vec3 norm = normalize(outNormal);
    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor * uDiffuseStrength;

    // Specular component
    vec3 viewDir = normalize(uViewPos - outWorldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    vec3 specular = uSpecularStrength * spec * uLightColor;

    // Combining all lighting components
    vec3 result = (ambient + diffuse + specular) * outCol.rgb;

    FragColor = vec4(result, outCol.w);
}
