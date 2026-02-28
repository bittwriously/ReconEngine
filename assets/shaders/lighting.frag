// lighting.frag v2
#version 330

#define MAX_LIGHTS 32

struct Light {
	int enabled;
	int type; // 0 directional / 1 point / 2 spot
	vec3 position;
	vec3 target;
	vec4 color;
	float distance;
	float innerAngle;
	float outerAngle;
};

// poisson disk samples for QUALITY shadows :D
const vec2 poissonDisk[16] = vec2[](
    vec2(-0.94201624,  -0.39906216),
    vec2( 0.94558609,  -0.76890725),
    vec2(-0.094184101, -0.92938870),
    vec2( 0.34495938,   0.29387760),
    vec2(-0.91588581,   0.45771432),
    vec2(-0.81544232,  -0.87912464),
    vec2(-0.38277543,   0.27676845),
    vec2( 0.97484398,   0.75648379),
    vec2( 0.44323325,  -0.97511554),
    vec2( 0.53742981,  -0.47373420),
    vec2(-0.26496911,  -0.41893023),
    vec2( 0.79197514,   0.19090188),
    vec2(-0.24188840,   0.99706507),
    vec2(-0.81409955,   0.91437590),
    vec2( 0.19984126,   0.78641367),
    vec2( 0.14383161,  -0.14100790)
);

/* enum LightingDebugMode
{
    None = 0,
    Normals = 1,
    UVs = 2,
    BaseTexture = 3,
    ShadowProjectedUVs = 4,
    LightOnly = 5,
    ShadowFactor = 6,
    RawLightSpacePos = 7,
    LightSpaceW = 8,
    ShadowMapDepth = 9,
} */

in vec3 fragPos;
in vec3 fragNormal;
in vec2 fragTexCoord;
in vec4 fragPosLightSpace;

uniform sampler2D texture0;
uniform sampler2D shadowMaps[4];
uniform mat4 lightSpaceMatrices[4];
uniform vec4 cascadeSplits;
uniform vec3 viewPos;
uniform mat4 lightSpaceMatrix;
uniform Light lights[MAX_LIGHTS];
uniform vec4 ambient;
uniform vec4 colDiffuse;
uniform int debugMode;

out vec4 finalColor;

int GetCascadeIndex(vec3 fragPos)
{
    float dist = length(fragPos - viewPos);
    if (dist < cascadeSplits.x) return 0;
    if (dist < cascadeSplits.y) return 1;
    if (dist < cascadeSplits.z) return 2;
    return 3;
}

float ShadowCalculation(vec3 worldPos, vec3 norm, vec3 lightDir)
{
    float rawNdotL = dot(norm, lightDir);
    if (rawNdotL <= 0.05) return 1.0;
    float ndotl = max(rawNdotL, 0.0);

    int cascadeIndex = GetCascadeIndex(worldPos);

    float normalOffsetScale = 0.02 * (1.0 - ndotl);
    vec3 offsetPos = worldPos + norm * normalOffsetScale;

    vec4 offsetLightSpace = lightSpaceMatrices[cascadeIndex] * vec4(offsetPos, 1.0);
    vec3 projCoords = offsetLightSpace.xyz / offsetLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    if (projCoords.z > 1.0) return 0.0;
    projCoords.xy = clamp(projCoords.xy, 0.001, 0.999);

    float slopeBias = 0;//0.0005 * tan(acos(ndotl));
    slopeBias = clamp(slopeBias, 0.0, 0.002);

    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMaps[cascadeIndex], 0);
    float spread = 2.0;

    for (int i = 0; i < 16; i++)
    {
        float closestDepth = texture(shadowMaps[cascadeIndex], 
            projCoords.xy + poissonDisk[i] * texelSize * spread).r;
        shadow += (projCoords.z - slopeBias > closestDepth) ? 1.0 : 0.0;
    }

    return shadow / 16.0;
}

void main()
{
	vec3 norm = normalize(fragNormal);
	vec4 texColor = texture(texture0, fragTexCoord);
	vec3 baseColor = texColor.rgb * colDiffuse.rgb;

    if (debugMode == 1) { finalColor = vec4(norm * 0.5 + 0.5, 1.0); return; }
    if (debugMode == 2) { finalColor = vec4(fragTexCoord, 0.0, 1.0); return; }
    if (debugMode == 3) { finalColor = vec4(baseColor, 1.0); return; }
    if (debugMode == 4)
    {
        int ci = GetCascadeIndex(fragPos);
        vec3 projCoords = (lightSpaceMatrices[ci] * vec4(fragPos, 1.0)).xyz;
        projCoords = projCoords * 0.5 + 0.5;
        finalColor = vec4(projCoords.x, projCoords.y, 0.0, 1.0);
        return;
    }
    if (debugMode == 7)
    {
        int ci = GetCascadeIndex(fragPos);
        vec4 rawLS = lightSpaceMatrices[ci] * vec4(fragPos, 1.0);
        finalColor = vec4(fract(rawLS.xyz * 0.1), 1.0);
        return;
    }
    if (debugMode == 8)
    {
        int ci = GetCascadeIndex(fragPos);
        vec4 rawLS = lightSpaceMatrices[ci] * vec4(fragPos, 1.0);
        finalColor = vec4(vec3(abs(rawLS.w) * 0.1), 1.0);
        return;
    }
    if (debugMode == 9)
    {
        int ci = GetCascadeIndex(fragPos);
        vec3 projCoords = (lightSpaceMatrices[ci] * vec4(fragPos, 1.0)).xyz;
        projCoords = projCoords * 0.5 + 0.5;
        float depth = texture(shadowMaps[ci], projCoords.xy).r;
        finalColor = vec4(vec3(depth), 1.0);
        return;
    }
    if (debugMode == 10)
    {
        int ci = GetCascadeIndex(fragPos);
        if (ci == 0) finalColor = vec4(1.0, 0.0, 0.0, 1.0);
        else if (ci == 1) finalColor = vec4(0.0, 1.0, 0.0, 1.0);
        else if (ci == 2) finalColor = vec4(0.0, 0.0, 1.0, 1.0);
        else              finalColor = vec4(1.0, 1.0, 1.0, 1.0);
        return;
    }

    vec3 totalLight = ambient.rgb;
    float totalShadow = 0.0;
    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (lights[i].enabled == 0) continue;
        vec3 lightColor = lights[i].color.rgb;
        vec3 lightDir;
        float attenuation = 1.0;

        if (lights[i].type == 0)
        { // directional light
            lightDir = normalize(-lights[i].target);

            totalShadow = ShadowCalculation(fragPos, norm, lightDir);
            attenuation = 1.0 - totalShadow;
        }
        else
        {
            vec3 toLight = lights[i].position - fragPos;
            float dist = length(toLight);
            lightDir = normalize(toLight);
            if (lights[i].distance > 0.0)
            {
                float t = clamp(1.0 - (dist / lights[i].distance), 0.0, 1.0);
                attenuation = t * t;
            }
            if (lights[i].type == 2 && lights[i].outerAngle > 0.0)
            {
                vec3 spotDir = normalize(lights[i].target);
                float theta = dot(-lightDir, spotDir);
                float inner = cos(lights[i].innerAngle);
                float outer = cos(lights[i].outerAngle);
                float intensity = clamp((theta - outer) / (inner - outer + 0.0001), 0.0, 1.0);
                attenuation *= intensity;
            }
        }

        float diff = max(dot(norm, lightDir), 0.0);
        totalLight += diff * lightColor * attenuation;

        // specular (blinn phong)
        vec3 viewDir = normalize(viewPos - fragPos);
        vec3 halfDir = normalize(lightDir + viewDir);
        float spec = pow(max(dot(norm, halfDir), 0.0), 32.0);
        totalLight += spec * 0.3 * lightColor * attenuation;
    }

    if (debugMode == 5) { finalColor = vec4(totalLight, 1.0); return; }
    if (debugMode == 6) { finalColor = vec4(vec3(1.0 - totalShadow), 1.0); return; }

    vec3 result = totalLight * baseColor;
    finalColor = vec4(result, texColor.a * colDiffuse.a);
}
