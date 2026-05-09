using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Sensor;

public sealed class SensorTowerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapIncludeXenosEvent>(OnTacticalMapIncludeXenos);

        SubscribeLocalEvent<SensorTowerComponent, MapInitEvent>(OnSensorTowerMapInit);
        SubscribeLocalEvent<SensorTowerComponent, InteractUsingEvent>(OnSensorTowerInteractUsing);
        SubscribeLocalEvent<SensorTowerComponent, InteractHandEvent>(OnSensorTowerInteractHand);
        SubscribeLocalEvent<SensorTowerComponent, ExaminedEvent>(OnSensorTowerExamined);
        SubscribeLocalEvent<SensorTowerComponent, SensorTowerRepairDoAfterEvent>(OnSensorTowerRepairDoAfter);
        SubscribeLocalEvent<SensorTowerComponent, SensorTowerDestroyDoAfterEvent>(OnSensorTowerDestroyDoAfter);
    }

    private void OnTacticalMapIncludeXenos(ref TacticalMapIncludeXenosEvent ev)
    {
        var towers = EntityQueryEnumerator<SensorTowerComponent>();
        while (towers.MoveNext(out var tower))
        {
            if (tower.State == SensorTowerState.On)
            {
                ev.Include = true;
                return;
            }
        }
    }

    private void OnSensorTowerMapInit(Entity<SensorTowerComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnSensorTowerInteractUsing(Entity<SensorTowerComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            var msg = Loc.GetString("rmc-skills-no-training", ("target", ent));
            _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            return;
        }

        var used = args.Used;

        if (TryComp<RMCDeviceBreakerComponent>(args.Used, out var breaker) && ent.Comp.State != SensorTowerState.Weld)
        {
            var doafter = new DoAfterArgs(EntityManager, args.User, breaker.DoAfterTime, new RMCDeviceBreakerDoAfterEvent(), args.Used, args.Target, args.Used)
            {
                BreakOnMove = true,
                RequireCanInteract = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            args.Handled = true;
            _doAfter.TryStartDoAfter(doafter);
            return;
        }

        var correctQuality = ent.Comp.State switch
        {
            SensorTowerState.Weld => ent.Comp.WeldingQuality,
            SensorTowerState.Wire => ent.Comp.CuttingQuality,
            SensorTowerState.Wrench => ent.Comp.WrenchQuality,
            _ => throw new ArgumentOutOfRangeException(),
        };

        args.Handled = true;

        if (_tool.HasQuality(used, correctQuality))
            TryRepair(ent, user, used, ent.Comp.State);
    }

    private void OnSensorTowerInteractHand(Entity<SensorTowerComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (HasComp<XenoComponent>(user))
        {
            if (!HasComp<HandsComponent>(user))
                return;

            Destroy(ent, user);
            return;
        }

        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            _popup.PopupClient(Loc.GetString("rmc-bioscan-skill-issue"), ent, user, PopupType.SmallCaution);
            return;
        }

        ref var state = ref ent.Comp.State;
        var popup = state switch
        {
            SensorTowerState.Weld => Loc.GetString("rmc-bioscan-repair-welder-wirecut-wrench"),
            SensorTowerState.Wire => Loc.GetString("rmc-bioscan-repair-wirecut-wrench"),
            SensorTowerState.Wrench => Loc.GetString("rmc-bioscan-repair-wrench"),
            SensorTowerState.Off => Loc.GetString("rmc-bioscan-lights-up", ("name", Name(ent))),
            SensorTowerState.On => Loc.GetString("rmc-bioscan-goes-dark", ("name", Name(ent))),
            _ => throw new ArgumentOutOfRangeException(),
        };
        _popup.PopupClient(popup, ent, user, PopupType.Medium);

        if (state < SensorTowerState.Off)
            return;

        if (state == SensorTowerState.Off)
            state = SensorTowerState.On;
        else if (state == SensorTowerState.On)
            state = SensorTowerState.Off;

        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnSensorTowerExamined(Entity<SensorTowerComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(SensorTowerComponent)))
        {
            // TODO: localize
            var text = ent.Comp.State switch
            {
                SensorTowerState.Weld => Loc.GetString("rmc-bioscan-damaged-repair-welder-wirecut-wrench"),
                SensorTowerState.Wire => Loc.GetString("rmc-bioscan-damaged-repair-wirecut-wrench"),
                SensorTowerState.Wrench => Loc.GetString("rmc-bioscan-damaged-repair-wrench"),
                SensorTowerState.Off => Loc.GetString("rmc-bioscan-status-on"),
                SensorTowerState.On => Loc.GetString("rmc-bioscan-status-off"),
                _ => throw new ArgumentOutOfRangeException(),
            };
            args.PushText(text);

            if (ent.Comp.State < SensorTowerState.Off)
            {
                var tool = ent.Comp.State switch
                {
                    SensorTowerState.Wrench => Loc.GetString("rmc-apc-use-repair-wrench"),
                    SensorTowerState.Wire => Loc.GetString("rmc-apc-use-repair-wirecutters"),
                    SensorTowerState.Weld => Loc.GetString("rmc-apc-use-repair-welder"),
                    _ => throw new ArgumentOutOfRangeException(),
                };

                args.PushMarkup(Loc.GetString("rmc-apc-use-repair-tool", ("tool", tool)));
            }
        }
    }

    private void OnSensorTowerRepairDoAfter(Entity<SensorTowerComponent> ent, ref SensorTowerRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.State != args.State)
            return;

        ent.Comp.State = args.State switch
        {
            SensorTowerState.Weld => SensorTowerState.Wire,
            SensorTowerState.Wire => SensorTowerState.Wrench,
            SensorTowerState.Wrench => SensorTowerState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnSensorTowerDestroyDoAfter(Entity<SensorTowerComponent> ent, ref SensorTowerDestroyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        FullyDestroy(ent);
    }

    public void FullyDestroy(Entity<SensorTowerComponent> ent)
    {
        ent.Comp.State = SensorTowerState.Weld;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    public void SensorTowerIncrementalDestroy(Entity<SensorTowerComponent> ent)
    {
        ent.Comp.State = ent.Comp.State switch
        {
            SensorTowerState.On => SensorTowerState.Wrench,
            SensorTowerState.Off => SensorTowerState.Wrench,
            SensorTowerState.Wrench => SensorTowerState.Wire,
            SensorTowerState.Wire => SensorTowerState.Weld,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void TryRepair(Entity<SensorTowerComponent> tower, EntityUid user, EntityUid used, SensorTowerState state)
    {
        var quality = state switch
        {
            SensorTowerState.Weld => tower.Comp.WeldingQuality,
            SensorTowerState.Wire => tower.Comp.CuttingQuality,
            SensorTowerState.Wrench => tower.Comp.WrenchQuality,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

        var delay = state switch
        {
            SensorTowerState.Weld => tower.Comp.WeldingDelay,
            SensorTowerState.Wire => tower.Comp.CuttingDelay,
            SensorTowerState.Wrench => tower.Comp.WrenchDelay,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        _tool.UseTool(
            used,
            user,
            tower,
            (float)delay.TotalSeconds,
            quality,
            new SensorTowerRepairDoAfterEvent(state),
            tower.Comp.WeldingCost
        );
    }

    private void UpdateAppearance(Entity<SensorTowerComponent> tower)
    {
        _appearance.SetData(tower, SensorTowerLayers.Layer, tower.Comp.State);
    }

    private void Destroy(Entity<SensorTowerComponent> tower, EntityUid user)
    {
        if (tower.Comp.State == SensorTowerState.Weld)
        {
            _popup.PopupClient(Loc.GetString("rmc-bioscan-xeno-issue"), user, user, PopupType.SmallCaution);
            return;
        }

        var ev = new SensorTowerDestroyDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, tower.Comp.DestroyDelay, ev, tower, tower, user)
        {
            ForceVisible = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(Loc.GetString("rmc-bioscan-xeno-breach", ("tower", Name(tower))), tower, user, PopupType.Medium);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<SensorTowerComponent>();
        while (query.MoveNext(out var uid, out var tower))
        {
            if (tower.State != SensorTowerState.On)
                continue;

            if (time < tower.NextBreakAt)
                continue;

            if (!_random.Prob(tower.BreakChance))
            {
                tower.NextBreakAt = time + tower.BreakEvery;
                Dirty(uid, tower);
                continue;
            }

            if (_random.Prob(0.75f))
            {
                _popup.PopupEntity(Loc.GetString("rmc-bioscan-breach-beep", ("name", Name(uid))), uid, uid, PopupType.LargeCaution);
                tower.State = SensorTowerState.Wrench;
                Dirty(uid, tower);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("rmc-bioscan-breach-beep-wirecut", ("name", Name(uid))), uid, uid, PopupType.LargeCaution);
                tower.State = SensorTowerState.Wire;
                Dirty(uid, tower);
            }

            UpdateAppearance((uid, tower));
        }
    }
}
