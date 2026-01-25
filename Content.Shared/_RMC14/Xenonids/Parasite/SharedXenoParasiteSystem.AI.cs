using Content.Shared._RMC14.NPC;
using Content.Shared._RMC14.Xenonids.Construction.EggMorpher;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Actions.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Parasite;

public abstract partial class SharedXenoParasiteSystem
{
    [Dependency] private readonly SharedRMCNPCSystem _rmcNpc = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public void IntializeAI()
    {
        SubscribeLocalEvent<XenoParasiteComponent, PlayerAttachedEvent>(OnPlayerAdded);
        SubscribeLocalEvent<XenoParasiteComponent, PlayerDetachedEvent>(OnPlayerRemoved);
        SubscribeLocalEvent<ParasiteAIDelayAddComponent, ComponentStartup>(OnAIDelayAdded);

        SubscribeLocalEvent<ParasiteAIComponent, MapInitEvent>(OnAIAdded);
        SubscribeLocalEvent<ParasiteAIComponent, ExaminedEvent>(OnAIExamined);
        SubscribeLocalEvent<ParasiteAIComponent, DroppedEvent>(OnAIDropPickup);
        SubscribeLocalEvent<ParasiteAIComponent, EntGotInsertedIntoContainerMessage>(OnAIDropPickup);
        SubscribeLocalEvent<ParasiteAIComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);

        SubscribeLocalEvent<TrapParasiteComponent, ComponentStartup>(OnTrapAdded);
        SubscribeLocalEvent<TrapParasiteComponent, PlayerAttachedEvent>(OnStopTrap);
        SubscribeLocalEvent<TrapParasiteComponent, EntGotInsertedIntoContainerMessage>(OnStopTrap);
        SubscribeLocalEvent<TrapParasiteComponent, XenoLeapHitEvent>(OnLeapEndTrap);
        SubscribeLocalEvent<TrapParasiteComponent, ComponentShutdown>(OnTrapEnd);

        SubscribeLocalEvent<ParasiteTiredOutComponent, MapInitEvent>(OnParasiteAIMapInit);
        SubscribeLocalEvent<ParasiteTiredOutComponent, UpdateMobStateEvent>(OnParasiteAIUpdateMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);
    }

    private void OnTrapAdded(Entity<TrapParasiteComponent> para, ref ComponentStartup args)
    {
        para.Comp.LeapAt = _timing.CurTime + para.Comp.JumpTime;
        para.Comp.DisableAt = para.Comp.LeapAt + para.Comp.DisableTime;

        if (!TryComp<XenoLeapComponent>(para, out var leap))
            return;

        para.Comp.NormalLeapDelay = leap.Delay;
        leap.Delay = TimeSpan.Zero;
        _rmcNpc.SleepNPC(para);
    }

    private void OnStopTrap<T>(Entity<TrapParasiteComponent> para, ref T args) where T : EntityEventArgs
    {
        RemCompDeferred<TrapParasiteComponent>(para);
    }

    private void OnTrapEnd(Entity<TrapParasiteComponent> para, ref ComponentShutdown args)
    {
        if (!TryComp<XenoLeapComponent>(para, out var leap))
            return;

        leap.Delay = para.Comp.NormalLeapDelay;
        _rmcNpc.SleepNPC(para);
    }

    private void OnLeapEndTrap(Entity<TrapParasiteComponent> para, ref XenoLeapHitEvent args)
    {
        ResetTrapState(para);
    }

    public void ResetTrapState(Entity<TrapParasiteComponent> para)
    {
        if (!TryComp<XenoLeapComponent>(para, out var leap))
            return;

        if (TryComp<ParasiteAIComponent>(para, out var leapAI))
            _rmcNpc.SleepNPC(para);

        leap.Delay = para.Comp.NormalLeapDelay;
        RemCompDeferred<TrapParasiteComponent>(para);
    }

    private void OnPlayerAdded(Entity<XenoParasiteComponent> para, ref PlayerAttachedEvent args)
    {
        RemCompDeferred<ParasiteAIComponent>(para);
        RemCompDeferred<ParasiteAIDelayAddComponent>(para);
    }

    private void OnPlayerRemoved(Entity<XenoParasiteComponent> para, ref PlayerDetachedEvent args)
    {
        if(!TerminatingOrDeleted(para))
            EnsureComp<ParasiteAIDelayAddComponent>(para);
    }

    private void OnAIDelayAdded(Entity<ParasiteAIDelayAddComponent> para, ref ComponentStartup args)
    {
        para.Comp.TimeToAI = _timing.CurTime + para.Comp.DelayTime;
        _rmcNpc.SleepNPC(para); // Keep it asleep
    }


    private void OnAIAdded(Entity<ParasiteAIComponent> para, ref MapInitEvent args)
    {
        if (_mobState.IsDead(para))
            return;

        HandleDeathTimer(para);
        GoActive(para);
    }

    private void OnAIExamined(Entity<ParasiteAIComponent> para, ref ExaminedEvent args)
    {
        if (_mobState.IsDead(para) || !HasComp<XenoComponent>(args.Examiner))
            return;

        switch (para.Comp.Mode)
        {
            case ParasiteMode.Idle:
                args.PushMarkup($"{Loc.GetString("rmc-xeno-parasite-ai-idle", ("parasite", para))}");
                break;
            case ParasiteMode.Active:
                args.PushMarkup($"{Loc.GetString("rmc-xeno-parasite-ai-active", ("parasite", para))}");
                break;
            case ParasiteMode.Dying:
                args.PushMarkup($"{Loc.GetString("rmc-xeno-parasite-ai-dying", ("parasite", para))}");
                break;
        }
    }


    private void OnAIDropPickup<T>(Entity<ParasiteAIComponent> para, ref T args) where T : EntityEventArgs
    {
        HandleDeathTimer(para);
        GoIdle(para);
    }

    public void HandleDeathTimer(Entity<ParasiteAIComponent> para)
    {
        if (_container.TryGetContainingContainer((para, null, null), out var carry) && HasComp<XenoNurturingComponent>(carry.Owner))
        {
            para.Comp.DeathTime = null;
            if (para.Comp.Mode == ParasiteMode.Dying)
            {
                para.Comp.Mode = ParasiteMode.Active;
                GoIdle(para);
            }
            return;
        }

        if (para.Comp.DeathTime == null)
            para.Comp.DeathTime = _timing.CurTime + para.Comp.LifeTime;
    }

    public void UpdateAI(Entity<ParasiteAIComponent> para, TimeSpan currentTime)
    {
        CheckCannibalize(para);
        if (para.Comp.DeathTime != null && currentTime > para.Comp.DeathTime || para.Comp.JumpsLeft <= 0)
        {
            if (para.Comp.Mode != ParasiteMode.Dying)
            {
                para.Comp.Mode = ParasiteMode.Dying;

                if (HasComp<XenoRestingComponent>(para))
                    DoRestAction(para);

                ChangeHTN(para, ParasiteMode.Dying);
                _rmcNpc.WakeNPC(para);

                Dirty(para);
            }

            CheckDeath(para);
            return;
        }

        if (para.Comp.Mode == ParasiteMode.Active && currentTime >= para.Comp.NextJump && !_container.IsEntityInContainer(para))
        {
            if (!HasComp<StunnedComponent>(para))
            {
                var trap = EnsureComp<TrapParasiteComponent>(para);
                trap.JumpTime = TimeSpan.Zero;
            }
            para.Comp.NextJump = currentTime + para.Comp.JumpTime;
        }

        if (para.Comp.Mode == ParasiteMode.Idle && currentTime > para.Comp.NextActiveTime)
            GoActive(para);
    }

    public void GoIdle(Entity<ParasiteAIComponent> para)
    {
        // Always reset jumps to you can continue infecting from pickup
        para.Comp.JumpsLeft = para.Comp.InitialJumps;

        if (para.Comp.Mode != ParasiteMode.Active)
            return;

        if (!HasComp<XenoRestingComponent>(para))
            DoRestAction(para);

        _rmcNpc.SleepNPC(para);
        para.Comp.Mode = ParasiteMode.Idle;

        if (HasComp<TrapParasiteComponent>(para))
            RemCompDeferred<TrapParasiteComponent>(para);

        para.Comp.NextActiveTime = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(para.Comp.MinIdleTime, para.Comp.MaxIdleTime + 1));

        Dirty(para);
    }

    public void GoActive(Entity<ParasiteAIComponent> para)
    {
        if (para.Comp.Mode == ParasiteMode.Dying)
            return;

        if (HasComp<XenoRestingComponent>(para))
            DoRestAction(para);

        ChangeHTN(para, ParasiteMode.Active);
        para.Comp.Mode = ParasiteMode.Active;
        _rmcNpc.SleepNPC(para);
        if (HasComp<TrapParasiteComponent>(para))
            RemCompDeferred<TrapParasiteComponent>(para);
        para.Comp.NextJump = _timing.CurTime + para.Comp.JumpTime;
        Dirty(para);
    }

    private void DoRestAction(Entity<ParasiteAIComponent> para)
    {
        if (!TryComp<XenoComponent>(para, out var xeno) || !xeno.Initialized)
            return;

        var actions = xeno.Actions;
        if (!actions.TryGetValue(para.Comp.RestAction, out var action))
            return;

        if (!TryComp<ActionComponent>(action, out var actionComp))
            return;

        var actionEvent = _action.GetEvent(action);
        _action.PerformAction(para.Owner, (action, actionComp), actionEvent);
    }

    protected virtual void ChangeHTN(EntityUid parasite, ParasiteMode mode)
    {
    }

    private void CheckCannibalize(Entity<ParasiteAIComponent> para)
    {
        if (_rmcHands.TryGetHolder(para, out var _))
            return;

        if (HasComp<ThrownItemComponent>(para))
            return;

        int totalParasites = 0;
        foreach (var parasite in _entityLookup.GetEntitiesInRange<ParasiteAIComponent>(_transform.GetMapCoordinates(para), para.Comp.CannibalizeCheck))
        {
            if (parasite == para)
                continue;

            // Ignore those that are dead, not active, or already are being deleted - plus a ton of other things
            if (TerminatingOrDeleted(parasite) || EntityManager.IsQueuedForDeletion(parasite) || _mobState.IsDead(parasite) ||
                parasite.Comp.Mode != ParasiteMode.Active || _rmcHands.TryGetHolder(parasite, out var _) ||
                HasComp<ThrownItemComponent>(parasite) || HasComp<StunnedComponent>(parasite))
                continue;

            totalParasites++;
        }

        if (totalParasites <= para.Comp.MaxSurroundingParas)
            return;

        // Get Eaten
        _popup.PopupCoordinates(Loc.GetString("rmc-xeno-parasite-ai-eaten", ("parasite", para)), _transform.GetMoverCoordinates(para), PopupType.SmallCaution);
        QueueDel(para);
    }

    private void CheckDeath(Entity<ParasiteAIComponent> para)
    {
        foreach (var egg in _entityLookup.GetEntitiesInRange<XenoEggComponent>(_transform.GetMoverCoordinates(para), para.Comp.RangeCheck))
        {
            if (egg.Comp.State == XenoEggState.Opened)
                return;
        }

        foreach (var trap in _entityLookup.GetEntitiesInRange<XenoResinHoleComponent>(_transform.GetMoverCoordinates(para), para.Comp.RangeCheck))
        {
            if (trap.Comp.TrapPrototype == null)
                return;
        }

        foreach (var eggmorpher in _entityLookup.GetEntitiesInRange<EggMorpherComponent>(_transform.GetMoverCoordinates(para), para.Comp.RangeCheck))
        {
            if (eggmorpher.Comp.CurParasites < eggmorpher.Comp.MaxParasites)
                return;
        }

        EnsureComp<ParasiteTiredOutComponent>(para);
    }

    private void OnParasiteAIMapInit(Entity<ParasiteTiredOutComponent> dead, ref MapInitEvent args)
    {
        if (TryComp(dead, out MobStateComponent? mobState))
            _mobState.UpdateMobState(dead, mobState);
    }

    private void OnParasiteAIUpdateMobState(Entity<ParasiteTiredOutComponent> dead, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnGetVerbs(Entity<ParasiteAIComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        var uid = args.User;

        // if it doesn't have an actor and we can't reach it then don't add the verb
        if (!HasComp<ActorComponent>(uid) || !HasComp<GhostComponent>(uid))
            return;

        if (!_mobState.IsAlive(ent))
            return;


        var parasiteVerb = new ActivationVerb
        {
            Text = Loc.GetString("rmc-xeno-egg-ghost-verb"),
            Act = () =>
            {
                _ui.TryOpenUi(ent.Owner, XenoParasiteGhostUI.Key, uid);
            },

            Impact = LogImpact.High,
        };

        args.Verbs.Add(parasiteVerb);
    }
}
