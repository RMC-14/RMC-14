using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._RMC14.Speech.Hushed;

/// <summary>
/// System that handles adding/removing RMCHushedComponent when status effect is applied/removed.
/// </summary>
public sealed class RMCHushedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCHushedStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<RMCHushedStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
    }

    private void OnStatusEffectApplied(Entity<RMCHushedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<RMCHushedComponent>(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<RMCHushedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var effectComp))
            return;

        if (effectComp.AppliedTo == null)
            return;

        // Check if there are other RMCHushed status effects active
        if (!TryComp<StatusEffectContainerComponent>(effectComp.AppliedTo, out var container))
            return;

        foreach (var effect in container.ActiveStatusEffects)
        {
            if (effect == ent.Owner)
                continue;

            if (HasComp<RMCHushedStatusEffectComponent>(effect))
                return; // Another RMCHushed effect is still active
        }

        // Remove RMCHushedComponent from the target entity
        RemComp<RMCHushedComponent>(effectComp.AppliedTo.Value);
    }
}
