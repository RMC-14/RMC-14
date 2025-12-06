using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.Chemistry.Reagent;

public sealed class RMCReagentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private FrozenDictionary<string, Reagent> _reagents = FrozenDictionary<string, Reagent>.Empty;

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ReloadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<ReagentPrototype>())
            ReloadPrototypes();
    }

    private void ReloadPrototypes()
    {
        var dict = new Dictionary<string, Reagent>();
        foreach (var reagentProto in _prototypes.EnumeratePrototypes<ReagentPrototype>())
        {
            object? reagentObj = new Reagent();
            _serialization.CopyTo(reagentProto, ref reagentObj);
            if (reagentObj is not Reagent reagent)
                continue;

            dict[reagentProto.ID] = reagent;
        }

        _reagents = dict.ToFrozenDictionary();
    }

    public Reagent Index(ProtoId<ReagentPrototype> id)
    {
        return _reagents[id];
    }

    public bool TryIndex(ProtoId<ReagentPrototype> id, [NotNullWhen(true)] out Reagent? reagent)
    {
        return _reagents.TryGetValue(id, out reagent);
    }

    public bool TryIndex(ReagentId id, [NotNullWhen(true)] out Reagent? reagent)
    {
        return _reagents.TryGetValue(id.Prototype, out reagent);
    }

    public bool CanBeIngested(ReagentId reagentId)
    {
        if (!TryIndex(reagentId, out var reagent) ||
            reagent.Metabolisms is not { } metabolisms)
        {
            return true;
        }

        foreach (var metabolism in metabolisms.Values)
        {
            foreach (var effect in metabolism.Effects)
            {
                if (effect is RMCChemicalEffect rmcEffect &&
                    rmcEffect.CanBeIngested())
                {
                    return false;
                }
            }
        }

        return true;
    }

    public FixedPoint2 GetMetabolismModifier(EntityEffect[] effects)
    {
        var multiplier = FixedPoint2.New(1);
        foreach (var effect in effects)
        {
            if (effect is RMCChemicalEffect rmcEffect)
                multiplier *= rmcEffect.GetMetabolismModifier();
        }

        return multiplier;
    }

    public bool CanMetabolize(EntityUid target, EntityEffect[] effects)
    {
        foreach (var effect in effects)
        {
            if (effect is RMCChemicalEffect rmcEffect &&
                !rmcEffect.CanMetabolize(target))
            {
                return false;
            }
        }

        return true;
    }
}
