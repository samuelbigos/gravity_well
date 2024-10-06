using Godot;
using System;

public partial class Metagame : Singleton<Metagame>
{
	// Digging
	public static float BaseDigDamage = 1.0f;
	public static float BaseDigEjectionSpeed = 10.0f;
	public static float BaseDigFrequency = 5.0f;
	public static float BaseDigEnergyCost = 1.0f;
	
	public float DigFrequency => BaseScaling(ResearchChickenTendersLevel, BaseDigFrequency, 0.9f);
	public float DigEjectionSpeed => BaseScaling(ResearchJumpLevel, BaseDigEjectionSpeed, 1.1f);
	public float DigEnergyCost => BaseDigEnergyCost;

	public bool Dig(bool isGlorp = false)
	{
		float cost = isGlorp ? BaseScaling(ResearchJumpLevel, DigEnergyCost, 1.1f) : DigEnergyCost;
		if (Energy > cost)
		{
			Energy -= cost;
			return true;
		}
		return false;
	}

	public float DigDamage(bool isGlorp = false)
	{
		return isGlorp ? BaseScaling(ResearchJumpLevel, BaseDigDamage, 1.1f) : BaseDigDamage;
	}
	
	// Replication
	public static float GlorpSpawnHeight = 10.0f;
	public static int InitialMaxPop = 5;
	public static int PopPerHouse = 3;

	public int GlorpCount => BoidController.Instance.NumGlorps;
	public int GlorpCountMax => InitialMaxPop + NumHouses * PopPerHouse;
	public float ReplicationRate => Mathf.Log(BoidController.Instance.NumGlorps);
	public float ReplicationCountdownMax => 100.0f;
	
	// Materials
	public static int PixelHealth = 10;
	
	public float Materials = 10000;
	
	// Energy
	public static float BaseEnergyRate = 1.0f;

	public float EnergyRate => BaseEnergyRate + NumSolarFarms * (ResearchedNeutrinoCapture ? 2 : 1);
	public float Energy = 0.0f;
	
	// Glorps
	public static float BaseWalkSpeed = 2.5f;
	public static float BaseGlorpRadius = 2.0f;
	
	public float WalkSpeed => BaseWalkSpeed;
	
	// Buildings
	public static float BaseHouseCost = 50.0f;
	public int NumHouses = 0;
	public float HouseCost => BaseScaling(NumHouses, BaseHouseCost, 1.25f);

	public static float BaseExcavatorCost = 100.0f;
	public static float BaseExcavatorDigMulti = 5;
	public static float ExcavatorRadius = 3.0f;
	public int NumExcavators = 0;
	public float ExcavatorCost => BaseScaling(NumExcavators, BaseExcavatorCost, 1.25f);
	public float ExcavatorDigFrequency => DigFrequency / BaseExcavatorDigMulti;
	
	public static float BaseSpaceLaserCost = 500.0f;
	public static float BaseSpaceLaserMulti = 1.0f;
	public static float SpaceLaserRadius = 3.0f;
	public int NumSpaceLasers = 0;
	public float SpaceLaserCost => BaseScaling(NumSpaceLasers, BaseSpaceLaserCost, 1.5f);
	public float SpaceLaserDigFrequency => DigFrequency / BaseSpaceLaserMulti;

	public static float BaseSolarFarmCost = 25.0f;
	public int NumSolarFarms = 0;
	public float SolarFarmCost => BaseScaling(NumSolarFarms, BaseSolarFarmCost, 1.33f);
	
	// Research
	public static float BaseResearchJumpCost = 100.0f;
	public int ResearchJumpLevel = 0;
	public float ResearchJumpCost => BaseScaling(ResearchJumpLevel, BaseResearchJumpCost, 1.33f);
	
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
	
	public static float BaseResearchChickenTendersCost = 50.0f;
	public int ResearchChickenTendersLevel = 0;
	public float ResearchChickenTendersCost => BaseScaling(ResearchChickenTendersLevel, BaseResearchChickenTendersCost, 1.25f);
	
	public static float BaseResearchGSPCost = 1000.0f;
	public bool ResearchedGSP;

	private float BaseScaling(int count, float cost, float scalingFactor)
	{
		return cost * Mathf.Pow(scalingFactor,count);
	}
}
