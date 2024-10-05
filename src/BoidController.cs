using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class BoidController : Singleton<BoidController>
{
    [Export] private PackedScene _boid;
    [Export] public float _boidRadius = 2.0f;
    
    // Compute
    private RenderingDevice _rd;
    private Vector2I _workgroupSize = new Vector2I(32, 32);
    private Rid _shader;
    private Rid _pipeline;
    private bool _ready;

    public struct BoidData
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 toSurface;
        public float dir;
        public float padding1;
        //public float padding2;
        //public float padding3;
    }

    private Rid _buffer;
    private Rid _bufferSet;
    
    private Rid _distanceFieldSet;

    private int MAX_BOIDS = 4096;
    private int _activeBoids = 0;

    private byte[] _boidData;
    private Dictionary<int, Node2D> _boids = new Dictionary<int, Node2D>();
    private Dictionary<Node2D, int> _boidsToData = new Dictionary<Node2D, int>();

    public int ActiveBoids => _activeBoids;
    public float Gravity => 50.0f;
    
    public BoidData DataForBoid(Boid boid)
    {
        Span<BoidData> dataSpan = MemoryMarshal.Cast<byte, BoidData>(new Span<byte>(_boidData, 0, _boidData.Length));
        return dataSpan[_boidsToData[boid]];
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        _rd = RenderingServer.GetRenderingDevice();
        World.Instance.OnInitialised += () =>
        {
            _rd = RenderingServer.GetRenderingDevice();
            {
                RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://assets/compute/boid_compute.glsl");
                RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
                _shader = _rd.ShaderCreateFromSpirV(shaderBytecode);
                _pipeline = _rd.ComputePipelineCreate(_shader);
            }

            // Data
            int bufferSize = Marshal.SizeOf<BoidData>();
            byte[] buffer = new byte[MAX_BOIDS * bufferSize];
            Span<BoidData> bufferSpan = MemoryMarshal.Cast<byte, BoidData>(new Span<byte>(buffer, 0, MAX_BOIDS * bufferSize));

            // Defaults.
            for (int i = 0; i < MAX_BOIDS; i++)
            {
                bufferSpan[i].position = World.Instance.Centre - new Vector2(0.0f, World.Instance.Radius + 64.0f);
                bufferSpan[i].velocity = new Vector2(0.0f, 0.0f);
                bufferSpan[i].dir = ((float)(Utils.Rng.Randi() % 2) - 0.5f) * 2.0f;
            }

            _buffer = _rd.StorageBufferCreate((uint)(MAX_BOIDS * bufferSize), buffer);
            _bufferSet = CreateUniform(_buffer, _shader, RenderingDevice.UniformType.StorageBuffer, 0);

            // Distance Field Layout
            RDUniform uniform = new RDUniform();
            uniform.UniformType = RenderingDevice.UniformType.SamplerWithTexture;
            uniform.Binding = 0;
            RDSamplerState sampler = new RDSamplerState();
            sampler.MagFilter = RenderingDevice.SamplerFilter.Linear;
            uniform.AddId(_rd.SamplerCreate(sampler));
            uniform.AddId(World.Instance.DistanceFieldRid);
            _distanceFieldSet = _rd.UniformSetCreate([uniform], _shader, 1);
            Utils.Assert(_distanceFieldSet.IsValid, "Failed to create uniform.");

            _ready = true;
        };
    }

    public void SpawnBoid()
    {
        if (_activeBoids < MAX_BOIDS)
        {
            _boids[_activeBoids] = _boid.Instantiate<Node2D>();
            _boids[_activeBoids].Visible = false;
            AddChild(_boids[_activeBoids]);
            _boidsToData[_boids[_activeBoids]] = _activeBoids;
            _activeBoids++;
        }
    }

    public override void _Process(double delta)
    {
        if (!_ready) return;
        if (_activeBoids == 0) return;
        
        ExecuteCompute(_pipeline, (float)delta);
        
        // Copy boid buffers to CPU for rendering.
        _boidData = _rd.BufferGetData(_buffer, 0, (uint) (MAX_BOIDS * Marshal.SizeOf<BoidData>()));
        Span<BoidData> dataSpan = MemoryMarshal.Cast<byte, BoidData>(new Span<byte>(_boidData, 0, _boidData.Length));
        
        for (int i = 0; i < _activeBoids; i++)
        {
            //GD.Print($"Boid #{i}: Position: {dataSpan[i].position} Velocity: {dataSpan[i].velocity}");
            //GD.Print($"Boid #{i}: ToSurface: {dataSpan[i].toSurface}");
            _boids[i].GlobalPosition = dataSpan[i].position;
            _boids[i].Visible = true;
        }
    }

    private void ExecuteCompute(Rid pipeline, float deltaTime)
    {
        long computeList = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(computeList, pipeline);
        _rd.ComputeListBindUniformSet(computeList, _bufferSet, 0);
        _rd.ComputeListBindUniformSet(computeList, _distanceFieldSet, 1);

        // Ordering must be the same as defined in the compute shader.
        List<float> constants =
        [
            _activeBoids,
            Instance._boidRadius,
            World.Instance.Size.X,
            World.Instance.Size.Y,
            World.Instance.SDFDistMod,
            deltaTime,
            Gravity, // gravity
            10.0f, // walk
        ];

        // Padding
        int alignment = 4;
        int paddedSize = Mathf.CeilToInt((float)constants.Count / alignment) * alignment;
        while (constants.Count < paddedSize) constants.Add(0);

        byte[] constantsByte = new byte[constants.Count * 4];
        Buffer.BlockCopy(constants.ToArray(), 0, constantsByte, 0, constantsByte.Length);
        _rd.ComputeListSetPushConstant(computeList, constantsByte, (uint)constantsByte.Length);

        int groups = _activeBoids;//Mathf.CeilToInt(_activeBoids / 1024.0f);
        _rd.ComputeListDispatch(computeList, (uint)groups, 1, 1);
        _rd.ComputeListEnd();
        _rd.Submit();
        _rd.Sync();
    }

    private Rid CreateUniform(Rid rid, Rid shader, RenderingDevice.UniformType type, uint set)
    {
        RDUniform uniform = new RDUniform();
        uniform.UniformType = type;
        uniform.Binding = 0;
        uniform.AddId(rid);
        Rid uniformRid = _rd.UniformSetCreate([uniform], shader, set);
        Utils.Assert(uniformRid.IsValid, "Failed to create uniform.");
        return uniformRid;
    }
}