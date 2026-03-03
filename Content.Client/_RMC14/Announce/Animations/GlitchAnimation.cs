using Content.Shared._RMC14.Announce;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;

namespace Content.Client._RMC14.Announce.Animations;

public sealed class GlitchAnimation : IAnnouncementAnimation
{
    private static readonly char[] GlitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~".ToCharArray();
    private const float MinTickInterval = 0.005f;
    private const int MaxAdvancePerUpdate = 5;

    public void Reset(AnnouncementAnimationContext context)
    {
        context.State.CurrentLine = 0;
        context.State.CurrentChar = 0;
        context.State.GlitchTimer = 0f;
        context.State.TypewriterTimer = 0f;
        ResetVisualState(context);

        for (var i = context.TitleOffset; i < context.Labels.Length; i++)
        {
            context.Labels[i].SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
        }
    }

    public AnnouncementAnimationStatus Update(AnnouncementAnimationContext context, float deltaTime)
    {
        var style = context.Style;
        var intensity = GetIntensity(style.AnimationConfig.GlitchChance);

        if (context.State.TypewriterTimer > 0f)
        {
            context.State.TypewriterTimer = MathF.Max(0f, context.State.TypewriterTimer - deltaTime);
        }
        else if (RandomChance(context.Random, GetBurstStartChancePerFrame(intensity, deltaTime)))
        {
            context.State.TypewriterTimer = context.Random.NextFloat(0.05f, 0.14f);
        }

        var burstActive = context.State.TypewriterTimer > 0f;
        var printInterval = MathF.Max(
            MinTickInterval,
            style.AnimationConfig.PrintSpeed * (burstActive ? 0.22f : 0.60f));

        context.State.GlitchTimer += deltaTime;
        var advanced = 0;

        while (context.State.GlitchTimer >= printInterval && advanced < MaxAdvancePerUpdate)
        {
            context.State.GlitchTimer -= printInterval;
            advanced++;

            var finished = Advance(context, burstActive, intensity);
            if (finished)
            {
                UpdateDisplay(context, intensity, burstActive);
                UpdateVisual(context, intensity, burstActive, deltaTime);
                return AnnouncementAnimationStatus.Finished;
            }
        }

        if (advanced > 0 || burstActive || RandomChance(context.Random, 0.08f + intensity * 0.25f))
            UpdateDisplay(context, intensity, burstActive);

        UpdateVisual(context, intensity, burstActive, deltaTime);

        return AnnouncementAnimationStatus.Running;
    }

    private static bool Advance(AnnouncementAnimationContext context, bool burstActive, float intensity)
    {
        var cleanText = context.CleanText;
        var currentLine = context.State.CurrentLine;
        var currentChar = context.State.CurrentChar;

        if (currentLine >= cleanText.Length)
            return true;

        var lineText = cleanText[currentLine];
        if (currentChar >= lineText.Length)
        {
            context.State.CurrentLine++;
            context.State.CurrentChar = 0;
            return context.State.CurrentLine >= cleanText.Length;
        }

        if (burstActive && currentChar > 0 && RandomChance(context.Random, 0.04f + intensity * 0.10f))
            context.State.CurrentChar--;

        var advanceBy = 1;
        if (burstActive && RandomChance(context.Random, 0.18f + intensity * 0.35f))
            advanceBy += context.Random.Next(1, 3);

        context.State.CurrentChar = Math.Min(context.State.CurrentChar + advanceBy, lineText.Length);
        return false;
    }

    private static void UpdateDisplay(AnnouncementAnimationContext context, float intensity, bool burstActive)
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
            if (textIndex < context.State.CurrentLine)
            {
                var visible = originalText[textIndex];
                if (RandomChance(context.Random, lineGlitchChance))
                {
                    var glitched = CreateGlitchedText(cleanText[textIndex], context.Random, charGlitchChance);
                    visible = glitched.Length > 0 ? glitched : visible;
                }

                var message = context.FormatMessage(visible, style);
                context.Labels[i].SetMessage(message);
            }
            else if (textIndex == context.State.CurrentLine)
            {
                var currentLineText = cleanText[textIndex];
                var maxLength = Math.Min(context.State.CurrentChar, currentLineText.Length);
                var partialText = currentLineText.Substring(0, maxLength);
                if (maxLength > 0 && RandomChance(context.Random, lineGlitchChance + 0.10f))
                    partialText = CreateGlitchedText(partialText, context.Random, charGlitchChance);

                var message = context.FormatMessage(partialText, style);
                context.Labels[i].SetMessage(message);
            }
            else
            {
                context.Labels[i].SetMessage(FormattedMessage.FromMarkupPermissive(string.Empty));
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
            if (char.IsWhiteSpace(chars[i]) || !RandomChance(random, charGlitchChance))
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

    private static bool RandomChance(IRobustRandom random, float probability)
    {
        return random.NextFloat() < probability;
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

        if (RandomChance(context.Random, jitterChance))
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
        if (RandomChance(context.Random, flickerChance))
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
