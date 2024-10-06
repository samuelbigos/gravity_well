using Godot;
using System;
using System.Collections.Generic;

public partial class FloatingDamageManager : Singleton<FloatingDamageManager>
{
	[Export] private float _textLifetime = 0.5f;
	[Export] private PackedScene _floatingDamageScene;

	private List<Label> _labels = new List<Label>();
	private List<float> _lifetimes = new List<float>();
	private List<Vector2> _basePositions = new List<Vector2>();

	public void AddNew(Vector2 position, float damage)
	{
		Label label =_floatingDamageScene.Instantiate<Label>();
		AddChild(label);
		_labels.Add(label);
		label.Text = $"{damage:F0}";
		_lifetimes.Add(_textLifetime);
		_basePositions.Add(position);
	}

	public override void _Process(double delta)
	{
		for (int i = _labels.Count - 1; i >= 0; i--)
		{
			_lifetimes[i] -= (float) delta;
			if (_lifetimes[i] < 0.0f)
			{
				_labels[i].QueueFree();
				_labels.RemoveAt(i);
				_lifetimes.RemoveAt(i);
				_basePositions.RemoveAt(i);
			}
			else
			{
				_labels[i].GlobalPosition = _basePositions[i] - World.Instance.ToCentre(_basePositions[i]) 
				  * (15.0f + (float)Utils.Ease_CubicIn(_lifetimes[i] / _textLifetime) * -10.0f)
					- _labels[i].Size * 0.5f;
			}
		}
	}
}
