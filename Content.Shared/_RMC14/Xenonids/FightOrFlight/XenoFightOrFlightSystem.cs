using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Coordinates;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.FightOrFlight;

public sealed class XenoFightOrFlightSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoEnergySystem _energy = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<XenoComponent>> _xenos = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoFightOrFlightComponent, XenoFightOrFlightActionEvent>(OnFightOrFlightAction);
    }

    private void OnFightOrFlightAction(Entity<XenoFightOrFlightComponent> xeno, ref XenoFightOrFlightActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        if (!TryComp<XenoEnergyComponent>(xeno, out var energy))
            return;

        args.Handled = true;

        _audio.PlayPredicted(xeno.Comp.RoarSound, xeno, xeno);

        var highFury = _energy.HasEnergy((xeno.Owner, energy), xeno.Comp.FuryThreshold);

        _xenos.Clear();
        _entityLookup.GetEntitiesInRange(xeno.Owner.ToCoordinates(), (highFury ? xeno.Comp.HighRange : xeno.Comp.LowRange), _xenos);

        if (_net.IsServer)
            SpawnAttachedTo((highFury ? xeno.Comp.RoarEffect : xeno.Comp.WeakRoarEffect), xeno.Owner.ToCoordinates());

        foreach (var otherXeno in _xenos)
        {
            if (!_hive.FromSameHive(xeno.Owner, otherXeno.Owner))
                continue;

            foreach (var status in xeno.Comp.AilmentsRemove)
            {
                _status.TryRemoveStatusEffect(otherXeno, status);
            }

            EntityManager.RemoveComponents(otherXeno, xeno.Comp.ComponentsRemove);

            _jitter.DoJitter(otherXeno, xeno.Comp.Jitter, true, 80, 8, true);

            if (_net.IsServer)
            {
                SpawnAttachedTo(xeno.Comp.HealEffect, otherXeno.Owner.ToCoordinates());
                _popup.PopupEntity(Loc.GetString("rmc-xeno-fof-effect"), otherXeno, otherXeno, PopupType.SmallCaution);
            }
        }
    }
}
