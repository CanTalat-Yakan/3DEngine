#version 420

// Program Uniforms
layout(set=0, binding=0) uniform Uniforms {
  vec4 State;
  mat4 Transform;
  vec4 Scalar4[2];
  vec4 Vector[8];
  mat4 Clip[8];
  uint ClipSize;
};

// Uniform Accessor Functions
float Time() { return State[0]; }
float ScreenWidth() { return State[1]; }
float ScreenHeight() { return State[2]; }
float ScreenScale() { return State[3]; }
float Scalar(uint i) { if (i < 4u) return Scalar4[0][i]; else return Scalar4[1][i - 4u]; }
vec4 sRGBToLinear(vec4 val) { return vec4(val.xyz * (val.xyz * (val.xyz * 0.305306011 + 0.682171111) + 0.012522878), val.w); }

// Vertex Attributes
layout(location = 0)in vec2 in_Position;
layout(location = 1)in vec4 in_Color;
layout(location = 2)in vec2 in_TexCoord;

// Out Params
layout(location = 0)out vec4 ex_Color;
layout(location = 1)out vec2 ex_ObjectCoord;

void main(void)
{
  ex_ObjectCoord = in_TexCoord;
  gl_Position = Transform * vec4(in_Position, 0.0, 1.0);
  ex_Color = in_Color;
	#if VELDRID
	gl_Position.y = -gl_Position.y;
	#endif
}
