shader_type canvas_item;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	COLOR = texture(TEXTURE, UV);

	vec2 coord = UV * 512.0;	
	vec2 centre = vec2(256, 256);
	vec2 toCentre = centre - coord;
	float dist = length(toCentre);
	if (dist < 32.0)
	{
		COLOR = vec4(0.0, 0.0, 0.0, 1.0);
	}
	if (dist < 30.0)
	{
		float pi = 3.1415926;
		dist /= 10.0;
		float time = TIME * dist;
		float angle = atan(toCentre.x, toCentre.y);

		float r = sin(mod(cos(TIME) + dist * 0.5 + angle * 2.0, pi));
		float g = sin(mod(sin(TIME) + dist + angle, pi));
		float b = sin(mod(cos(TIME) + dist * 2.0 + angle * 0.5, pi));
		COLOR = vec4(r, g, b, 1.0);
	}
}
