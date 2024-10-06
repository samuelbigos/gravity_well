#[compute]
#version 450

layout(set = 0, binding = 0, rgba8) uniform image2D _input;
layout(set = 1, binding = 0, rgba8) uniform image2D _output;
layout(set = 2, binding = 0, rgba8) uniform image2D _worldImage;

layout(push_constant, std430) uniform Params {
	float time;
} params;

layout(local_size_x = 32, local_size_y = 32, local_size_z = 1) in;
void main() 
{
	ivec2 uv;
	uv.x = int(gl_WorkGroupID.x) * int(gl_WorkGroupSize.x) + int(gl_LocalInvocationID.x);
	uv.y = int(gl_WorkGroupID.y) * int(gl_WorkGroupSize.y) + int(gl_LocalInvocationID.y);

	float val = imageLoad(_worldImage, uv).r;

	// vec2 centre = vec2(256, 256);
	// float dist = length(centre - uv);
	// if (dist < 32.0)
	// {
	// 	imageStore(_output, uv, vec4(sin(params.time), sin(params.time + 3.14 * 0.66), sin(params.time + 3.14 * 1.33), 1.0));
	// }
	// else
	{
		imageStore(_output, uv, vec4(val, val, val, 1.0));
	}	
}
