using Godot;
using System;

public partial class Resources : Singleton<Resources>
{
	[Export] public PackedScene GlorpScene;
	[Export] public PackedScene HouseScene;
	[Export] public PackedScene SpaceshipScene;
	[Export] public PackedScene FactoryScene;
	[Export] public PackedScene ExcavatorScene;
	[Export] public PackedScene SolarFarmScene;
	[Export] public PackedScene SpaceCentreScene;
	[Export] public PackedScene SpaceLaserScene;
	[Export] public PackedScene SolarSailScene;
}
