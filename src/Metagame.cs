using Godot;
using System;

public partial class Metagame : Singleton<Metagame>
{
	// Digging
	public static float BaseDigDamage = 1.0f;
	public static float BaseDigEjectionSpeed = 10.0f;
	public static float BaseDigFrequency = 5.0f;
	public static float BaseDigEnergyCost = 1.0f;
	
	public float DigDamage => BaseDigDamage;
	public float DigFrequency => BaseDigFrequency;
	public float DigEjectionSpeed => BaseDigEjectionSpeed;
	public float DigEnergyCost => BaseDigEnergyCost;

	public bool Dig()
	{
		if (Energy > DigEnergyCost)
		{
			Energy -= DigEnergyCost;
			return true;
		}
		return false;
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
	
	public float Materials = 0;
	
	// Energy
	public static float BaseEnergyRate = 1.0f;

	public float EnergyRate => BaseEnergyRate;
	public float Energy = 0.0f;
	
	// Glorps
	public static float BaseWalkSpeed = 2.5f;
	
	public float WalkSpeed => BaseWalkSpeed;
	
	// Buildings
	public static float BaseHouseCost = 50.0f;

	public int NumHouses => BoidController.Instance.NumHouses;
	public float HouseCost => (1.0f + Mathf.Pow(NumHouses, 1.1f))  * BaseHouseCost;


	// Research
	public static float BaseResearchJumpCost = 100.0f;

	public int ResearchJumpLevel = 0;
	public float ResearchJumpCost => (1.0f + Mathf.Pow(ResearchJumpLevel, 1.5f))  * BaseResearchJumpCost;

}
