using Content.Shared._RMC14.Comms;
using Content.Shared._RMC14.Marines;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Comms;

public abstract class SharedCommsEncryptionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CommsEncryptionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CommsEncryptionComponent> ent, ref MapInitEvent args)
    {
        // Initialize clarity to 100% with grace period
        ent.Comp.Clarity = 1.0f;
        ent.Comp.HasGracePeriod = true;
        ent.Comp.GracePeriodEnd = _timing.CurTime + ent.Comp.GracePeriodDuration;
        ent.Comp.LastDecryptionTime = _timing.CurTime;
        ent.Comp.DegradationStartTime = _timing.CurTime;
        Dirty(ent);
    }

    public float GetGarblePercentage(CommsEncryptionComponent comp)
    {
        // Garble = 1 - Clarity, clamped to 0-55%
        return Math.Clamp(1f - comp.Clarity, 0f, 1f - comp.MinClarity);
    }

    public string GetClarityDescription(CommsEncryptionComponent comp)
    {
        var clarityPercent = comp.Clarity * 100f;
        if (clarityPercent >= 90f)
            return "excellent";
        if (clarityPercent >= 75f)
            return "good";
        if (clarityPercent >= 60f)
            return "fair";
        if (clarityPercent >= 45f)
            return "poor";
        return "terrible";
    }

    public int GetKnownPongLetters(CommsEncryptionComponent comp)
    {
        var clarityPercent = comp.Clarity * 100f;

        if (clarityPercent >= 90f)
            return 4;
        if (clarityPercent >= 75f)
            return 3;
        if (clarityPercent >= 60f)
            return 2;
        if (clarityPercent > 45f)
            return 1;
        return 0;
    }

    public string GarbleMessage(string message, float garblePercent)
    {
        if (garblePercent <= 0f)
            return message;

        var chars = message.ToCharArray();
        var garbledChars = (int)(chars.Length * garblePercent);
        var indices = new List<int>();

        // Select random characters to garble, skipping markup
        bool inTag = false;
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '[')
                inTag = true;
            else if (chars[i] == ']')
                inTag = false;
            else if (!inTag && char.IsLetterOrDigit(chars[i]))
                indices.Add(i);
        }

        // Shuffle and take first N
        var random = new System.Random();
        indices = indices.OrderBy(_ => random.Next()).Take(garbledChars).ToList();

        foreach (var index in indices)
        {
            if (char.IsLetter(chars[index]))
            {
                // Replace with random letter
                chars[index] = (char)('A' + random.Next(26));
            }
            else if (char.IsDigit(chars[index]))
            {
                // Replace with random digit
                chars[index] = (char)('0' + random.Next(10));
            }
        }

        return new string(chars);
    }

    public void RestoreClarity(Entity<CommsEncryptionComponent> ent, bool fullRestore = true)
    {
        if (fullRestore)
        {
            ent.Comp.Clarity = ent.Comp.MaxClarity;
            ent.Comp.LastDecryptionTime = _timing.CurTime;
        }

        ent.Comp.DegradationStartTime = _timing.CurTime;
        Dirty(ent);
    }
}
