#[compute]
#version 450

/* Layouts */
struct ssbo_data
{
    vec2 pos;
	vec2 vel;
	vec2 toSurface;
	float dir;
	float padding1;

	//float padding2;
	//float padding3;
};
layout(set = 0, binding = 0, std430) restrict buffer Data {
	ssbo_data data[];
} boidData;

layout(set = 1, binding = 0) uniform sampler2D _distanceField;

layout(push_constant, std430) uniform Params {
	float numBoids;	
	float boidRadius;
	float imageSizeX;
	float imageSizeY;
	float sdfDistMod;
	float deltaTime;
	float gravity;
	float walkSpeed;
} params;

layout(local_size_x = 1024, local_size_y = 1, local_size_z = 1) in;

/* Shared functions */
float sdf(vec2 p) {
	vec2 uv = vec2(p.x, p.y);
	uv.x = uv.x / params.imageSizeX;
	uv.y = uv.y / params.imageSizeY;
	return clamp(texture(_distanceField, uv).r - 0.01, 0.000001, 1.0) * params.sdfDistMod;
}
vec2 calcNormal(vec2 p) {
	float h = 1;
	return normalize(vec2(sdf(p + vec2(h, 0)) - sdf(p - vec2(h, 0)),
					sdf(p + vec2(0, h)) - sdf(p - vec2(0, h))));
}
vec2 projectUonV(vec2 u, vec2 v) {
	vec2 r;
	r = v * (dot(u, v) / max(0.000001, dot(v, v)));
	return r;
}
float lengthSq(vec2 v) {
	return dot(v, v);
}
float sq(float v) {
	return v * v;
}
vec2 limit(vec2 v, float l) {
	float len = length(v);
	if (len == 0.0f) return v;
	float i = l / len;
	i = min(i, 1.0f);
	return v * i;
}

/* Main */
void main() 
{
	uint id = gl_GlobalInvocationID.x;
	if (id >= params.numBoids) return;

	vec2 boidPos = boidData.data[id].pos;
	vec2 boidVel = boidData.data[id].vel;

	float boidRadius = params.boidRadius;

	// Gravity
	vec2 centre = vec2(params.imageSizeX, params.imageSizeY) * 0.5;
	vec2 toCentre = normalize(centre - boidPos);
	vec2 totalForce = toCentre * params.gravity;

	// Collide with terrain
	float terrainDist = sdf(boidPos);
	vec2 toSurface = -calcNormal(boidPos);

	vec2 p0 = boidPos;
	vec2 p1 = boidPos + toSurface * terrainDist;
	vec2 v0 = boidVel;
	vec2 v1 = vec2(0.0, 0.0);
	float r0 = boidRadius;
	float r1 = 0.0;

	float separation = distance(p0, p1);
	float r = r0 + r1;
	float diff = separation - r;
	if (diff <= 0.0) {
		boidPos += diff * 1.0 * normalize(p1 - p0);
		vec2 nv0 = v0;
		nv0 += projectUonV(v1, p1 - p0);
		nv0 -= projectUonV(v0, p0 - p1);
		boidVel = nv0 * 1.0;
	
		// Walking
		vec2 tangent = normalize(vec2(-toSurface.y, toSurface.x)) * boidData.data[id].dir;
		boidVel = tangent * params.walkSpeed;
	}	

	boidVel += totalForce * params.deltaTime;
	boidPos += boidVel * params.deltaTime;

	boidPos += toCentre * params.deltaTime * 10.0f;

	// Write new data.
	boidData.data[id].pos = boidPos;
	boidData.data[id].vel = boidVel;
	boidData.data[id].toSurface = toSurface;
}