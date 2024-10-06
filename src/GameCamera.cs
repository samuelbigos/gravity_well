using Godot;

public partial class GameCamera : Camera2D
{
	[Export] private World _world;
	[Export] private float _zoomSpeed = 0.01f;

	public static GameCamera Instance;
	
	private Vector2 _targetPosition;
	private float _targetSize;
	
	public override void _Ready()
	{
		Zoom = Vector2.One * 2.0f;
		Position = _world.Centre;
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		Instance = this;
	}

	public override void _Process(double delta)
	{
		if (Game.Instance.WonGame)
		{
			Zoom -= Zoom * _zoomSpeed * 2.0f;
		}
		else
		{
			if (Input.IsActionJustPressed("camera_zoom_in"))
			{
				Zoom += Zoom * _zoomSpeed;
			}
			if (Input.IsActionJustPressed("camera_zoom_out"))
			{
				Zoom -= Zoom * _zoomSpeed;
			}
		}
	}
}
