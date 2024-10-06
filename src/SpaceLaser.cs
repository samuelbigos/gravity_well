using Godot;
using System;

public partial class SpaceLaser : Node2D
{
    [Export] private Node2D _sprite;
    [Export] private Node2D _laserSprite;
    [Export] private float _laserTime = 1.0f;
    [Export] private float _laserDigFrequency = 0.1f;
    
    private float _digTimer;
    private float _laserDigTimer;
    private bool _lasering;
    private BoidController.BoidType _type = BoidController.BoidType.Orbiter;
	
    public override void _Ready()
    {
        _digTimer = Metagame.Instance.SpaceLaserDigFrequency;
        _laserSprite.Visible = false;
    }
	
    public override void _Process(double delta)
    {
        _digTimer -= (float)delta;
        if (!_lasering && _digTimer < 0.0f && Metagame.Instance.Dig(_type))
        {
            _digTimer = _laserTime;
            _lasering = true;
            _laserDigTimer = _laserDigFrequency;
        }
        if (_lasering)
        {
            _laserSprite.Visible = true;
            _laserDigTimer -= (float) delta;
            bool canLaser = Metagame.Instance.Dig(_type);
            if (!canLaser)
            {
                _digTimer = 0.0f;
            }
            else if (_laserDigTimer < 0.0f)
            {
                _laserDigTimer = _laserDigFrequency;
                Vector2 position = World.Instance.ProjectOntoPixel(GlobalPosition, World.Instance.ToCentre(GlobalPosition));
                float dist = (position - GlobalPosition).Length();
                float width = 1.0f;
                _laserSprite.Scale = new Vector2(Metagame.Instance.SpaceLaserRadius, dist);
                _laserSprite.Position = new Vector2(0.0f, 3.0f + dist * 0.5f);
                World.Instance.Dig(position, Metagame.Instance.SpaceLaserRadius * 4.0f - 2.0f, Metagame.Instance.DigDamage(_type));
            }
            if (_digTimer < 0.0f)
            {
                _laserSprite.Visible = false;
                _lasering = false;
                _digTimer = Metagame.Instance.SpaceLaserDigFrequency;
            }
        }
    }
}
