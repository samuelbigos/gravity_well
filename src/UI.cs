using Godot;
using System;

public partial class UI : Control
{
	[Export] private Label _materials;
	[Export] private Label _energy;
	[Export] private Label _excavated;
	
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		_materials.Text = $"Materials: {Metagame.Instance.Materials:F0}";
		_energy.Text = $"Energy: {Metagame.Instance.Energy/100.0f:P}";
		//_excavated.Text = $"Energy: {World.Instance.Excavated():P}";
	}
}
