using Godot;
using System;
using System.Collections.Generic;
using ImGuiNET;

public partial class DebugImGui : Node
{
    private Action DrawImGuiMenuBar = null;

    public static DebugImGui Instance;

    private struct RegisteredWindow
    {
        public string Id;
        public string Name;
        public Action Callback;
        public string Shortcut;
        public string ShortcutDisplay;
    }
    
    private float _timescale;
    private List<RegisteredWindow> _registeredWindows = new();
    private bool _hasMovedParent;
    
    private const float _windowAlpha = 0.75f;
    private ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.AlwaysAutoResize;
    private Dictionary<string, bool> _windowEnabled = new Dictionary<string, bool>();
    
    public override void _Ready()
    {
        base._Ready();
        
        //RegisterWindow("performance", "Performance", _OnImGuiLayoutPerformance);
        //RegisterWindow("debug", "Debug", _OnImGuiLayoutDebug);
        
        _timescale = (float)Engine.TimeScale;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Instance = null;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        _OnImGuiLayout();
    }
    
    public override void _Input(InputEvent evt)
    {
        foreach (RegisteredWindow window in _registeredWindows)
        {
            if (!InputMap.HasAction(window.Shortcut))
                continue;
            
            if (evt.IsActionPressed(window.Shortcut))
                SetCustomWindowEnabled(window.Id, !_windowEnabled[window.Id]);
        }
    }

    public void SetCustomWindowEnabled(string Id, bool enabled)
    {
        _windowEnabled[Id] = enabled;
    }

    public void RegisterWindow(string id, string name, Action callback)
    {
        RegisteredWindow window = new() {Id = id, Name = name, Callback = callback, Shortcut = $"debug_show_{id}"};

        foreach (Variant action in InputMap.ActionGetEvents(window.Shortcut))
        {
            InputEventKey key = action.As<InputEventKey>();
            DebugUtils.Assert(key.CtrlPressed, $"RegisterWindow: {window.Id} action does not have Control modifier.");
            window.ShortcutDisplay += "CTRL+" + (char)key.Keycode;
        }
        
        _registeredWindows.Add(window);

        if (!_windowEnabled.ContainsKey(id))
            _windowEnabled[id] = false;
    }
    
    public void UnRegisterWindow(string id, Action callback)
    {
        int index = -1;
        for (int i = 0; i < _registeredWindows.Count; i++)
        {
            RegisteredWindow window = _registeredWindows[i];
            if (window.Id == id)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
            _registeredWindows.RemoveAt(index);
    }

    private void _OnImGuiLayoutPerformance()
    {
        ImGui.Text($"FPS: {Performance.GetMonitor(Performance.Monitor.TimeFps):F0}");
        
        ImGui.Text(" ### Processing");
        ImGui.Text($"TimeProcess: {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0f:F0}ms");
        ImGui.Text($"ObjectCount: {Performance.GetMonitor(Performance.Monitor.ObjectCount):F0}");
        ImGui.Text($"ObjectNodeCount: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount):F0}");
        ImGui.Text($"ObjectResourceCount: {Performance.GetMonitor(Performance.Monitor.ObjectResourceCount):F0}");
        ImGui.Text($"ObjectOrphanNodeCount: {Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount):F0}");

        ImGui.Text(" ### Rendering");
        ImGui.Text($"RenderTotalDrawCallsInFrame: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):F0}");
        ImGui.Text($"RenderTotalObjectsInFrame: {Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame):F0}");
        ImGui.Text($"RenderTotalPrimitivesInFrame: {Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame):F0}");

        ImGui.Text(" ### Memory");
        ImGui.Text($"MemoryStatic: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1024.0f:F0}KiB");
        ImGui.Text($"MemoryMessageBufferMax: {Performance.GetMonitor(Performance.Monitor.MemoryMessageBufferMax) / 1024.0f:F0}KiB");

        ImGui.Text(" ### Physics");
        ImGui.Text($"Physics3DActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics3DActiveObjects):F0}");
        ImGui.Text($"Physics2DActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics2DActiveObjects):F0}");
        ImGui.Text($"Physics3DIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics3DIslandCount):F0}KiB");
        ImGui.Text($"Physics2DIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics2DIslandCount):F0}KiB");
    }
    
    private void _OnImGuiLayoutDebug()
    {
        ImGui.Text("There's nothing here...");
    }

    private int _cameraMode;
    private Vector2I _terrainSize;

    private void _OnImGuiLayout()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Menu"))
            {
                if (ImGui.MenuItem("Restart"))
                {
                    GetTree().ReloadCurrentScene();
                }
                if (ImGui.MenuItem("Quit"))
                {
                    GetTree().Quit();
                }
                ImGui.EndMenu();
            }
            
            if (ImGui.BeginMenu("Windows"))
            {
                foreach (RegisteredWindow window in _registeredWindows)
                {
                    bool selected = _windowEnabled[window.Id];
                    if (ImGui.MenuItem($"{window.Name}", window.ShortcutDisplay, selected))
                    {
                        SetCustomWindowEnabled(window.Id, !selected);
                    }
                }
                ImGui.EndMenu();
            }
            
            DrawImGuiMenuBar?.Invoke();
            
            ImGui.EndMainMenuBar();
        }

        foreach (RegisteredWindow window in _registeredWindows)
        {
            if (_windowEnabled[window.Id])
            {
                ImGui.SetNextWindowBgAlpha(_windowAlpha);
                if (ImGui.Begin(window.Name, _windowFlags))
                {
                    window.Callback?.Invoke();
                }
            }
        }
    }
}
