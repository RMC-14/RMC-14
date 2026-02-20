using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Content.Client.Gameplay;
using Content.Client.Stylesheets;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Marines.ScreenAnnounce;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Marines.ScreenAnnounce;

public sealed class ScreenAnnounceUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [UISystemDependency] private readonly ScreenAnnounceSystem? _screenAnnounce = default!;

    private ScreenAnnounceControl? _screenAnnounceControl;

    public void OnStateEntered(GameplayState state)
    {
        _screenAnnounceControl = new ScreenAnnounceControl();
        UIManager.RootControl.AddChild(_screenAnnounceControl);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_screenAnnounceControl != null)
        {
            UIManager.RootControl.RemoveChild(_screenAnnounceControl);
            _screenAnnounceControl = null;
        }
    }

    public void UpdateAnnouncement(string[] announceText, ScreenAnnounceTarget type, ScreenAnnounceArgs settings, string startingMessage, NetEntity? squad)
    {
        _screenAnnounceControl?.UpdateAnnouncement(announceText, type, settings, startingMessage, squad);
    }
}

public sealed class ScreenAnnounceControl : Control
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Font _font;
    private string[] _announceText = Array.Empty<string>();
    private string _startingMessage = string.Empty;
    private ScreenAnnounceTarget _type;
    private EntityUid? _squad;

    private float _printSpeed = 0.03f;
    private float _shakeIntensity = 0.8f;
    private float _flickerChance = 0.02f;
    private float _glitchChance = 0.01f;
    private float _holdDuration = 3f;
    private float _fadeDuration = 1.5f;
    private float _lineHeightUnscaled = 40f;
    private float _maxTextWidthFraction = 0.9f;

    private float _timer;
    private float _globalTime;
    private float _holdStartTime;
    private int _currentLine;
    private int _currentChar;
    private bool _finished;
    private bool _fadingOut;

    public ScreenAnnounceControl()
    {
        IoCManager.InjectDependencies(this);
        _font = _resCache.NotoStack(variation: "Bold", 15);
    }

    public void UpdateAnnouncement(string[] announceText, ScreenAnnounceTarget type, ScreenAnnounceArgs settings, string startingMessage, NetEntity? squad)
    {
        _announceText = announceText;
        _startingMessage = startingMessage;
        _type = type;
        _squad = _entMan.GetEntity(squad);

        _printSpeed = settings.PrintSpeed;
        _shakeIntensity = settings.ShakeIntensity;
        _flickerChance = settings.FlickerChance;
        _glitchChance = settings.GlitchChance;
        _holdDuration = settings.HoldDuration;
        _fadeDuration = settings.FadeDuration;
        _lineHeightUnscaled = settings.LineHeightUnscaled;
        _maxTextWidthFraction = settings.MaxTextWidthFraction;

        _timer = 0;
        _globalTime = 0;
        _currentLine = 0;
        _currentChar = 0;
        _finished = false;
        _fadingOut = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null || !_entMan.HasComponent<MarineComponent>(player))
            return;

        var screenSize = PixelSize;

        float scale = _configManager.GetCVar(CVars.DisplayUIScale);
        if (scale <= 0f)
            scale = 1f;
        scale *= 1.5f;

        _globalTime += (float)_timing.FrameTime.TotalSeconds;
        float alpha = GetAlpha();

        switch (_type)
        {
            case ScreenAnnounceTarget.FirstDeploy:
                DrawFirstDeployAnnounce(handle, screenSize, scale, alpha);
                break;

            case ScreenAnnounceTarget.AllMarines:
                DrawCenteredAnnouncement(
                    handle, 
                    screenSize, 
                    scale, 
                    Color.LimeGreen, 
                    alpha,
                    _startingMessage
                );
                break;

            case ScreenAnnounceTarget.SquadOnly:
                if (_squad != null && 
                    _entMan.TryGetComponent<SquadMemberComponent>(player, out var squadComp) && 
                    squadComp.Squad == _squad &&
                    _entMan.TryGetComponent<SquadTeamComponent>(_squad, out var squadTeam))
                {
                    DrawCenteredAnnouncement(
                        handle,
                        screenSize,
                        scale,
                        squadTeam.Color,
                        alpha,
                        _startingMessage
                    );
                }
                break;
        }

        UpdateState((float)_timing.FrameTime.TotalSeconds);
    }

    private void DrawFirstDeployAnnounce(DrawingHandleScreen handle, Vector2i screenSize, float scale, float alpha)
    {
        var lineHeight = _lineHeightUnscaled * scale;
        var totalHeight = _announceText.Length * lineHeight;
        var padding = 20f * scale;
        var baseX = padding;
        var baseY = screenSize.Y - totalHeight - padding;

        for (int i = 0; i <= _currentLine && i < _announceText.Length; i++)
        {
            var text = BuildVisibleText(i, alpha);
            var offset = _random.NextVector2(-_shakeIntensity, _shakeIntensity) * alpha;
            var textPos = new Vector2(baseX, baseY + i * lineHeight) + offset;

            DrawTextWithOutline(handle, text, textPos, scale, GetTextColor(i, alpha), alpha);
        }
    }

    private void DrawCenteredAnnouncement(DrawingHandleScreen handle, Vector2i screenSize, float scale, Color color, float alpha, string startingMessage)
    {
        var allLines = new List<string>();
        var lineHeight = _lineHeightUnscaled * scale;
        var maxWidth = screenSize.X * _maxTextWidthFraction;

        // Add starting message if it exists
        if (!string.IsNullOrEmpty(startingMessage))
        {
            var underlinedMessage = $"[u]{startingMessage}[/u]";
            allLines.AddRange(WrapText(new[] { underlinedMessage }, maxWidth, scale));
            allLines.Add(""); // Empty line as separator
        }

        // Add main announcement text
        allLines.AddRange(WrapText(_announceText, maxWidth, scale));

        // Calculate total height
        var totalHeight = allLines.Count * lineHeight;
        var baseY = (screenSize.Y - totalHeight) / 2f;

        for (int i = 0; i < allLines.Count; i++)
        {
            var line = allLines[i];
            if (string.IsNullOrEmpty(line))
                continue;

            var textWidth = MeasureTextWidth(_font, line, scale);
            var baseX = (screenSize.X - textWidth) / 2f;
            var offset = _random.NextVector2(-_shakeIntensity, _shakeIntensity) * alpha;
            var textPos = new Vector2(baseX, baseY + i * lineHeight) + offset;

            var lineColor = color;
            if (line.StartsWith("[u]") && line.EndsWith("[/u]"))
            {
                lineColor = color.WithAlpha(alpha * 0.8f); // Slightly transparent for underlined text
            }

            DrawTextWithOutline(handle, line, textPos, scale, lineColor.WithAlpha(alpha), alpha);
        }
    }

    private void DrawTextWithOutline(DrawingHandleScreen handle, string text, Vector2 position, float scale, Color color, float alpha)
    {
        // Draw outline (4 directions)
        var outlineColor = Color.Black.WithAlpha(alpha * 0.7f);
        var outlineOffset = 1f * scale;
        handle.DrawString(_font, position + new Vector2(-outlineOffset, 0), text, scale, outlineColor);
        handle.DrawString(_font, position + new Vector2(outlineOffset, 0), text, scale, outlineColor);
        handle.DrawString(_font, position + new Vector2(0, -outlineOffset), text, scale, outlineColor);
        handle.DrawString(_font, position + new Vector2(0, outlineOffset), text, scale, outlineColor);

        // Draw main text
        handle.DrawString(_font, position, text, scale, color);
    }

    private List<string> WrapText(string[] lines, float maxWidth, float scale)
    {
        var result = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                result.Add(line);
                continue;
            }

            // Handle special formatting (like underlines)
            bool isUnderlined = line.StartsWith("[u]") && line.EndsWith("[/u]");
            string cleanLine = isUnderlined ? line.Substring(3, line.Length - 7) : line;

            var words = cleanLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();

            foreach (var word in words)
            {
                var next = current.Length == 0 ? word : $"{current} {word}";
                var width = MeasureTextWidth(_font, isUnderlined ? $"[u]{next}[/u]" : next, scale);

                if (width > maxWidth)
                {
                    if (current.Length > 0)
                        result.Add(isUnderlined ? $"[u]{current}[/u]" : current.ToString());
                    current.Clear().Append(word);
                }
                else
                {
                    if (current.Length > 0)
                        current.Append(' ');
                    current.Append(word);
                }
            }

            if (current.Length > 0)
                result.Add(isUnderlined ? $"[u]{current}[/u]" : current.ToString());
        }

        return result;
    }

    private float GetAlpha()
    {
        if (!_finished)
            return MathF.Min(1f, _globalTime / _fadeDuration);

        if (_fadingOut)
            return MathF.Max(0f, 1f - (_globalTime - _holdStartTime - _holdDuration) / _fadeDuration);

        return 1f;
    }

    private string BuildVisibleText(int lineIndex, float alpha)
    {
        if (lineIndex < _currentLine)
            return _announceText[lineIndex];

        if (lineIndex > _currentLine)
            return string.Empty;

        int visibleChars = _currentChar;

        string fullText = _announceText[lineIndex];

        if (_random.Prob(_glitchChance * alpha))
        {
            var glitched = new StringBuilder();
            for (int i = 0; i < fullText.Length; i++)
            {
                if (i < visibleChars)
                    glitched.Append(_random.Prob(0.3f) ? (char)_random.Next(33, 126) : fullText[i]);
                else
                    glitched.Append(_random.Next(2) == 0 ? ' ' : (char)_random.Next(33, 126));
            }
            return glitched.ToString();
        }

        var sb = new StringBuilder();
        for (int i = 0; i < visibleChars && i < fullText.Length; i++)
        {
            sb.Append(_random.Prob(0.1f) && i > 0 ? ' ' : fullText[i]);
        }

        return sb.ToString();
    }

    private Color GetTextColor(int lineIndex, float alpha)
    {
        if (_random.Prob(_flickerChance * alpha))
            return Color.LimeGreen.WithAlpha(0.8f * alpha);

        if ((_globalTime + lineIndex * 0.2f) % 0.5f < 0.1f)
            return Color.LimeGreen.WithAlpha(0.9f * alpha);

        return Color.LimeGreen.WithAlpha(alpha);
    }

    private void UpdateState(float deltaTime)
    {
        if (_finished)
        {
            if (!_fadingOut && (_globalTime - _holdStartTime) > _holdDuration)
                _fadingOut = true;
            return;
        }

        _timer += deltaTime;
        if (_timer < _printSpeed)
            return;

        _timer = 0;

        if (_currentLine >= _announceText.Length)
        {
            _finished = true;
            _holdStartTime = _globalTime;
            return;
        }

        var line = _announceText[_currentLine];

        if (_currentChar < line.Length)
        {
            _currentChar++;

            if (_random.Prob(0.05f) && _currentChar > 5)
                _timer = -_random.NextFloat(0.1f, 0.5f);
        }
        else
        {
            _currentLine++;
            _currentChar = 0;
            _timer = -_random.NextFloat(0.2f, 0.8f);
        }
    }

    public static float MeasureTextWidth(Font font, string text, float scale)
    {
        float total = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            if (font.TryGetCharMetrics(rune, scale, out var metrics))
            {
                total += metrics.Advance;
            }
        }

        return total;
    }
}