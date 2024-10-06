using Godot;
using System;

public partial class Metagame : Singleton<Metagame>
{
	public float Materials = 0;
	
	// Digging
	public static float BaseDigDamage = 1.0f;
	public static float BaseDigEjectionSpeed = 10.0f;
	public static float BaseDigFrequency = 5.0f;
	public static float BaseDigEnergyCost = 1.0f;
	
	public float DigFrequency => BaseScaling(ResearchChickenTendersLevel, BaseDigFrequency, 0.9f);

	public bool Dig(BoidController.BoidType type)
	{
		float cost = DigEnergyCost(type);
		if (Energy > cost)
		{
			Energy -= cost;
			return true;
		}
		return false;
	}

	public float DigEnergyCost(BoidController.BoidType type)
	{
		switch (type)
		{
			case BoidController.BoidType.Glorp:
				return BaseScaling(ResearchJumpLevel, BaseDigEnergyCost, 1.1f);
			case BoidController.BoidType.Excavator:
				return BaseScaling(OverdriveExcavatorScale, BaseDigEnergyCost * 0.25f, 1.1f);
			case BoidController.BoidType.Orbiter:
				return BaseScaling(OverdriveSpaceLaserScale, BaseDigEnergyCost * 0.5f, 1.1f);
			default: Utils.Assert(false, "Unknown boid type");
				break;
		}

		return 0.0f;
	}

	public float DigDamage(BoidController.BoidType type)
	{
		switch (type)
		{
			case BoidController.BoidType.Glorp:
				return BaseScaling(ResearchJumpLevel, BaseDigDamage, 1.33f);
			case BoidController.BoidType.Excavator:
				return BaseScaling(OverdriveExcavatorScale, BaseDigDamage, 1.33f);
			case BoidController.BoidType.Orbiter:
				return BaseScaling(OverdriveSpaceLaserScale, BaseDigDamage, 1.5f);
			default: Utils.Assert(false, "Unknown boid type");
				break;
		}

		return 0.0f;
	}

	public float DigEjectionSpeed(float damage)
	{
		return Mathf.Log(damage * BaseDigEjectionSpeed);
	}
	
	// Replication
	public static int InitialMaxPop = 5;
	
	public int PopPerHouse = 5;
	public int GlorpCount => BoidController.Instance.NumGlorps;
	public int GlorpCountMax => InitialMaxPop + NumHouses * PopPerHouse;
	public float ReplicationRate => BaseScaling(ResearchForcedReplicationLevel, Mathf.Log(BoidController.Instance.NumGlorps), 1.33f);
	public float ReplicationCountdownMax => 100.0f;
	
	// Materials
	public static int PixelHealth = 10;
	
	// Energy
	public static float BaseEnergyRate = 1.0f;

	public float EnergyRate => BaseEnergyRate + (NumSolarFarms + NumSolarSails * 5.0f) * (ResearchedNeutrinoCapture ? 3 : 1);
	public float Energy = 100.0f;
	
	// Glorps
	public static float BaseWalkSpeed = 2.5f;
	public static float BaseGlorpRadius = 2.0f;
	
	public float WalkSpeed => BaseWalkSpeed;
	
	// Buildings
	public static float BaseHouseCost = 50.0f;
	public int NumHouses = 0;
	public float HouseCost => BaseScaling(NumHouses, BaseHouseCost, 1.025f);

	public static float BaseExcavatorCost = 100.0f;
	public static float BaseExcavatorDigMulti = 5;
	public static float ExcavatorRadius = 3.0f;
	public int NumExcavators = 0;
	public float ExcavatorCost => BaseScaling(NumExcavators, BaseExcavatorCost, 1.25f);
	public float ExcavatorDigFrequency => DigFrequency / BaseExcavatorDigMulti;
	
	public static float BaseSpaceLaserCost = 500.0f;
	public static float BaseSpaceLaserMulti = 1.0f;
	
	public float SpaceLaserRadius => 1.0f + (ResearchSwolasersLevel > 0 ? Mathf.Log(ResearchSwolasersLevel) : 0.0f);
	public int NumSpaceLasers = 0;
	public float SpaceLaserCost => BaseScaling(NumSpaceLasers, BaseSpaceLaserCost, 1.5f);
	public float SpaceLaserDigFrequency => DigFrequency / BaseSpaceLaserMulti;

	public static float BaseSolarFarmCost = 25.0f;
	public int NumSolarFarms = 0;
	public float SolarFarmCost => BaseScaling(NumSolarFarms, BaseSolarFarmCost, 1.5f);
	
	public static float BaseSolarSailCost = 2000.0f;
	public int NumSolarSails = 0;
	public float SolarSailCost => BaseScaling(NumSolarSails, BaseSolarSailCost, 1.75f);
	
	// Research
	public static float BaseResearchJumpCost = 100.0f;
	public int ResearchJumpLevel = 0;
	public float ResearchJumpCost => BaseScaling(ResearchJumpLevel, BaseResearchJumpCost, 1.33f);
	
	public static float BaseResearchForcedReplicationCost = 100.0f;
	public int ResearchForcedReplicationLevel = 0;
	public float ResearchForcedReplicationCost => BaseScaling(ResearchForcedReplicationLevel, BaseResearchForcedReplicationCost, 1.33f);
	
	public static float ResearchFactoryCost = 200.0f;
	public bool ResearchedFactory;
	
	public static float ResearchSpaceCentreCost = 100.0f;
	public bool ResearchedSpaceCentre;

	public static float BaseResearchNeutrinoCaptureCost = 300.0f;
	public bool ResearchedNeutrinoCapture;
	
	public static float BaseResearchGlorpPoppingCost = 200.0f;
	public int ResearchGlorpPoppingLevel = 0;
	public float ResearchGlorpPoppingCost => BaseScaling(ResearchGlorpPoppingLevel, BaseResearchGlorpPoppingCost, 1.33f);
	
	public static float BaseResearchOffworldCateringCost = 500.0f;
	public bool ResearchedOffworldCatering;
	
	public static float BaseResearchOverdriveCost = 750.0f;
	public bool ResearchedOverdrive;
	public float OverdriveExcavatorScale = 1.0f;
	public float OverdriveSpaceLaserScale = 1.0f;
	
	public static float BaseResearchChickenTendersCost = 50.0f;
	public int ResearchChickenTendersLevel = 0;
	public float ResearchChickenTendersCost => BaseScaling(ResearchChickenTendersLevel, BaseResearchChickenTendersCost, 1.25f);
	
	public static float BaseResearchSwolasersCost = 1000.0f;
	public int ResearchSwolasersLevel = 0;
	public float ResearchSwolasersCost => BaseScaling(ResearchSwolasersLevel, BaseResearchSwolasersCost, 1.3f);
	
	public static float BaseResearchGSPCost = 1000.0f;
	public bool ResearchedGSP;

	private float BaseScaling(float count, float value, float scalingFactor)
	{
		return value * Mathf.Pow(scalingFactor, count);
	}
}
