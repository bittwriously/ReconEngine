#version 330

in vec3 fragPos;

uniform samplerCube envMap;
uniform bool vFlipped;
uniform bool doGamma;

out vec4 finalColor;

void main()
{
    vec3 color;
    if (vFlipped) color = texture(envMap, vec3(fragPos.x, -fragPos.y, fragPos.z)).rgb;
    else color = texture(envMap, fragPos).rgb;

    if (doGamma)
    {
        color = color / (color + vec3(1.0));
        color = pow(color, vec3(1.0/2.2));
    }

    finalColor = vec4(color, 1.0);
}
