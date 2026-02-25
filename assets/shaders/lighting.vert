// lighting.vert
#version 330

in vec3 vertexPosition;
in vec3 vertexNormal;
in vec2 vertexTexCoord;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 lightSpaceMatrix;

out vec3 fragPos;
out vec3 fragNormal;
out vec2 fragTexCoord;
out vec4 fragPosLightSpace;

void main()
{
    fragPos = vec3(matModel * vec4(vertexPosition, 1.0));
    fragNormal  = mat3(transpose(inverse(matModel))) * vertexNormal;
    fragTexCoord = vertexTexCoord;

    fragPosLightSpace = lightSpaceMatrix * vec4(fragPos, 1.0);

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
