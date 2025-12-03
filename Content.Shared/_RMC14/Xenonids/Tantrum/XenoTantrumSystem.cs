using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared.Popups;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Mobs.Systems;
using Content.Shared._RMC14.Xenonids.Strain;
using Robust.Shared.Timing;
using Content.Shared._RMC14.Aura;
using Robust.Shared.Audio.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared._RMC14.Armor;
using Robust.Shared.Network;
using Content.Shared.Coordinates;

namespace Content.Shared._RMC14.Xenonids.Tantrum;

public sealed class XenoTantrumSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly XenoStrainSystem _strain = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly XenoEnergySystem _energy = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTantrumComponent, XenoTantrumActionEvent>(OnXenoTantrumAction);

        SubscribeLocalEvent<TantrumingComponent, ComponentStartup>(OnTantrumingAdded);
        SubscribeLocalEvent<TantrumingComponent, ComponentShutdown>(OnTantrumingRemoved);
        SubscribeLocalEvent<TantrumingComponent, RefreshMovementSpeedModifiersEvent>(OnTantrumingRefreshSpeed);
        SubscribeLocalEvent<TantrumingComponent, CMGetArmorEvent>(OnTantrumingGetArmor);
    }

    private void OnTantrumingAdded(Entity<TantrumingComponent> xeno, ref ComponentStartup args)
    {
        if (HasComp<TantrumSpeedBuffComponent>(xeno))
            _speed.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnTantrumingRemoved(Entity<TantrumingComponent> xeno, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(xeno))
            return;

        if (HasComp<TantrumSpeedBuffComponent>(xeno))
            _speed.RefreshMovementSpeedModifiers(xeno);

        if (_timing.IsFirstTimePredicted)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-tantrum-end"), xeno, xeno, PopupType.SmallCaution);
    }
    private void OnTantrumingRefreshSpeed(Entity<TantrumingComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<TantrumSpeedBuffComponent>(xeno, out var speedBuff) || !xeno.Comp.Running)
            return;

        var modifier = speedBuff.SpeedIncrease;

        args.ModifySpeed(modifier, modifier);
    }

    private void OnTantrumingGetArmor(Entity<TantrumingComponent> xeno, ref CMGetArmorEvent args)
    {
        if (HasComp<TantrumSpeedBuffComponent>(xeno) || !xeno.Comp.Running)
            return;

        args.XenoArmor += xeno.Comp.ArmorGain;
    }

    private void OnXenoTantrumAction(Entity<XenoTantrumComponent> xeno, ref XenoTantrumActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_transform.InRange(xeno.Owner, args.Target, xeno.Comp.Range))
            return;

        if (HasComp<TantrumingComponent>(xeno))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-raging-self"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<XenoComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-not-xeno"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (xeno.Owner == args.Target)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-self"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (!_hive.FromSameHive(xeno.Owner, args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-wrong-hive"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (_mob.IsDead(args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-dead"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (_strain.AreSameStrain(xeno.Owner, args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-valkyrie"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (HasComp<TantrumingComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-fail-raging", ("target", args.Target)), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (!_energy.TryRemoveEnergyPopup(xeno.Owner, xeno.Comp.FuryCost) || !_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var time = _timing.CurTime;
        var ourRage = EnsureComp<TantrumingComponent>(xeno);
        ourRage.ArmorGain = xeno.Comp.SelfArmorBoost;
        ourRage.ExpireAt = time + xeno.Comp.SelfArmorDuration;
        _popup.PopupClient(Loc.GetString("rmc-xeno-tantrum-self"), xeno, xeno, PopupType.MediumCaution);
        _audio.PlayPredicted(xeno.Comp.BuffSound, xeno, xeno);
        _aura.GiveAura(xeno, xeno.Comp.EnrageColor, xeno.Comp.SelfArmorDuration);
        _armor.UpdateArmorValue(xeno.Owner);

        var otherDuration = HasComp<TantrumSpeedBuffComponent>(args.Target) ? xeno.Comp.OtherSpeedDuration : xeno.Comp.OtherArmorDuration;
        var otherRage = EnsureComp<TantrumingComponent>(args.Target);
        otherRage.ExpireAt = time + otherDuration;
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-tantrum-other"), args.Target, args.Target, PopupType.MediumCaution);
        _audio.PlayPredicted(xeno.Comp.BuffSound, args.Target, xeno);
        _aura.GiveAura(args.Target, xeno.Comp.EnrageColor, otherDuration);
        _armor.UpdateArmorValue(args.Target);
        _speed.RefreshMovementSpeedModifiers(args.Target);

        if (_net.IsClient)
            return;

        SpawnAttachedTo(xeno.Comp.EnrageEffect, xeno.Owner.ToCoordinates());
        SpawnAttachedTo(xeno.Comp.EnrageEffect, args.Target.ToCoordinates());
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;
        var time = _timing.CurTime;

        var tantrumQuery = EntityQueryEnumerator<TantrumingComponent>();

        while (tantrumQuery.MoveNext(out var uid, out var tantrum))
        {
            if (time < tantrum.ExpireAt)
                return;

            RemCompDeferred<TantrumingComponent>(uid);
            _speed.RefreshMovementSpeedModifiers(uid);
            _armor.UpdateArmorValue(uid);
        }
    }
}
