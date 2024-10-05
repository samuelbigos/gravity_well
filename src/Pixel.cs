using Godot;
using System;

public partial class Pixel : Sprite2D
{
	public Vector2 _velocity;

	public override void _Process(double delta)
	{
		_velocity += World.Instance.ToCentre(GlobalPosition) * (float)delta * BoidController.Instance.Gravity;
		GlobalPosition += _velocity * (float)delta;
	}
}
