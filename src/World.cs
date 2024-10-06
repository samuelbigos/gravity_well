using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using ImGuiNET;

public partial class World : Singleton<World>
{
	[Export] private int _worldResolution = 512;
	[Export] private float _worldRadius = 256.0f;
	[Export] private Sprite2D _sprite;
	[Export] private float _sdfDistMod = 10.0f;
	[Export] private PackedScene _pixelScene;

	public Vector2I Size => new Vector2I(_worldResolution, _worldResolution);
	public Vector2 Centre => Size / (int)2.0f;
	public float Radius => _worldRadius;
	public Rid DistanceFieldRid => _dfTexture;
	public float SDFDistMod => _sdfDistMod;
	
	public Action OnInitialised;

	private Image _worldImage;
	private ImageTexture _worldTexture;
	private bool _worldImageDirty = true;
	
	// Compute
	private Vector2I _workgroupSize = new Vector2I(32, 32);
	
	private Rid _worldSimShader;
	private Rid _voronoiSeedShader;
	private Rid _jumpFloodShader;
	private Rid _distanceFieldShader;
	
	private Rid _worldSimPipeline;
	private Rid _voronoiSeedPipeline;
	private Rid _jumpFlooPipeline;
	private Rid _distanceFieldPipeline;
	
	private Rid[] _swapTextures = new Rid[2];
	private Rid _dfTexture;
	private Rid _dfSwap;
	private Dictionary<Rid, Rid[]> _swapSets = new Dictionary<Rid, Rid[]>();
	private int _currentSwap;

	private RenderingDevice _rd;
	private RDTextureFormat _format;
	private Texture2Drd _texture2Drd = new Texture2Drd();
	
	private List<Pixel> _pixels = new List<Pixel>();
	
	public void Dig(Glorp glorp)
	{
		// Find the nearest pixel to dig.
		int digRadius = (int)BoidController.Instance.GlorpRadius + 2;
		Vector2 digOffset = ToCentre(glorp.GlobalPosition) * 0.0f; 
		for (int x = -digRadius; x <= digRadius * 2; x++)
		{
			for (int y = -digRadius; y <= digRadius * 2; y++)
			{
				if ((new Vector2I(x, y)).LengthSquared() < digRadius * digRadius)
				{
					DigPixel(glorp.GlobalPosition, (Vector2I)(glorp.GlobalPosition + digOffset + new Vector2(x, y)));
				}
			}
		}
	}

	private void DigPixel(Vector2 boidPos, Vector2I pixel)
	{
		if (CheckPixel(pixel))
		{
			if (DamagePixel(pixel, Metagame.Instance.DigDamage, out float materials))
			{
				DestroyPixel(pixel);
			}
			CreatePixelParticle(boidPos);
			Metagame.Instance.Materials += materials;
			_worldImageDirty = true;
		}
	}

	private void DestroyPixel(Vector2I pixel)
	{
		_worldImage.SetPixel(pixel.X, pixel.Y, new Color(0.0f, 0.0f, 0.0f, 0.0f));
		_worldImageDirty = true;
	}

	private void CreatePixelParticle(Vector2 position)
	{
		Pixel thePixel = _pixelScene.Instantiate<Pixel>();
		_pixels.Add(thePixel);
		AddChild(thePixel);
		thePixel.GlobalPosition = position;
		Vector2 toCentre = ToCentre(position);
		Vector2 tangent = toCentre.ToNumerics().Rot90().ToGodot() * (Utils.Rng.Randf() - 1.0f);
		thePixel._velocity = (tangent * 0.5f - toCentre) * Metagame.Instance.DigEjectionSpeed;
	}

	private bool DamagePixel(Vector2I pixel, float damage, out float materials)
	{
		Color current = _worldImage.GetPixel(pixel.X, pixel.Y);
		float healthBefore = PixelColorToHealth(current);
		float healthAfter = Mathf.Max(0.0f, healthBefore - damage);
		float delta = healthBefore - healthAfter;
		_worldImage.SetPixel(pixel.X, pixel.Y, PixelHealthToColor(healthAfter));
		materials = delta;
		
		return healthAfter <= 0.0f || Mathf.IsZeroApprox(healthAfter);
	}

	public float PixelColorToHealth(Color col)
	{
		return col.G * Metagame.PixelHealth;
	}

	public Color PixelHealthToColor(float health)
	{
		health /= Metagame.PixelHealth;
		return new Color(1.0f, health, health, 1.0f);
	}

	public Vector2 ToCentre(Vector2 pos)
	{
		return (Centre - pos).Normalized();
	}

	private bool CheckPixel(Vector2I pos)
	{
		if (pos.X < 0 || pos.Y < 0 || pos.X >= _worldResolution || pos.Y >= _worldResolution) return false;
		Color pixel = _worldImage.GetPixel(pos.X, pos.Y);
		return pixel.A > 0.5f;
	}
	
	public override void _Ready()
	{
		DebugImGui.Instance.RegisterWindow("world", "World", _ImGui);
		//DebugImGui.Instance.SetCustomWindowEnabled("world", true);
			
		// Create the world, slow and hacky but easy.
		Vector2 centre = new Vector2(_worldResolution / 2, _worldResolution / 2);
		_worldImage = Image.CreateEmpty(_worldResolution, _worldResolution, false, Image.Format.Rgbaf);
		for (int x = 0; x < _worldResolution; x++)
		{
			for (int y = 0; y < _worldResolution; y++)
			{
				int i = y * _worldResolution + x;
				float d = (centre - new Vector2(x, y)).Length();
				if (d <= _worldRadius)
					_worldImage.SetPixel(x, y, Colors.White);
			}
		}
		
		_format = new RDTextureFormat();
		_format.Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat;
		_format.Width = (uint) _worldResolution;
		_format.Height = (uint) _worldResolution;
		_format.UsageBits = RenderingDevice.TextureUsageBits.SamplingBit |
		                    RenderingDevice.TextureUsageBits.ColorAttachmentBit |
		                    RenderingDevice.TextureUsageBits.StorageBit |
		                    RenderingDevice.TextureUsageBits.CanUpdateBit |
		                    RenderingDevice.TextureUsageBits.CanCopyToBit;
		
		_rd = RenderingServer.GetRenderingDevice();

		{
			RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://assets/compute/world_sim.glsl");
			RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
			_worldSimShader = _rd.ShaderCreateFromSpirV(shaderBytecode);
			_worldSimPipeline = _rd.ComputePipelineCreate(_worldSimShader);
		}

		{
			RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://assets/compute/voronoi_seed.glsl");
			RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
			_voronoiSeedShader = _rd.ShaderCreateFromSpirV(shaderBytecode);
			_voronoiSeedPipeline = _rd.ComputePipelineCreate(_voronoiSeedShader);
		}
		
		{
			RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://assets/compute/jump_flood.glsl");
			RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
			_jumpFloodShader = _rd.ShaderCreateFromSpirV(shaderBytecode);
			_jumpFlooPipeline = _rd.ComputePipelineCreate(_jumpFloodShader);
		}
		
		{
			RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://assets/compute/distance_field.glsl");
			RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
			_distanceFieldShader = _rd.ShaderCreateFromSpirV(shaderBytecode);
			_distanceFieldPipeline = _rd.ComputePipelineCreate(_distanceFieldShader);
		}

		CreateSwapTextures();
		
		_dfTexture = _rd.TextureCreate(_format, new RDTextureView());
		_dfSwap = CreateUniform(_dfTexture, _distanceFieldShader);
		
		OnInitialised?.Invoke();
	}
	
	private void CreateSwapTextures()
	{
		_swapTextures[0] = _rd.TextureCreate(_format, new RDTextureView());
		_swapTextures[1] = _rd.TextureCreate(_format, new RDTextureView());
		_rd.TextureClear(_swapTextures[0], Colors.Teal, 0, 1, 0, 1);
		_rd.TextureClear(_swapTextures[1], Colors.Teal, 0, 1, 0, 1);
		_swapSets[_worldSimShader] = new Rid[2];
		_swapSets[_worldSimShader][0] = CreateUniform(_swapTextures[0], _worldSimShader);
		_swapSets[_worldSimShader][1] = CreateUniform(_swapTextures[1], _worldSimShader);
		_swapSets[_voronoiSeedShader] = new Rid[2];
		_swapSets[_voronoiSeedShader][0] = CreateUniform(_swapTextures[0], _voronoiSeedShader);
		_swapSets[_voronoiSeedShader][1] = CreateUniform(_swapTextures[1], _voronoiSeedShader);
		_swapSets[_jumpFloodShader] = new Rid[2];
		_swapSets[_jumpFloodShader][0] = CreateUniform(_swapTextures[0], _jumpFloodShader);
		_swapSets[_jumpFloodShader][1] = CreateUniform(_swapTextures[1], _jumpFloodShader);
		_swapSets[_distanceFieldShader] = new Rid[2];
		_swapSets[_distanceFieldShader][0] = CreateUniform(_swapTextures[0], _distanceFieldShader);
		_swapSets[_distanceFieldShader][1] = CreateUniform(_swapTextures[1], _distanceFieldShader);
	}
	
	private Rid CreateUniform(Rid rid, Rid shader)
	{
		RDUniform uniform = new RDUniform();
		uniform.UniformType = RenderingDevice.UniformType.Image;
		uniform.Binding = 0;
		uniform.AddId(rid);
		Rid uniformRid = _rd.UniformSetCreate([uniform], shader, 0);
		Utils.Assert(uniformRid.IsValid, "Failed to create uniform.");
		return uniformRid;
	}

	private void StepWorldSim()
	{
		Rid worldImageTextureRid = _rd.TextureCreate(_format, new RDTextureView(), [_worldImage.GetData()]);
		Rid worldImageUniformRid = CreateUniform(worldImageTextureRid, _worldSimShader);
		
		ExecuteCompute(_worldSimPipeline, [_swapSets[_worldSimShader][InputSwapIndex], 
			_swapSets[_worldSimShader][OutputSwapIndex], worldImageUniformRid]);
		
		_rd.FreeRid(worldImageTextureRid);
		
		_texture2Drd.TextureRdRid = _swapTextures[OutputSwapIndex];
	}

	private void GenerateVoronoiSeed()
	{
		_currentSwap = (_currentSwap + 1) % 2;
		
		ExecuteCompute(_voronoiSeedPipeline, [_swapSets[_jumpFloodShader][InputSwapIndex], 
			_swapSets[_jumpFloodShader][OutputSwapIndex]]);
		
		_texture2Drd.TextureRdRid = _swapTextures[OutputSwapIndex];
	}

	private void GenerateVoronoi()
	{
		// Number of passes required is the log2 of the largest viewport dimension rounded up to the nearest power of 2.
		int passes = Mathf.CeilToInt(Mathf.Log(Mathf.Max(_worldResolution, _worldResolution)) / Mathf.Log(2.0f));
		for (int i = 0; i < passes; i++)
		{
			// Offset for each pass is half the previous one, starting at half the square resolution rounded up to nearest power 2.
			//i.e. for 768x512 we round up to 1024x1024 and the offset for the first pass is 512x512, then 256x256, etc. 
			float offset = Mathf.Pow(2, passes - i - 1);
			
			float[] constants = [offset, 0.0f, 0.0f, 0.0f];
			byte[] constantsByte = new byte[constants.Length * 4];
			Buffer.BlockCopy(constants, 0, constantsByte, 0, constantsByte.Length);

			// Switch our swap textures, so the previous output becomes the input. If this is the first pass,
			// the input will now be the output of the Voronoi seed pass.
			_currentSwap = (_currentSwap + 1) % 2;
			
			ExecuteCompute(_jumpFlooPipeline, [_swapSets[_jumpFloodShader][InputSwapIndex], 
				_swapSets[_jumpFloodShader][OutputSwapIndex]], (computeList) =>
			{
				_rd.ComputeListSetPushConstant(computeList, constantsByte, (uint) constantsByte.Length);
			});
			
			_texture2Drd.TextureRdRid = _swapTextures[OutputSwapIndex];
		}
	}

	private void GenerateDistanceField()
	{
		_currentSwap = (_currentSwap + 1) % 2;
		
		float[] constants = [_sdfDistMod, 0.0f, 0.0f, 0.0f];
		byte[] constantsByte = new byte[constants.Length * 4];
		Buffer.BlockCopy(constants, 0, constantsByte, 0, constantsByte.Length);

		ExecuteCompute(_distanceFieldPipeline, [_swapSets[_jumpFloodShader][InputSwapIndex], 
			_dfSwap], (computeList) =>
		{
			_rd.ComputeListSetPushConstant(computeList, constantsByte, (uint) constantsByte.Length);
		});
		
		_texture2Drd.TextureRdRid = _dfTexture;
	}

	private int InputSwapIndex => _currentSwap;
	private int OutputSwapIndex => (_currentSwap + 1) % 2;

	private void ExecuteCompute(Rid pipeline, IReadOnlyList<Rid> uniforms, Action<long> extra = null)
	{
		long computeList = _rd.ComputeListBegin();
		_rd.ComputeListBindComputePipeline(computeList, pipeline);

		for (uint i = 0; i < uniforms.Count; i++)
		{
			Rid uniform = uniforms[(int) i];
			_rd.ComputeListBindUniformSet(computeList, uniform, i);
		}
		
		extra?.Invoke(computeList);
		
		Vector2I workgroupCount = new Vector2I(_worldResolution / _workgroupSize.X, _worldResolution / _workgroupSize.Y);
		_rd.ComputeListDispatch(computeList, (uint) workgroupCount.X, (uint) workgroupCount.Y, 1);
		_rd.ComputeListEnd();
		_rd.Submit();
		_rd.Sync();
	}

	public override void _Process(double delta)
	{
		// Merge loose pixels.
		List<int> toRemove = new List<int>();
		for (var i = 0; i < _pixels.Count; i++)
		{
			Pixel pixel = _pixels[i];
			if (pixel.Lifetime < 1.0f) continue;
			if (pixel.WasFree && CheckPixel((Vector2I)pixel.GlobalPosition))
			{
				// Step back until reaching an empty pixel.
				float stepSize = 0.1f;
				float dist = stepSize;
				while (true)
				{
					Vector2I pos = (Vector2I)(pixel.GlobalPosition - dist * pixel._velocity.Normalized());
					if (!CheckPixel(pos))
					{
						//_worldImage.SetPixel(pos.X, pos.Y, new Color(1.0f, 1.0f, 1.0f, 1.0f));
						toRemove.Add(i);
						_worldImageDirty = true;
						break;
					}
		
					dist += stepSize;
					if (dist > 10.0f) // safety.
					{
						toRemove.Add(i);
						break;
					}
				}
			}
			else
			{
				pixel.WasFree = true;
			}
		}
		
		foreach (int i in toRemove)
		{
			_pixels[i].QueueFree();
			_pixels.RemoveAt(i);
		}

		if (_worldImageDirty)
		{
			_worldTexture = ImageTexture.CreateFromImage(_worldImage);
			_sprite.Texture = _worldTexture;
			_worldImageDirty = false;
		}
		
		StepWorldSim();
		GenerateVoronoiSeed();
		GenerateVoronoi();
		GenerateDistanceField();
	}
	
	private void _ImGui()
	{
		ImGuiGodot.Widgets.Image(_texture2Drd, new System.Numerics.Vector2(512, 512));
	}
}
