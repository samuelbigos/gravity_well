using Godot;
using System;

public partial class Pixel : Node2D
{
	public Vector2 _velocity;
	public float Lifetime;
	public bool WasFree = true;

	public override void _Process(double delta)
	{
		Lifetime += (float)delta;
		_velocity += World.Instance.ToCentre(GlobalPosition) * (float)delta * BoidController.Instance.Gravity;
		GlobalPosition += _velocity * (float)delta;
	}
}
