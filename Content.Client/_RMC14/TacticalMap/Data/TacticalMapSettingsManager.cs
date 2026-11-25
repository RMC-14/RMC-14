using System.Globalization;
using System.Numerics;
using Content.Client._RMC14.TacticalMap.Controls;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapSettingsManager
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private const int MaxEntries = 32;

    private readonly List<(string Key, float Zoom, Vector2 Pan)> _views = new();
    private bool _loaded;

    public float BlipSize
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapBlipSize);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapBlipSize, value);
    }

    public float LineThickness
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapLineThickness);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapLineThickness, value);
    }

    public int ColorIndex
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapColorIndex);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapColorIndex, value);
    }

    public TacticalMapLabelMode LabelMode
    {
        get => (TacticalMapLabelMode) Math.Clamp(_config.GetCVar(RMCCVars.RMCTacticalMapLabelMode), 0, 2);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapLabelMode, (int) value);
    }

    public bool LabelsEnabled
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapLabelsEnabled);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapLabelsEnabled, value);
    }

    public bool ObjectivesVisible
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapObjectivesVisible);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapObjectivesVisible, value);
    }

    public bool OverlaysVisible
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapOverlaysVisible);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapOverlaysVisible, value);
    }

    public bool DisplayVisible
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapSettingsVisible);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapSettingsVisible, value);
    }

    public bool CrtEffect
    {
        get => _config.GetCVar(RMCCVars.RMCTacticalMapCrtEffect);
        set => _config.SetCVar(RMCCVars.RMCTacticalMapCrtEffect, value);
    }

    public Vector2? WindowSize
    {
        get
        {
            var width = _config.GetCVar(RMCCVars.RMCTacticalMapWindowWidth);
            var height = _config.GetCVar(RMCCVars.RMCTacticalMapWindowHeight);
            return width > 0 && height > 0 ? new Vector2(width, height) : null;
        }
        set
        {
            if (value is not { } size)
                return;

            _config.SetCVar(RMCCVars.RMCTacticalMapWindowWidth, (int) size.X);
            _config.SetCVar(RMCCVars.RMCTacticalMapWindowHeight, (int) size.Y);
        }
    }

    public Vector2? WindowPosition
    {
        get
        {
            var x = _config.GetCVar(RMCCVars.RMCTacticalMapWindowX);
            var y = _config.GetCVar(RMCCVars.RMCTacticalMapWindowY);
            return x >= 0 && y >= 0 ? new Vector2(x, y) : null;
        }
        set
        {
            if (value is not { } position)
                return;

            _config.SetCVar(RMCCVars.RMCTacticalMapWindowX, position.X);
            _config.SetCVar(RMCCVars.RMCTacticalMapWindowY, position.Y);
        }
    }

    public bool TryGetViewState(string key, out float zoom, out Vector2 pan)
    {
        EnsureLoaded();
        foreach (var view in _views)
        {
            if (view.Key != key)
                continue;

            zoom = view.Zoom;
            pan = view.Pan;
            return true;
        }

        zoom = 1f;
        pan = Vector2.Zero;
        return false;
    }

    public void SetViewState(string key, float zoom, Vector2 pan)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains(';') || key.Contains('='))
            return;

        EnsureLoaded();
        for (var i = _views.Count - 1; i >= 0; i--)
        {
            if (_views[i].Key == key)
                _views.RemoveAt(i);
        }

        _views.Add((key, zoom, pan));
        while (_views.Count > MaxEntries)
        {
            _views.RemoveAt(0);
        }

        var entries = new List<string>(_views.Count);
        foreach (var view in _views)
        {
            var values = string.Join(",",
                view.Zoom.ToString("R", CultureInfo.InvariantCulture),
                view.Pan.X.ToString("R", CultureInfo.InvariantCulture),
                view.Pan.Y.ToString("R", CultureInfo.InvariantCulture));
            entries.Add($"{view.Key}={values}");
        }

        _config.SetCVar(RMCCVars.RMCTacticalMapViewStates, string.Join(";", entries));
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        var raw = _config.GetCVar(RMCCVars.RMCTacticalMapViewStates);
        foreach (var entry in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split('=');
            if (parts.Length != 2)
                continue;

            var values = parts[1].Split(',');
            if (values.Length != 3 ||
                !float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom) ||
                !float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                continue;
            }

            _views.Add((parts[0], zoom, new Vector2(x, y)));
        }
    }
}
