using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.IffTag;

public sealed class RMCXenoIffTagSystem : EntitySystem
{
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly TimeSpan ImplantDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RemoveDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReprogramDelay = TimeSpan.FromSeconds(5);

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _targetFactionBuffer = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCXenoIffTagItemComponent, AfterInteractEvent>(OnTagAfterInteract);
        SubscribeLocalEvent<RMCXenoIffTagItemComponent, RMCXenoIffTagDoAfterEvent>(OnTagDoAfter);

        SubscribeLocalEvent<RMCXenoIffTagComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCXenoIffTagComponent, GetVerbsEvent<AlternativeVerb>>(OnGetRemoveVerb);
        SubscribeLocalEvent<RMCXenoIffTagComponent, GetVerbsEvent<Verb>>(OnGetProtectsVerb);
        SubscribeLocalEvent<RMCXenoIffTagComponent, RMCXenoIffTagRemoveDoAfterEvent>(OnRemoveDoAfter);

        SubscribeLocalEvent<RMCXenoIffTagComponent, InteractUsingEvent>(OnReprogramInteract);
        SubscribeLocalEvent<RMCXenoIffTagComponent, RMCXenoIffTagReprogramDoAfterEvent>(OnReprogramDoAfter);
        SubscribeLocalEvent<RMCXenoIffTagComponent, RMCXenoIffTagReprogramChosenEvent>(OnReprogramChosen);
    }

    private void OnTagAfterInteract(Entity<RMCXenoIffTagItemComponent> tag, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<XenoComponent>(target))
            return;

        if (!TryComp<MobStateComponent>(target, out var mobState) || _mobState.IsDead(target, mobState))
        {
            _popup.PopupClient("You can't implant a tag into a dead Xenonid!", args.User, args.User);
            return;
        }

        if (HasComp<RMCXenoIffTagComponent>(target))
        {
            _popup.PopupClient($"{Name(target)} already has a tag inside it.", args.User, args.User);
            return;
        }

        args.Handled = true;
        _popup.PopupClient($"You start forcing {Name(tag)} into {Name(target)}'s carapace...", args.User, args.User);

        var ev = new RMCXenoIffTagDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ImplantDelay, ev, tag, target, tag)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTagDoAfter(Entity<RMCXenoIffTagItemComponent> tag, ref RMCXenoIffTagDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!HasComp<XenoComponent>(target) || HasComp<RMCXenoIffTagComponent>(target))
            return;

        if (!TryComp<MobStateComponent>(target, out var mobState) || _mobState.IsDead(target, mobState))
            return;

        args.Handled = true;

        _popup.PopupClient($"You force {Name(tag)} into {Name(target)}'s carapace!", args.User, args.User);

        var xeno = new Entity<RMCXenoIffTagComponent>(target, EnsureComp<RMCXenoIffTagComponent>(target));

        var buffer = new HashSet<EntProtoId<IFFFactionComponent>>();
        _gunIFF.TryGetFactions(args.User, buffer);
        xeno.Comp.Factions.UnionWith(buffer);
        Dirty(xeno);

        foreach (var faction in xeno.Comp.Factions)
        {
            _gunIFF.AddUserFaction(xeno.Owner, faction);
        }

        if (_hive.GetHive(xeno.Owner) is { } implantHive &&
            TryComp(implantHive.Owner, out HiveSlotComponent? implantSlot) &&
            implantSlot.Position == HiveSlots.Renegade)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-renegade-instincts-changed",
                ("factions", xeno.Comp.Factions.Count == 0 ? Loc.GetString("rmc-xeno-iff-tag-no-one") : string.Join(", ", xeno.Comp.Factions))),
                xeno, xeno);
        }

        if (_net.IsClient)
            return;

        QueueDel(tag);
    }

    private void OnExamined(Entity<RMCXenoIffTagComponent> xeno, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-xeno-iff-tag-examine"));
    }

    private void OnGetProtectsVerb(Entity<RMCXenoIffTagComponent> xeno, ref GetVerbsEvent<Verb> args)
    {
        if (args.User != xeno.Owner)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("rmc-xeno-iff-tag-check-protects"),
            Category = VerbCategory.Examine,
            Act = () =>
            {
                var msg = xeno.Comp.Factions.Count == 0
                    ? Loc.GetString("rmc-xeno-iff-tag-protects-none")
                    : Loc.GetString("rmc-xeno-iff-tag-protects", ("factions", string.Join(", ", xeno.Comp.Factions)));

                _popup.PopupClient(msg, xeno, xeno);
            },
        });
    }

    private void OnGetRemoveVerb(Entity<RMCXenoIffTagComponent> xeno, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Remove IFF Tag",
            Act = () =>
            {
                _popup.PopupClient($"You start removing the IFF tag from {Name(xeno)}'s carapace...", user, user);

                var ev = new RMCXenoIffTagRemoveDoAfterEvent();
                var doAfter = new DoAfterArgs(EntityManager, user, RemoveDelay, ev, xeno, xeno)
                {
                    BreakOnMove = true,
                    NeedHand = true,
                };

                _doAfter.TryStartDoAfter(doAfter);
            },
        });
    }

    private void OnRemoveDoAfter(Entity<RMCXenoIffTagComponent> xeno, ref RMCXenoIffTagRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var wasRenegade = _hive.GetHive(xeno.Owner) is { } hive &&
            TryComp(hive.Owner, out HiveSlotComponent? slot) &&
            slot.Position == HiveSlots.Renegade;

        RemoveTag(xeno.Owner);

        _popup.PopupClient($"You rip the IFF tag out of {Name(xeno)}'s carapace!", args.User, args.User);

        if (wasRenegade)
            _popup.PopupClient(Loc.GetString("rmc-xeno-renegade-tag-removed"), xeno, xeno);
    }

    public void TransferTag(EntityUid from, EntityUid to)
    {
        if (!TryComp(from, out RMCXenoIffTagComponent? oldTag))
            return;

        var factions = new HashSet<EntProtoId<IFFFactionComponent>>(oldTag.Factions);
        RemoveTag(from);

        var newTag = EnsureComp<RMCXenoIffTagComponent>(to);
        newTag.Factions.UnionWith(factions);
        Dirty(to, newTag);

        foreach (var faction in factions)
        {
            _gunIFF.AddUserFaction(to, faction);
        }
    }

    public bool RemoveTag(EntityUid xeno)
    {
        if (!TryComp(xeno, out RMCXenoIffTagComponent? tag))
            return false;

        foreach (var faction in tag.Factions)
        {
            _gunIFF.RemoveUserFaction(xeno, faction);
        }

        RemComp<RMCXenoIffTagComponent>(xeno);
        return true;
    }

    private void OnReprogramInteract(Entity<RMCXenoIffTagComponent> xeno, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<MultitoolComponent>(args.Used) || HasComp<RMCXenoIffTagReprogramPendingComponent>(xeno.Owner))
            return;

        args.Handled = true;

        _popup.PopupClient($"You start reprogramming {Name(xeno)}'s IFF tag...", args.User, args.User);

        var ev = new RMCXenoIffTagReprogramDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ReprogramDelay, ev, xeno, xeno, args.Used)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnReprogramDoAfter(Entity<RMCXenoIffTagComponent> xeno, ref RMCXenoIffTagReprogramDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || _net.IsClient)
            return;

        args.Handled = true;

        var pending = EnsureComp<RMCXenoIffTagReprogramPendingComponent>(xeno.Owner);
        pending.Programmer = args.User;

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-xeno-iff-tag-reprogram-overwrite"), new RMCXenoIffTagReprogramChosenEvent(RMCXenoIffTagReprogramOption.Overwrite)),
            new(Loc.GetString("rmc-xeno-iff-tag-reprogram-add"), new RMCXenoIffTagReprogramChosenEvent(RMCXenoIffTagReprogramOption.Add)),
            new(Loc.GetString("rmc-xeno-iff-tag-reprogram-remove"), new RMCXenoIffTagReprogramChosenEvent(RMCXenoIffTagReprogramOption.Remove)),
        };

        _dialog.OpenOptions(xeno.Owner, args.User, Loc.GetString("rmc-xeno-iff-tag-reprogram-title"),
            options, Loc.GetString("rmc-xeno-iff-tag-reprogram-message"));
    }

    private void OnReprogramChosen(Entity<RMCXenoIffTagComponent> xeno, ref RMCXenoIffTagReprogramChosenEvent args)
    {
        if (_net.IsClient || !TryComp(xeno.Owner, out RMCXenoIffTagReprogramPendingComponent? pending))
            return;

        var programmer = pending.Programmer;
        RemComp<RMCXenoIffTagReprogramPendingComponent>(xeno.Owner);

        var buffer = new HashSet<EntProtoId<IFFFactionComponent>>();
        _gunIFF.TryGetFactions(programmer, buffer);

        foreach (var faction in xeno.Comp.Factions)
        {
            _gunIFF.RemoveUserFaction(xeno.Owner, faction);
        }

        switch (args.Option)
        {
            case RMCXenoIffTagReprogramOption.Overwrite:
                xeno.Comp.Factions.Clear();
                xeno.Comp.Factions.UnionWith(buffer);
                break;
            case RMCXenoIffTagReprogramOption.Add:
                xeno.Comp.Factions.UnionWith(buffer);
                break;
            case RMCXenoIffTagReprogramOption.Remove:
                xeno.Comp.Factions.Clear();
                break;
        }

        Dirty(xeno);

        foreach (var faction in xeno.Comp.Factions)
        {
            _gunIFF.AddUserFaction(xeno.Owner, faction);
        }

        _popup.PopupEntity(Loc.GetString("rmc-xeno-iff-tag-reprogram-done", ("xeno", xeno.Owner)), xeno, programmer, PopupType.Medium);

        if (_hive.GetHive(xeno.Owner) is { } hive &&
            TryComp(hive.Owner, out HiveSlotComponent? slot) &&
            slot.Position == HiveSlots.Renegade)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-renegade-instincts-changed",
                ("factions", xeno.Comp.Factions.Count == 0 ? Loc.GetString("rmc-xeno-iff-tag-no-one") : string.Join(", ", xeno.Comp.Factions))),
                xeno, xeno, PopupType.Medium);
        }
    }

    public bool HasFaction(EntityUid xeno, EntProtoId<IFFFactionComponent> faction)
    {
        return TryComp(xeno, out RMCXenoIffTagComponent? tag) && tag.Factions.Contains(faction);
    }

    public bool IffProtects(EntityUid attacker, EntityUid target)
    {
        if (attacker == target)
            return true;

        if (!TryComp(attacker, out RMCXenoIffTagComponent? attackerTag))
            return false;

        if (HasComp<XenoComponent>(target))
        {
            if (!TryComp(target, out RMCXenoIffTagComponent? targetTag))
                return false;

            return attackerTag.Factions.Overlaps(targetTag.Factions);
        }

        _targetFactionBuffer.Clear();
        _gunIFF.TryGetFactions(target, _targetFactionBuffer);
        return attackerTag.Factions.Overlaps(_targetFactionBuffer);
    }
}
