using Content.Shared.Interaction.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Whitelist;

namespace Content.Server._RMC14.Abilities;

public sealed class ApplyToAreaOnActivateSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ApplyStatusToAreaOnActivateComponent, UseInHandEvent>(OnUsed);
    }

    /// <summary>
    /// After being used, triggers this status effect
    /// Doesn't care if it's already handled, and doesn't handle it
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    public void OnUsed(Entity<ApplyStatusToAreaOnActivateComponent> ent, ref UseInHandEvent args)
    {
        var transform = Transform(args.User);
        foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, ent.Comp.Range))
        {
            if (entity == args.User && !ent.Comp.ApplyToSelf)
                continue;

            if (_whitelistSystem.IsBlacklistPass(ent.Comp.Blacklist, entity) || _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, entity))
                continue;

            if(ent.Comp.Component != String.Empty)
                _statusEffect.TryAddStatusEffect(entity, ent.Comp.Key, TimeSpan.FromSeconds(ent.Comp.Time), true, ent.Comp.Component);
        }
    }
}
