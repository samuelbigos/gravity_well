using Godot;
using System;

public partial class Glorp : Node2D
{
	[Export] private Node2D _sprite;
	[Export] private float _jumpTime = 0.25f;
	
	private float _digTimer;
	private bool _jumping;
	
	public override void _Ready()
	{
		_digTimer = Metagame.Instance.DigFrequency;
	}
	
	public override void _Process(double delta)
	{
		_digTimer -= (float)delta;
		if (!_jumping && _digTimer < 0.0f && Metagame.Instance.Dig(true))
		{
			_digTimer = _jumpTime;
			_jumping = true;
		}
		if (_jumping)
		{
			float d = -Mathf.Sin((_digTimer / _jumpTime) * Mathf.Pi);
			_sprite.Position = new Vector2(0.0f, d * Metagame.Instance.ResearchJumpLevel * Metagame.BaseGlorpRadius * 1.0f);
			
			if (_digTimer < 0.0f)
			{
				_jumping = false;
				_sprite.Position = Vector2.Zero;
				World.Instance.Dig(GlobalPosition, Metagame.BaseGlorpRadius, Metagame.Instance.DigDamage(true));
				_digTimer = Metagame.Instance.DigFrequency;
				Game.Instance.GlorpPop();
			}
		}
	}
}
