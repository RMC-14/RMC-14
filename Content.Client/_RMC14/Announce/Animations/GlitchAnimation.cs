using System.Collections.Generic;
using Content.Shared._RMC14.Announce.Animations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class GlitchAnimation : IAnnouncementAnimation
{
    private static readonly char[] GlitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~".ToCharArray();
    private const float MinTickInterval = 0.005f;
    private const int MaxAdvancePerUpdate = 5;

    private readonly GlitchAnimationConfig _config;
    private int _currentLine;
    private int _currentChar;
    private float _glitchTimer;
    private float _burstTimer;

    public GlitchAnimation(GlitchAnimationConfig config) => _config = config;

    public void Reset(AnnouncementAnimationContext context)
    {
        _currentLine = 0;
        _currentChar = 0;
        _glitchTimer = 0f;
        _burstTimer = 0f;
        ResetVisualState(context);

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            (context.Labels[i] as RichTextLabel)?.SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
        }
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var intensity = GetIntensity(_config.GlitchChance);

        if (_burstTimer > 0f)
        {
            _burstTimer = MathF.Max(0f, _burstTimer - deltaTime);
        }
        else if (context.Random.Prob(GetBurstStartChancePerFrame(intensity, deltaTime)))
        {
            _burstTimer = context.Random.NextFloat(0.05f, 0.14f);
        }

        var burstActive = _burstTimer > 0f;
        var printInterval = MathF.Max(
            MinTickInterval,
            _config.PrintSpeed * (burstActive ? 0.22f : 0.60f));

        _glitchTimer += deltaTime;
        var advanced = 0;

        while (_glitchTimer >= printInterval && advanced < MaxAdvancePerUpdate)
        {
            _glitchTimer -= printInterval;
            advanced++;

            var finished = Advance(context, burstActive, intensity);
            if (finished)
            {
                UpdateDisplay(context, intensity, burstActive);
                UpdateVisual(context, intensity, burstActive, deltaTime);
                return AnnouncementAnimationStatus.Finished;
            }
        }

        if (advanced > 0 || burstActive || context.Random.Prob(0.08f + intensity * 0.25f))
            UpdateDisplay(context, intensity, burstActive);

        UpdateVisual(context, intensity, burstActive, deltaTime);

        return AnnouncementAnimationStatus.Running;
    }

    private bool Advance(AnnouncementAnimationContext context, bool burstActive, float intensity)
    {
        var cleanText = context.CleanText;

        if (_currentLine >= cleanText.Length)
            return true;

        var lineText = cleanText[_currentLine];
        if (_currentChar >= lineText.Length)
        {
            _currentLine++;
            _currentChar = 0;
            return _currentLine >= cleanText.Length;
        }

        if (burstActive && _currentChar > 0 && context.Random.Prob(0.04f + intensity * 0.10f))
            _currentChar--;

        var advanceBy = 1;
        if (burstActive && context.Random.Prob(0.18f + intensity * 0.35f))
            advanceBy += context.Random.Next(1, 3);

        _currentChar = Math.Min(_currentChar + advanceBy, lineText.Length);
        return false;
    }

    private void UpdateDisplay(AnnouncementAnimationContext context, float intensity, bool burstActive)
    {
        var originalText = context.OriginalText;
        var cleanText = context.CleanText;
        var style = context.Style;
        var lineGlitchChance = burstActive
            ? MathF.Min(0.90f, 0.35f + intensity * 0.45f)
            : MathF.Min(0.55f, 0.06f + intensity * 0.25f);
        var charGlitchChance = burstActive
            ? MathF.Min(0.60f, 0.08f + intensity * 0.35f)
            : MathF.Min(0.22f, 0.02f + intensity * 0.12f);

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            var textIndex = i - context.TitleOffset;
            if (textIndex < _currentLine)
            {
                var visible = originalText[textIndex];
                if (context.Random.Prob(lineGlitchChance))
                {
                    var glitched = CreateGlitchedText(cleanText[textIndex], context.Random, charGlitchChance);
                    visible = glitched.Length > 0 ? glitched : visible;
                }

                var message = context.FormatMessage(visible, style);
                (context.Labels[i] as RichTextLabel)?.SetMessage(message);
            }
            else if (textIndex == _currentLine)
            {
                var currentLineText = cleanText[textIndex];
                var maxLength = Math.Min(_currentChar, currentLineText.Length);
                var partialText = currentLineText[..maxLength];
                if (maxLength > 0 && context.Random.Prob(lineGlitchChance + 0.10f))
                    partialText = CreateGlitchedText(partialText, context.Random, charGlitchChance);

                var message = context.FormatMessage(partialText, style);
                (context.Labels[i] as RichTextLabel)?.SetMessage(message);
            }
            else
            {
                (context.Labels[i] as RichTextLabel)?.SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
            }
        }
    }

    private static string CreateGlitchedText(string text, IRobustRandom random, float charGlitchChance)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var chars = text.ToCharArray();
        var changed = false;

        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]) || !random.Prob(charGlitchChance))
                continue;

            chars[i] = GlitchChars[random.Next(GlitchChars.Length)];
            changed = true;
        }

        if (!changed)
            return text;

        return new string(chars);
    }

    private static float GetIntensity(float glitchChance)
    {
        return Math.Clamp(0.15f + glitchChance * 6f, 0.15f, 1f);
    }

    private static float GetBurstStartChancePerFrame(float intensity, float deltaTime)
    {
        var startsPerSecond = 0.9f + intensity * 3.6f;
        return Math.Clamp(startsPerSecond * deltaTime, 0f, 1f);
    }

    private static void ResetVisualState(AnnouncementAnimationContext context)
    {
        if (context.VisualContainer == null)
            return;

        context.VisualContainer.Margin = new Thickness(0f);
        ApplyVisualTintRecursive(context.VisualContainer, Color.White);
    }

    private static void UpdateVisual(AnnouncementAnimationContext context, float intensity, bool burstActive, float deltaTime)
    {
        var visual = context.VisualContainer;
        if (visual == null)
            return;

        var burstFactor = burstActive ? 1f : 0.45f;
        var jitterAmount = 2f + intensity * (burstActive ? 14f : 8f);
        var jitterChance = Math.Clamp((0.20f + intensity * 0.60f) * burstFactor * deltaTime * 60f, 0f, 1f);

        if (context.Random.Prob(jitterChance))
        {
            var jitterX = context.Random.NextFloat(-jitterAmount, jitterAmount);
            var jitterY = context.Random.NextFloat(-jitterAmount * 0.35f, jitterAmount * 0.35f);
            visual.Margin = new Thickness(jitterX, jitterY, 0f, 0f);
        }
        else
        {
            visual.Margin = new Thickness(0f);
        }

        var flickerChance = Math.Clamp((0.03f + intensity * 0.20f) * burstFactor * deltaTime * 60f, 0f, 1f);
        if (context.Random.Prob(flickerChance))
        {
            var tint = context.Random.NextFloat(0.70f, 1.0f);
            var tintColor = new Color(tint, MathF.Min(1f, tint * 1.08f), tint, 1f);
            ApplyVisualTintRecursive(visual, tintColor);
        }
        else
        {
            ApplyVisualTintRecursive(visual, Color.White);
        }
    }

    private static void ApplyVisualTintRecursive(Control root, Color tint)
    {
        var stack = new Stack<Control>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            node.Modulate = tint;

            if (node is AnimatedTextureRect animated)
            {
                var alpha = animated.DisplayRect.Modulate.A;
                animated.DisplayRect.Modulate = new Color(tint.R, tint.G, tint.B, alpha);
            }

            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }
    }
}
