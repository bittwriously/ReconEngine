#version 330

in vec3 fragPosition;
in vec2 fragTexCoord;
in vec3 fragNormal;

uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 viewPos;
uniform vec4 ambient;

#define MAX_LIGHTS 4
#define LIGHT_DIRECTIONAL 0
#define LIGHT_POINT       1
#define LIGHT_SPOT        2

struct Light {
    int   enabled;
    int   type;
    vec3  position;
    vec3  target;
    vec4  color;
    float distance;
    float innerAngle;
    float outerAngle;
};

uniform Light lights[MAX_LIGHTS];

out vec4 finalColor;

void main()
{
    vec4 texel   = texture(texture0, fragTexCoord) * colDiffuse;
    vec3 normal  = normalize(fragNormal);
    vec3 viewDir = normalize(viewPos - fragPosition);

    vec3 diffuseAccum  = vec3(0.0);
    vec3 specularAccum = vec3(0.0);

    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (lights[i].enabled != 1) continue;

        vec3  lightDir;
        float attenuation = 1.0;

        if (lights[i].type == LIGHT_DIRECTIONAL)
        {
            lightDir = normalize(lights[i].target);
        }
        else if (lights[i].type == LIGHT_POINT)
        {
            vec3  toLight    = lights[i].position - fragPosition;
            float dist       = length(toLight);
            lightDir         = toLight / dist;

            float range      = max(lights[i].distance, 0.001);
            float normalDist = dist / range;
            attenuation      = clamp(1.0 - normalDist * normalDist, 0.0, 1.0);
            attenuation     *= attenuation;
        }
        else if (lights[i].type == LIGHT_SPOT)
        {
            vec3  toLight    = lights[i].position - fragPosition;
            float dist       = length(toLight);
            lightDir         = toLight / dist;

            float range      = max(lights[i].distance, 0.001);
            float normalDist = dist / range;
            float distAtten  = clamp(1.0 - normalDist * normalDist, 0.0, 1.0);
            distAtten       *= distAtten;

            vec3  spotDir    = normalize(-lights[i].target);
            float cosAngle   = dot(spotDir, lightDir);
            float cosInner   = cos(lights[i].innerAngle);
            float cosOuter   = cos(lights[i].outerAngle);

            float spotAtten  = smoothstep(cosOuter, cosInner, cosAngle);

            attenuation = distAtten * spotAtten;
        }

        vec3 lightColor = lights[i].color.rgb * attenuation;
        float NdotL = max(dot(normal, lightDir), 0.0);
        diffuseAccum += lightColor * NdotL;

        // blinn-phong specular
        if (NdotL > 0.0)
        {
            vec3  halfDir = normalize(lightDir + viewDir);
            float spec    = pow(max(dot(normal, halfDir), 0.0), 32.0);
            specularAccum += lightColor * spec * 0.5;
        }
    }

    vec3 ambientColor = ambient.rgb * texel.rgb;
    vec3 result       = ambientColor + (diffuseAccum + specularAccum) * texel.rgb;

    result     = pow(result, vec3(1.0 / 2.2));
    finalColor = vec4(result, texel.a);
}