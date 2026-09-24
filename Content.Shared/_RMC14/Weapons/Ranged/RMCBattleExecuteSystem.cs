using System.Numerics;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Camera;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class RMCBattleExecuteSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _cameraRecoil = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<MarineComponent, GetVerbsEvent<AlternativeVerb>>(AlternativeInteract);
        SubscribeLocalEvent<MarineComponent, RMCBattleExecuteEvent>(ExecuteDoAfter);
        SubscribeLocalEvent<RMCBattleExecutedComponent, ExaminedEvent>(ExamineBody);
        SubscribeLocalEvent<RMCBattleExecuteComponent, ExaminedEvent>(OnGunExecuteExamined);
    }

    private void OnGunExecuteExamined(Entity<RMCBattleExecuteComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCBattleExecuteComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-examine-text-execute"));
        }
    }

    private void AlternativeInteract(Entity<MarineComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.User != args.Target &&
            _hands.TryGetActiveItem(args.User, out var handHeldItem) &&
            TryComp(args.User, out CombatModeComponent? combatModeComponent) &&
            combatModeComponent.IsInCombatMode &&
            TryComp(handHeldItem, out RMCBattleExecuteComponent? executionComponent) &&
            _skills.HasSkill(args.User, executionComponent.Skill, 1))

        {
            var target = args.Target;
            var user = args.User;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-execution"),
                Act = () => { Execute(user, target, executionComponent, handHeldItem.Value); },
                Priority = 100
            });
        }
    }

    private void Execute(EntityUid user,
        EntityUid target,
        RMCBattleExecuteComponent executionComponent,
        EntityUid handHeldItem)
    {
        if (_mobState.IsDead(target) && _unrevivable.IsUnrevivable(target))
        {
            var cancelledMessage = $"You decide to not Execute {Name(target)}, as they are already far beyond revival.";
            _popup.PopupClient(cancelledMessage, user, PopupType.MediumCaution);
            return;
        }

        var ev = new RMCBattleExecuteEvent(GetNetEntity(user), GetNetEntity(target), executionComponent.Damage);
        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            executionComponent.BattleExecuteTimeSeconds,
            ev,
            target,
            target,
            handHeldItem);
        _doAfter.TryStartDoAfter(doAfterArgs);

        var selfMsg = Loc.GetString("rmc-execute-start-self", ("target", Name(target)), ("gun", Name(handHeldItem)));
        var othersMsg = Loc.GetString("rmc-execute-start-others", ("user", Name(user)), ("target", Name(target)), ("gun", Name(handHeldItem)));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user, PopupType.LargeCaution);
    }

    private void ExecuteDoAfter(Entity<MarineComponent> ent, ref RMCBattleExecuteEvent args)
    {
        var user = GetEntity(args.User);
        var target = GetEntity(args.Target);

        if (args.Cancelled)
        {
            _admin.Add(LogType.RMCExecution,
                LogImpact.High,
                $"{ToPrettyString(user)}'s Execution of {ToPrettyString(target)} was cancelled.");
            var cancelledMessage = $"You decide to not Execute {Name(target)}.";
            _popup.PopupClient(cancelledMessage, user, PopupType.MediumCaution);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        if (!Exists(args.Used) || !TryComp<GunComponent>(args.Used, out var gun))
            return;

        var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();
        var ev = new TakeAmmoEvent(1, ammo, Transform(user).Coordinates, user);
        RaiseLocalEvent(args.Used.Value, ev);

        if (ev.Ammo.Count == 0)
        {
            _admin.Add(LogType.RMCExecution,
                LogImpact.High,
                $"{ToPrettyString(user)}'s Execution of {ToPrettyString(target)} was cancelled from lack of ammo.");
            _audio.PlayPredicted(gun.SoundEmpty, args.Used.Value, user);
            return;
        }

        foreach (var (bullet, _) in ev.Ammo)
        {
            Del(bullet);
        }

        _admin.Add(LogType.RMCExecution,
            LogImpact.High,
            $"{ToPrettyString(user)}'s Execution of {ToPrettyString(target)} Succeeded!");

        if (TryComp<WieldableComponent>(args.Used.Value, out var wieldable))
        {
            if (!wieldable.Wielded)
            {
                var recoilScalar = gun.CameraRecoilScalarModified;

                var userCoords = _transform.GetWorldPosition(user);
                var targetCoords = _transform.GetWorldPosition(target);
                var direction = targetCoords - userCoords;

                if (direction == Vector2.Zero)
                    direction = new Vector2(0, -1);

                var kick = direction.Normalized() * recoilScalar;
                _cameraRecoil.KickCamera(user, kick);
            }
        }

        //ToDo RMC14 Make this head damage.
        _damageable.TryChangeDamage(target, args.BattleExecuteDamage, true);
        _mobState.ChangeMobState(target, MobState.Dead);
        _unrevivable.MakeUnrevivable(target);

        _audio.PlayPredicted(gun.SoundGunshotModified, args.Used.Value, user);

        var popupMessage = $"{Name(target)} WAS EXECUTED BY {Name(user)}!";
        _popup.PopupPredicted(popupMessage, target, user, PopupType.LargeCaution);

        var chatMsg = $"[bold][font size=24][color=red]\n{Name(target)} WAS EXECUTED BY {Name(user)}!\n[/color][/font][/bold]";
        var coordinates = _transform.GetMapCoordinates(target);
        var players = Filter.Empty().AddInRange(coordinates, 12, _player, EntityManager);

        players.RemoveWhereAttachedEntity(HasComp<XenoComponent>);
        _rmcChat.ChatMessageToMany(chatMsg, chatMsg, players, ChatChannel.Local);
        EnsureComp<RMCBattleExecutedComponent>(target);
    }

    private void ExamineBody(Entity<RMCBattleExecutedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ExecutedText, ("victim", ent.Owner)));
    }
}
