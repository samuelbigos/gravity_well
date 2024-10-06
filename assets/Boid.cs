using Godot;
using System;

public partial class Boid : Sprite2D
{
	private float _digTimer;
	
	public override void _Ready()
	{
		_digTimer = BoidController.Instance.DigFrequency;
	}
	
	public override void _Process(double delta)
	{
		_digTimer -= (float)delta;
		if (_digTimer < 0.0f)
		{
			World.Instance.Dig(this);
			_digTimer = BoidController.Instance.DigFrequency;// + (Utils.Rng.Randf() - 1.0f) * DigRate;
		}
	}
}
