#version 330

in vec3 fragPos;

uniform sampler2D envMap;
uniform bool vFlipped;
uniform bool doGamma;

out vec4 finalColor;

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= vec2(0.1591, 0.3183);
    uv += 0.5;
    return uv;
}

void main()
{
    vec3 dir = vFlipped 
            ? vec3(fragPos.x, -fragPos.y, fragPos.z) 
            : fragPos;
    vec2 uv = SampleSphericalMap(normalize(dir));
    vec3 color = texture(envMap, uv).rgb;

    if (doGamma)
    {
        color = color / (color + vec3(1.0));
        color = pow(color, vec3(1.0 / 2.2));
    }

    finalColor = vec4(color, 1.0);
}
