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

	public void GlorpPop()
	{
		if (Metagame.Instance.ResearchGlorpPoppingLevel > 0 && Metagame.Instance.ResearchJumpLevel > 0
		        && Utils.Rng.Randf() < 1.0f - Mathf.Pow(1.0f / Metagame.Instance.ResearchGlorpPoppingLevel, 0.5f))
		{
			_replicationCountdown += Metagame.Instance.ResearchJumpLevel;
		}
	}
	
	private Vector2 RandomPosition(float height = 10.0f) => World.Instance.Centre + (new Vector2(Utils.Rng.Randf() - 0.5f, Utils.Rng.Randf() - 0.5f)).Normalized() * (World.Instance.Radius + height);

	private bool ConstructionHelper(string item, string tooltip, float cost, int count, PackedScene scene, BoidController.BoidType type, float radius)
	{
		ImGui.BeginDisabled(Metagame.Instance.Materials < cost);
		if (ImGui.Button($"Construct {item} ({cost:F1})"))
		{
			Metagame.Instance.Materials -= cost;
			BoidController.Instance.SpawnBuilding(scene, type, type == BoidController.BoidType.SpaceLaser ? RandomPosition(48.0f) : RandomPosition(), radius);
			return true;
		}
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) 
		{
			ImGui.BeginTooltip();
			ImGui.PushTextWrapPos(512.0f);
			ImGui.Text(tooltip);
			ImGui.PopTextWrapPos();
			ImGui.EndTooltip();
		}
		ImGui.SameLine(); ImGui.Text($"Currently x{count}");
		ImGui.EndDisabled();
		return false;
	}
	
	private bool ResearchHelper(string item, string tooltip, float cost, int count, bool researched)
	{
		ImGui.BeginDisabled(Metagame.Instance.Materials < cost || researched);
		string text = researched ? $"{item} Researched!" : $"Research {item} ({cost:F1})";
		if (ImGui.Button(text))
		{
			Metagame.Instance.Materials -= cost;
			return true;
		}
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
		{
			ImGui.BeginTooltip();
			ImGui.PushTextWrapPos(512.0f);
			ImGui.Text(tooltip);
			ImGui.PopTextWrapPos();
			ImGui.EndTooltip();
		}
		if (!researched)
		{
			ImGui.SameLine(); ImGui.Text($"Currently x{count}");
		}
		ImGui.EndDisabled();
		return false;
	}
	
	// TODO: big dig throwing (scattered pixels damage more pixels)
	// TODO: glorp space agency
	// TODO: mining space lasers

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
			ImGui.Text($"Energy Generation Rate: {Metagame.Instance.EnergyRate} per second");
			// if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
			// 	ImGui.SetTooltip("");
			// }
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
		if (ConstructionHelper("Hovel", "Hovels increase maximum Glorp population (Glorpulation).",
			    Metagame.Instance.HouseCost, Metagame.Instance.NumHouses, Resources.Instance.HouseScene, BoidController.BoidType.House, 4.0f))
		{
			Metagame.Instance.NumHouses++;
		}
		
		// Solar Farms
		if (ConstructionHelper("Solar Farm", "Increases energy generation rate.",
			    Metagame.Instance.SolarFarmCost, Metagame.Instance.NumSolarFarms, Resources.Instance.SolarFarmScene, BoidController.BoidType.ResearchedBuilding, 2.0f))
		{
			Metagame.Instance.NumSolarFarms++;
		}
		
		// Factory
		if (!Metagame.Instance.ResearchedFactory)
		{
			if (ConstructionHelper("Fabrication Station", "Allows glorpstruction of advanced digging Glorpnology.",
				    Metagame.ResearchFactoryCost, 0, Resources.Instance.FactoryScene, BoidController.BoidType.ResearchedBuilding, 8.0f))
			{
				Metagame.Instance.ResearchedFactory = true;
			}
		}
		else
		{
			// Factory Constructions
			if (ImGui.CollapsingHeader("Fabrication", ImGuiTreeNodeFlags.DefaultOpen))
			{
				if (ConstructionHelper("Excavator", "Digs harder and faster than a Glorp. May or may not be an evolved form of a Glorp.",
					    Metagame.Instance.ExcavatorCost, Metagame.Instance.NumExcavators, Resources.Instance.ExcavatorScene, BoidController.BoidType.Excavator,
					    Metagame.ExcavatorRadius))
				{
					Metagame.Instance.NumExcavators++;
				}
			}
		}
		
		// Space Centre
		if (Metagame.Instance.ResearchedGSP)
		{
			if (!Metagame.Instance.ResearchedSpaceCentre)
			{
				if (ConstructionHelper("Glorp Space Centre", "Glorpkind was once a space-faring race. They will be so again.",
					    Metagame.ResearchSpaceCentreCost, 0, Resources.Instance.SpaceCentreScene, BoidController.BoidType.ResearchedBuilding, 8.0f))
				{
					Metagame.Instance.ResearchedSpaceCentre = true;
				}
			}
			else
			{
				// Space Centre Constructions
				if (ImGui.CollapsingHeader("Glorp Space Centre", ImGuiTreeNodeFlags.DefaultOpen))
				{
					if (ConstructionHelper("Space Lasers", "Did you think the first piece of Gnorpnology to be unlocked through the Space Centre would be anything else?",
						    Metagame.Instance.SpaceLaserCost, Metagame.Instance.NumSpaceLasers, Resources.Instance.SpaceLaserScene, BoidController.BoidType.SpaceLaser,
						    Metagame.ExcavatorRadius))
					{
						Metagame.Instance.NumSpaceLasers++;
					}
				}
			}
		}
	}

	private void _ImGuiResearch()
	{
		if (ResearchHelper("Yo-diggity", "Glorps will jump before digging, increasing impact force but costing more energy.",
			    Metagame.Instance.ResearchJumpCost, Metagame.Instance.ResearchJumpLevel, false))
		{
			Metagame.Instance.ResearchJumpLevel += 1;
		}
		
		if (ResearchHelper("Offworld Catering", "While the Glorp situation might seem dire, stranded on a remote planet in the void of space, luckily somebody left a takeout menu here and the number is still active.",
			    Metagame.BaseResearchOffworldCateringCost, 0, Metagame.Instance.ResearchedOffworldCatering))
		{
			Metagame.Instance.ResearchedOffworldCatering = true;
		}

		if (Metagame.Instance.ResearchedOffworldCatering)
		{
			if (ImGui.CollapsingHeader("Offworld Catering", ImGuiTreeNodeFlags.DefaultOpen))
			{
				ImGui.Indent();
				if (ResearchHelper("Chicken Tender Surprise", "Offworld shipping provides succulent chicken for all Glorpkind, improving morale and thus digging speed.",
					    Metagame.Instance.ResearchChickenTendersCost, Metagame.Instance.ResearchChickenTendersLevel, false))
				{
					Metagame.Instance.ResearchChickenTendersLevel += 1;
				}
				ImGui.Unindent();
			}
		}
		
		if (ResearchHelper("Neutrino Capture", "Solar Farms also capture the elusive neutrino particles, converting them into extra energy.",
			    Metagame.BaseResearchNeutrinoCaptureCost, 0, Metagame.Instance.ResearchedNeutrinoCapture))
		{
			Metagame.Instance.ResearchedNeutrinoCapture = true;
		}
		
		if (ResearchHelper("Glorp Popping", "Sometimes, when a Glorp impacts the ground, a new Glorp will pop out. Adds progress to the next Glorp replacation when this happens.",
			    Metagame.Instance.ResearchGlorpPoppingCost, Metagame.Instance.ResearchGlorpPoppingLevel, false))
		{
			Metagame.Instance.ResearchGlorpPoppingLevel += 1;
		}
		
		if (ResearchHelper("Glorp Space Program", "To escape this planet, Glorps will need a space program that can compete with any other space program!",
			    Metagame.BaseResearchGSPCost, 0, Metagame.Instance.ResearchedGSP))
		{
			Metagame.Instance.ResearchedGSP = true;
		}
	}
}
