using Godot;
using System;
using ImGuiNET;

public partial class Game : Node2D
{
	[Export] private int _initialBoids = 10;
	[Export] private float _initialSpawnRate = 0.5f;

	private int _maxBoids;
	private float _initialSpawnTimer = 0.0f;

	public float ReplicationRate => 0.0f;
	
	public override void _Ready()
	{
		DebugImGui.Instance.RegisterWindow("game", "Game", _ImGui);
		DebugImGui.Instance.SetCustomWindowEnabled("game", true);

		_maxBoids = _initialBoids;
		_initialSpawnTimer = _initialSpawnRate;
	}

	public override void _Process(double delta)
	{
		_initialSpawnTimer -= (float)delta;
		if (_initialSpawnTimer < 0.0f && BoidController.Instance.ActiveBoids < _initialBoids)
		{
			_initialSpawnTimer = _initialSpawnRate;
			BoidController.Instance.SpawnBoid();
		}
	}
	
	private void _ImGui()
	{
		ImGui.Text($"Glorp count: {BoidController.Instance.ActiveBoids}");
		ImGui.Text($"Glorp replication rate: {ReplicationRate}");
	}
}
