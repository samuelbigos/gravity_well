using Godot;
using System;

public partial class Boid : Sprite2D
{
	public float DigRate = 3.0f;
	public bool PendingDig = false;
	
	private float _digTimer;
	
	public override void _Ready()
	{
		_digTimer = DigRate;
	}
	
	public override void _Process(double delta)
	{
		_digTimer -= (float)delta;
		if (_digTimer < 0.0f)
		{
			//World.Instance.Dig(this);
			PendingDig = true;
			_digTimer = DigRate;// + (Utils.Rng.Randf() - 1.0f) * DigRate;
		}
	}
}
