// depth.vert
// Uses our custom lightSpaceMatrix instead of Raylib's mvp.
// This guarantees the depth pass and lighting pass use identical transforms.
// Raylib automatically provides matModel via DrawModelEx.
#version 330

in vec3 vertexPosition;

uniform mat4 lightSpaceMatrix;
uniform mat4 matModel;

void main()
{
    gl_Position = lightSpaceMatrix * matModel * vec4(vertexPosition, 1.0);
}
