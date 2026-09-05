using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using static Content.Shared.Popups.PopupType;

namespace Content.Shared._RMC14.Power;

public sealed partial class RMCPortableGeneratorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPortableGeneratorComponent, InteractHandEvent>(OnPortableGeneratorInteractHand);
        SubscribeLocalEvent<RMCPortableGeneratorComponent, RMCPortableGeneratorStartDoAfterEvent>(OnPortableGeneratorStartDoAfter);
        SubscribeLocalEvent<RMCPortableGeneratorComponent, ExaminedEvent>(OnPortableGeneratorExamined);
        SubscribeLocalEvent<RMCPortableGeneratorComponent, AnchorStateChangedEvent>(OnPortableGeneratorAnchorChanged);
        SubscribeLocalEvent<RMCPortableGeneratorComponent, MaterialAmountChangedEvent>(OnPortableGeneratorMaterialAmountChanged);

        Subs.BuiEvents<RMCPortableGeneratorComponent>(RMCPortableGeneratorUiKey.Key,
            subs =>
            {
                subs.Event<RMCPortableGeneratorToggleBuiMsg>(OnPortableGeneratorToggle);
                subs.Event<RMCPortableGeneratorEjectFuelBuiMsg>(OnPortableGeneratorEjectFuel);
                subs.Event<RMCPortableGeneratorRaisePowerBuiMsg>(OnPortableGeneratorRaisePower);
                subs.Event<RMCPortableGeneratorLowerPowerBuiMsg>(OnPortableGeneratorLowerPower);
            });
    }

    private void OnPortableGeneratorInteractHand(Entity<RMCPortableGeneratorComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;

        if (HasComp<XenoComponent>(user) && HasComp<MeleeWeaponComponent>(user))
        {
            if (_sizeStun.TryGetSize(user, out var size) && size < RMCSizes.Xeno)
            {
                _popup.PopupClient(Loc.GetString("rmc-portable-generator-xeno-too-small", ("generator", ent)), ent, user, SmallCaution);
                args.Handled = true;
                return;
            }

            if (ent.Comp.On)
            {
                SetPortableGeneratorOn(ent, false);
                _popup.PopupEntity(Loc.GetString("rmc-portable-generator-xeno-off", ("generator", ent)), ent, SmallCaution);
            }
            else if (Transform(ent).Anchored)
            {
                _transform.Unanchor(ent, Transform(ent));
                _popup.PopupEntity(Loc.GetString("rmc-portable-generator-xeno-unanchor", ("generator", ent)), ent, SmallCaution);
            }

            args.Handled = true;
        }
    }

    private void OnPortableGeneratorStartDoAfter(Entity<RMCPortableGeneratorComponent> ent, ref RMCPortableGeneratorStartDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.On)
            return;

        if (!Transform(ent).Anchored)
            return;

        var empty = _materialStorage.GetMaterialAmount(ent, ent.Comp.Material) <= 0;

        if (empty)
            return;

        if (!_net.IsServer)
            return;

        SetPortableGeneratorOn(ent, true);
        _audio.PlayPvs(ent.Comp.StartSound, ent);
        _popup.PopupClient(Loc.GetString("rmc-portable-generator-start-success", ("generator", ent)), ent, args.User);
    }

    private void OnPortableGeneratorExamined(Entity<RMCPortableGeneratorComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCPortableGeneratorComponent)))
        {
            if (ent.Comp.On)
                args.PushMarkup(Loc.GetString("rmc-portable-generator-examine-on"));
            else
                args.PushMarkup(Loc.GetString("rmc-portable-generator-examine-off"));

            args.PushMarkup(Loc.GetString("rmc-portable-generator-examine-fuel",
                ("sheets", _materialStorage.GetMaterialAmount(ent, ent.Comp.Material) / ent.Comp.MaterialPerSheet),
                ("fuel", ent.Comp.FuelName),
                ("watts", ent.Comp.Watts)));

            if (ent.Comp.CritFail)
                args.PushMarkup(Loc.GetString("rmc-portable-generator-examine-crit"));
        }
    }

    private void OnPortableGeneratorAnchorChanged(Entity<RMCPortableGeneratorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!args.Anchored && ent.Comp.On)
            SetPortableGeneratorOn(ent, false);

        Dirty(ent);
    }

    private void OnPortableGeneratorToggle(Entity<RMCPortableGeneratorComponent> ent, ref RMCPortableGeneratorToggleBuiMsg args)
    {
        var user = args.Actor;

        if (ent.Comp.On)
        {
            SetPortableGeneratorOn(ent, false);
            return;
        }

        if (!Transform(ent).Anchored)
        {
            _popup.PopupClient(Loc.GetString("rmc-portable-generator-not-anchored", ("generator", ent)), ent, user, SmallCaution);
            return;
        }

        if (_materialStorage.GetMaterialAmount(ent, ent.Comp.Material) <= 0 && ent.Comp.FractionalMaterial <= 0)
            return;

        var ev = new RMCPortableGeneratorStartDoAfterEvent();
        var delay = ent.Comp.StartDelay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnPortableGeneratorEjectFuel(Entity<RMCPortableGeneratorComponent> ent, ref RMCPortableGeneratorEjectFuelBuiMsg args)
    {
        RaiseLocalEvent(ent, RMCGeneratorEmpty.Instance);
    }

    private void OnPortableGeneratorRaisePower(Entity<RMCPortableGeneratorComponent> ent, ref RMCPortableGeneratorRaisePowerBuiMsg args)
    {
        if (ent.Comp.PowerGenPercent >= ent.Comp.MaxPowerPercent)
            return;

        ent.Comp.PowerGenPercent = Math.Min(ent.Comp.PowerGenPercent + ent.Comp.PowerPercentStep, ent.Comp.MaxPowerPercent);
        Dirty(ent);
    }

    private void OnPortableGeneratorLowerPower(Entity<RMCPortableGeneratorComponent> ent, ref RMCPortableGeneratorLowerPowerBuiMsg args)
    {
        if (ent.Comp.PowerGenPercent <= ent.Comp.MinPowerPercent)
            return;

        ent.Comp.PowerGenPercent = Math.Max(ent.Comp.PowerGenPercent - ent.Comp.PowerPercentStep, ent.Comp.MinPowerPercent);
        Dirty(ent);
    }

    private void OnPortableGeneratorMaterialAmountChanged(Entity<RMCPortableGeneratorComponent> ent, ref MaterialAmountChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        Dirty(ent);
    }

    private void SetPortableGeneratorOn(Entity<RMCPortableGeneratorComponent> ent, bool on)
    {
        ent.Comp.On = on;
        Dirty(ent);

        // TODO: Needs sprite implementation
        _appearance.SetData(ent, RMCPortableGeneratorVisuals.Running, on);
        _ambientSound.SetAmbience(ent, on);
    }

    public void StopPortableGenerator(Entity<RMCPortableGeneratorComponent> ent)
    {
        if (!ent.Comp.On)
            return;

        SetPortableGeneratorOn(ent, false);
    }
}

public sealed class RMCGeneratorEmpty
{
    public static readonly RMCGeneratorEmpty Instance = new();
}
