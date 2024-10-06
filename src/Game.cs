using Godot;
using System;
using ImGuiNET;

public partial class Game : Singleton<Game>
{
	[Export] private int _initialBoids = 5;
	[Export] private float _initialSpawnRate = 2.5f;

	private int _maxBoids;
	private float _initialSpawnTimer = 0.0f;
	private int _spaceshipId;
	
	public override void _Ready()
	{
		DebugImGui.Instance.RegisterWindow("game", "Gravity Well", _ImGuiStats);
		DebugImGui.Instance.SetCustomWindowEnabled("game", true);
		
		DebugImGui.Instance.RegisterWindow("build", "Construct", _ImGuiConstruct);
		DebugImGui.Instance.SetCustomWindowEnabled("build", true);
		
		DebugImGui.Instance.RegisterWindow("research", "Research", _ImGuiResearch);
		DebugImGui.Instance.SetCustomWindowEnabled("research", true);

		_maxBoids = _initialBoids;
		_initialSpawnTimer = _initialSpawnRate;
	}
	
	public float _replicationProgress => _replicationCountdown / Metagame.Instance.ReplicationCountdownMax;
	private float _replicationCountdown = 0.0f;

	private bool _firstFrame = true;
	public override void _Process(double delta)
	{
		if (_firstFrame)
		{
			Vector2 position = World.Instance.Centre - new Vector2(0.0f, (World.Instance.Radius + 10.0f));
			_spaceshipId = BoidController.Instance.SpawnBuilding(Resources.Instance.SpaceshipScene, BoidController.BoidType.Spaceship, position, 8.0f);
			_firstFrame = false;
		}
		
		// Spawning
		_initialSpawnTimer -= (float)delta;
		if (_initialSpawnTimer < 0.0f && BoidController.Instance.NumGlorps < _initialBoids)
		{
			_initialSpawnTimer = _initialSpawnRate;
			BoidController.Instance.SpawnGlorp(BoidController.Instance.DataForBoid(_spaceshipId).position);
		}

		if (Metagame.Instance.GlorpCount > 0)
		{
			_replicationCountdown += Metagame.Instance.ReplicationRate * (float)delta;
			if (_replicationCountdown > Metagame.Instance.ReplicationCountdownMax && Metagame.Instance.GlorpCount < Metagame.Instance.GlorpCountMax)
			{
				_replicationCountdown = 0.0f;
				BoidController.Instance.SpawnGlorp(BoidController.Instance.DataForBoid(_spaceshipId).position);
			}
		}
		
		// Energy
		Metagame.Instance.Energy = Mathf.Min(Metagame.Instance.Energy + Metagame.Instance.EnergyRate * (float)delta, 100.0f);
	}
	
	// TODO: boid dig jumping
	// TODO: big dig throwing (scattered pixels damage more pixels)
	// TODO: glorp space agency
	// TODO: mining space lasers
	// TODO: excavators

	private void _ImGuiStats()
	{
		if (ImGui.CollapsingHeader("Population", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text($"Glorp Count: {Metagame.Instance.GlorpCount} / {Metagame.Instance.GlorpCountMax}");
			ImGui.Text($"Glorp Replication Rate: {Metagame.Instance.ReplicationRate:F1}");
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				ImGui.SetTooltip("Replication rate is primarily a function of the Glorp population, but there may be other ways to increase it.");
			}
			ImGui.Text($"Glorp Replication Progress: {_replicationProgress:P}%");
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				ImGui.SetTooltip("When this reaches 100%, a new Glorp will pop out (Glorps are asexual, even they don't know exactly how this happens).");
			}
		}
		if (ImGui.CollapsingHeader("Resources", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text($"Matter Count: {Metagame.Instance.Materials:F0}");
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				ImGui.SetTooltip("Glorp-chinery and Glorp-knowledge generally requires materials, which can be acquired by exploiting the planet.");
			}
			ImGui.Text($"Energy: {Metagame.Instance.Energy/100.0f:P}%");
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				ImGui.SetTooltip("Most processes consume energy, make sure you have enough generation to fuel your exploits!");
			}
		}
		if (ImGui.CollapsingHeader("Gathering", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text($"Dig Frequency: {Metagame.Instance.DigFrequency}");
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				ImGui.SetTooltip("Digging deals damage to the planet and extracts materials. It's a good thing Glorps aren't environmentalists.");
			}
		}
	}

	private void _ImGuiConstruct()
	{
		// Houses
		ImGui.BeginDisabled(Metagame.Instance.Materials < Metagame.Instance.HouseCost);
		if (ImGui.Button($"Build Hovel ({Metagame.Instance.HouseCost})"))
		{
			Metagame.Instance.Materials -= Metagame.Instance.HouseCost;
			Vector2 position = World.Instance.Centre + (new Vector2(Utils.Rng.Randf() - 0.5f, Utils.Rng.Randf() - 0.5f)).Normalized() * (World.Instance.Radius + 10.0f);
			BoidController.Instance.SpawnBuilding(Resources.Instance.HouseScene, BoidController.BoidType.House, position, 4.0f);
		}
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
			ImGui.SetTooltip("Hovels increase maximum Glorp population (Glorpulation).");
		}
	}

	private void _ImGuiResearch()
	{
		// Jumping
		ImGui.BeginDisabled(Metagame.Instance.Materials < Metagame.Instance.ResearchJumpCost);
		if (ImGui.Button($"Research Jump ({Metagame.Instance.ResearchJumpCost}) - Current Jump Level: {Metagame.Instance.ResearchJumpLevel}"))
		{
			Metagame.Instance.Materials -= Metagame.Instance.ResearchJumpCost;
			Metagame.Instance.ResearchJumpLevel += 1;
		}
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
			ImGui.SetTooltip("Glorps will jump before digging, increasing impact force but costing more energy.");
		}
	}
}
