#[compute]
#version 450

layout(set = 0, binding = 0, rgba8) uniform image2D _input;
layout(set = 1, binding = 0, rgba8) uniform image2D _output;
layout(set = 2, binding = 0, rgba8) uniform image2D _worldImage;

ivec2 dirs[] = {
	ivec2(0, -1),
	ivec2(-1, 0),
	ivec2(1, 0),
	ivec2(0, 1),
};

layout(local_size_x = 32, local_size_y = 32, local_size_z = 1) in;
void main() 
{
	ivec2 uv;
	uv.x = int(gl_WorkGroupID.x) * int(gl_WorkGroupSize.x) + int(gl_LocalInvocationID.x);
	uv.y = int(gl_WorkGroupID.y) * int(gl_WorkGroupSize.y) + int(gl_LocalInvocationID.y);

	float val = imageLoad(_worldImage, uv).r;

	// check all surrounding pixels, if no support, move this pixel down.
	bool connected = false;
	for (int i = 0; i < 8; i++)
	{
		if (imageLoad(_worldImage, uv + dirs[i]).r > 0.5)
		{
			connected = true;
			break;
		}
	}

	// if (!connected)
	// {
	// 	imageStore(_output, uv, vec4(val, val, val, 1.0));
	// }

	imageStore(_output, uv, vec4(val, val, val, 1.0));
}
