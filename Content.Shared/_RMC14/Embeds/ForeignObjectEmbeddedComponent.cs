using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Embeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedForeignObjectEmbeddedSystem), typeof(ForeignObjectEmbeddedUtility))]
public sealed partial class ForeignObjectEmbeddedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int StackCount;

    [DataField, AutoNetworkedField]
    public TimeSpan NextDamageAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public List<ForeignObjectEmbeddedEntry> Entries = new();
}

public static class ForeignObjectEmbeddedUtility
{
    public static int AddEntry(
        ForeignObjectEmbeddedComponent component,
        string sourceId,
        BodyPartType bodyPart,
        int quantity = 1,
        BodyPartSymmetry symmetry = BodyPartSymmetry.None)
    {
        if (quantity <= 0)
            return component.StackCount;

        component.Entries.Add(new ForeignObjectEmbeddedEntry
        {
            SourceId = sourceId,
            BodyPart = bodyPart,
            Symmetry = symmetry,
            Quantity = quantity,
        });

        component.StackCount += quantity;
        return component.StackCount;
    }

    public static void SetNextDamageAt(ForeignObjectEmbeddedComponent component, TimeSpan nextDamageAt)
    {
        component.NextDamageAt = nextDamageAt;
    }

    public static void InitializeTickState(ForeignObjectEmbeddedComponent component, TimeSpan now)
    {
        if (component.NextDamageAt == TimeSpan.Zero)
            component.NextDamageAt = now + TimeSpan.FromSeconds(1);

    }

    public static List<ForeignObjectEmbeddedEntry> GetEmbeddedObjectInBodyParts(ForeignObjectEmbeddedComponent component)
    {
        return component.Entries
            .GroupBy(entry => new { entry.BodyPart, entry.Symmetry })
            .Select(group => new ForeignObjectEmbeddedEntry
            {
                SourceId = group.First().SourceId,
                BodyPart = group.Key.BodyPart,
                Symmetry = group.Key.Symmetry,
                Quantity = group.Sum(entry => entry.Quantity),
            })
            .ToList();
    }

    public static bool TryRemoveMatchingBodyPart(ForeignObjectEmbeddedComponent component, BodyPartType bodyPart, BodyPartSymmetry symmetry, int quantity = 1)
    {
        if (quantity <= 0)
            return false;

        var remaining = quantity;
        for (var i = component.Entries.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var entry = component.Entries[i];
            if (entry.BodyPart != bodyPart || entry.Symmetry != symmetry)
                continue;

            if (entry.Quantity <= remaining)
            {
                remaining -= entry.Quantity;
                component.Entries.RemoveAt(i);
                continue;
            }

            entry.Quantity -= remaining;
            component.Entries[i] = entry;
            remaining = 0;
        }

        if (remaining > 0)
            return false;

        component.StackCount -= quantity;
        if (component.StackCount <= 0)
            component.StackCount = 0;

        return true;
    }

    public static (BodyPartType BodyPart, BodyPartSymmetry Symmetry) SelectRandomBodyPartAndSymmetry()
    {
        var parts = Enum.GetValues<BodyPartType>()
            .Where(x => x != BodyPartType.Other && x != BodyPartType.Tail)//surgery system doesnt handle tails yet, so ignoring for now.
            .ToArray();

        var bodyPart = parts[System.Random.Shared.Next(parts.Length)];
        var symmetry = bodyPart switch
        {
            BodyPartType.Arm or BodyPartType.Hand or BodyPartType.Leg or BodyPartType.Foot =>
                System.Random.Shared.NextDouble() < 0.5 ? BodyPartSymmetry.Left : BodyPartSymmetry.Right,
            _ => BodyPartSymmetry.None,
        };

        return (bodyPart, symmetry);
    }

    public static BodyPartType SelectRandomBodyPart()
    {
        return SelectRandomBodyPartAndSymmetry().BodyPart;
    }
}

[Serializable, NetSerializable]
public sealed class ForeignObjectEmbeddedEntry
{
    public string SourceId { get; set; } = string.Empty;

    public BodyPartType BodyPart { get; set; } = BodyPartType.Torso;

    public BodyPartSymmetry Symmetry { get; set; } = BodyPartSymmetry.None;

    public int Quantity { get; set; } = 1;
}
