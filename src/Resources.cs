using Godot;
using System;

public partial class Resources : Singleton<Resources>
{
	[Export] public PackedScene GlorpScene;
	[Export] public PackedScene HouseScene;
	[Export] public PackedScene SpaceshipScene;
}
