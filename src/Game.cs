using Godot;
using System;
using ImGuiNET;

public partial class Game : Singleton<Game>
{
	[Export] private int _initialBoids = 5;
	[Export] private float _initialSpawnRate = 0.5f;
	
	// Houses
	[Export] private int _popPerHouse = 3;
	[Export] private int _houseCost = 50;

	public int MaxPop => _initialBoids + BoidController.Instance.NumHouses * _popPerHouse;
	public int MaterialCount = 0;

	private int _maxBoids;
	private float _initialSpawnTimer = 0.0f;

	public float ReplicationRate => 0.0f;
	
	public override void _Ready()
	{
		DebugImGui.Instance.RegisterWindow("game", "Gravity Well", _ImGuiStats);
		DebugImGui.Instance.SetCustomWindowEnabled("game", true);
		
		DebugImGui.Instance.RegisterWindow("build", "Construct", _ImGuiConstruct);
		DebugImGui.Instance.SetCustomWindowEnabled("build", true);

		_maxBoids = _initialBoids;
		_initialSpawnTimer = _initialSpawnRate;
	}

	public override void _Process(double delta)
	{
		_initialSpawnTimer -= (float)delta;
		if (_initialSpawnTimer < 0.0f && BoidController.Instance.NumGlorps < _initialBoids)
		{
			_initialSpawnTimer = _initialSpawnRate;
			BoidController.Instance.SpawnGlorp();
		}
	}
	
	// TODO: pixel health points.
	// TODO: crashed spaceship.
	// TODO: energy
	// TODO: boid dig jumping
	// TODO: big dig throwing (scattered pixels damage more pixels)
	// TODO: glorp space agency
	// TODO: mining space lasers
	// TODO: excavators

	private void _ImGuiStats()
	{
		if (ImGui.CollapsingHeader("Population", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text($"Glorp Count / Max: {BoidController.Instance.NumGlorps} / {MaxPop}");
			ImGui.Text($"Glorp Replication Rate: {BoidController.Instance.ReplicationRate:F1}");
			ImGui.Text($"Glorp Replication Progress: {BoidController.Instance.ReplicationProgress:P}%");
			
		}
		if (ImGui.CollapsingHeader("Resources", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text($"Matter Count: {Game.Instance.MaterialCount}");
		}
		if (ImGui.CollapsingHeader("Gathering", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text($"Dig Frequency: {BoidController.Instance.DigFrequency}");
		}
	}

	private void _ImGuiConstruct()
	{
		ImGui.BeginDisabled(MaterialCount < _houseCost);
		if (ImGui.Button("Build House (50)"))
		{
			MaterialCount -= _houseCost;
			BoidController.Instance.SpawnHouse();
		}
		ImGui.EndDisabled();
	}
}
