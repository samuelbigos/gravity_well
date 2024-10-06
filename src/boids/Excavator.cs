using Godot;
using System;

public partial class Excavator : Node2D
{
	[Export] private Node2D _sprite;
	
	private float _digTimer;
	private BoidController.BoidType _type = BoidController.BoidType.Excavator;
	
	public override void _Ready()
	{
		_digTimer = Metagame.Instance.ExcavatorDigFrequency;
	}
	
	public override void _Process(double delta)
	{
		_digTimer -= (float)delta;
		if (_digTimer < 0.0f && Metagame.Instance.Dig(_type))
		{
			_sprite.Position = Vector2.Zero;
			World.Instance.Dig(GlobalPosition, Metagame.ExcavatorRadius, Metagame.Instance.DigDamage(_type));
			_digTimer = Metagame.Instance.ExcavatorDigFrequency;
		}
	}
}
