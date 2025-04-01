using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Lifesteal;

public sealed class XenoLifestealSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _rmcEmote = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly INetManager _net = default!;

    private readonly HashSet<Entity<MarineComponent>> _targets = new();

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<XenoNestedComponent> _xenoNestedQuery;

    public override void Initialize()
    {
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _xenoNestedQuery = GetEntityQuery<XenoNestedComponent>();

        SubscribeLocalEvent<XenoLifestealComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<XenoLifestealComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!_xeno.CanHeal(xeno.Owner))
            return;

        var found = false;
        foreach (var hit in args.HitEntities)
        {
            if (!_marineQuery.HasComp(hit))
                continue;

            if (_xenoNestedQuery.HasComp(hit))
                continue;

            if (_mobStateQuery.TryComp(hit, out var mobState) &&
                mobState.CurrentState == MobState.Dead)
            {
                continue;
            }

            found = true;
            break;
        }

        if (!found)
            return;

        if (xeno.Comp.Emote is { } emote)
            _rmcEmote.TryEmoteWithChat(xeno, emote, cooldown: xeno.Comp.EmoteCooldown);

        if (!_damageableQuery.TryComp(xeno, out var damageable))
            return;

        var total = damageable.TotalDamage;
        if (total == FixedPoint2.Zero)
            return;

        _targets.Clear();
        _entityLookup.GetEntitiesInRange(xeno.Owner.ToCoordinates(), xeno.Comp.TargetRange, _targets);

        var lifesteal = xeno.Comp.BasePercentage;
        foreach (var hit in _targets)
        {
            if (!_marineQuery.HasComp(hit))
                continue;

            if (_xenoNestedQuery.HasComp(hit))
                continue;

            if (_mobStateQuery.TryComp(hit, out var mobState) &&
                mobState.CurrentState == MobState.Dead)
            {
                continue;
            }

            lifesteal += xeno.Comp.TargetIncreasePercentage;
            if (lifesteal >= xeno.Comp.MaxPercentage)
            {
                lifesteal = xeno.Comp.MaxPercentage;
                break;
            }
        }

        var amount = -FixedPoint2.Clamp(total * lifesteal, xeno.Comp.MinHeal, xeno.Comp.MaxHeal);
        var heal = _rmcDamageable.DistributeTypes(xeno.Owner, amount);
        _damageable.TryChangeDamage(xeno, heal, true, origin: xeno, tool: xeno);

        if (lifesteal >= xeno.Comp.MaxPercentage)
        {
            var marines = Filter.PvsExcept(xeno).RemoveWhereAttachedEntity(e => !_marineQuery.HasComp(e));
            var marineMsg = Loc.GetString("rmc-lifesteal-more-marine", ("xeno", xeno.Owner));
            _popup.PopupEntity(marineMsg, xeno, marines, true, PopupType.SmallCaution);

            var selfMsg = Loc.GetString("rmc-lifesteal-more-self");
            _popup.PopupClient(selfMsg, xeno, xeno);
            _aura.GiveAura(xeno, xeno.Comp.AuraColor, TimeSpan.FromSeconds(1));

            if (_net.IsServer && xeno.Comp.MaxEffect is { } effect)
                SpawnAttachedTo(effect, xeno.Owner.ToCoordinates());
        }
    }
}
