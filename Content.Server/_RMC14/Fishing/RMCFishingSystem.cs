using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Fishing;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Water;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Directions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Server._RMC14.Fishing;

public sealed class RMCFishingSystem : EntitySystem
{
    private const string FishingLineVisualPrototypePrefix = "RMCFishingLineVisual";

    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCWaterSystem _water = default!;

    private readonly Dictionary<EntityUid, EntityUid> _lineVisuals = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCFishingRodComponent, ComponentInit>(OnRodInit);
        SubscribeLocalEvent<RMCFishingRodComponent, UseInHandEvent>(OnRodUseInHand);
        SubscribeLocalEvent<RMCFishingRodComponent, InteractUsingEvent>(OnRodInteractUsing);
        SubscribeLocalEvent<RMCFishingRodComponent, InteractHandEvent>(OnRodInteractHand);
        SubscribeLocalEvent<RMCFishingRodComponent, GetVerbsEvent<AlternativeVerb>>(OnRodGetAlternativeVerbs);
        SubscribeLocalEvent<RMCFishingRodComponent, ExaminedEvent>(OnRodExamined);
        SubscribeLocalEvent<RMCFishingRodComponent, RMCFishingDeployDoAfterEvent>(OnRodDeployDoAfter);
        SubscribeLocalEvent<RMCFishingRodComponent, RMCFishingPackDoAfterEvent>(OnRodPackDoAfter);
        SubscribeLocalEvent<RMCFishingRodComponent, RMCFishingWaitDoAfterEvent>(OnRodWaitDoAfter);
        SubscribeLocalEvent<RMCFishingRodComponent, ComponentShutdown>(OnRodShutdown);

        SubscribeLocalEvent<RMCFishComponent, MapInitEvent>(OnFishMapInit);
        SubscribeLocalEvent<RMCFishComponent, ExaminedEvent>(OnFishExamined);
        SubscribeLocalEvent<RMCFishComponent, InteractUsingEvent>(OnFishInteractUsing);

        SubscribeLocalEvent<RMCFishingSpearComponent, AfterInteractEvent>(OnSpearAfterInteract);
        SubscribeLocalEvent<RMCFishingSpearComponent, RMCFishingSpearDoAfterEvent>(OnSpearDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCFishingRodComponent>();
        while (query.MoveNext(out var uid, out var rod))
        {
            if (!rod.Deployed ||
                rod.State != RMCFishingRodState.Biting ||
                now < rod.BiteEndsAt)
            {
                continue;
            }

            FailBite((uid, rod), rod.CurrentFisher, "rmc-fishing-hook-timeout");
        }
    }

    private void OnRodInit(Entity<RMCFishingRodComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.BaitSlotId);
        UpdateRodAppearance(ent);
    }

    private void OnRodShutdown(Entity<RMCFishingRodComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.WaitToken++;
        ent.Comp.BiteToken++;
        ClearFishingLine(ent.Owner);
    }

    private void OnRodUseInHand(Entity<RMCFishingRodComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Deployed)
            return;

        args.Handled = true;
        var direction = Transform(args.User).LocalRotation.GetCardinalDir();
        if (!TryGetRodWater(args.User, direction, args.User, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-need-water"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DeployDelay, new RMCFishingDeployDoAfterEvent(direction), ent.Owner, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnRodDeployDoAfter(Entity<RMCFishingRodComponent> ent, ref RMCFishingDeployDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        if (ent.Comp.Deployed ||
            !TryGetRodWater(user, args.Direction, user, out _, out _))
        {
            return;
        }

        var dropCoords = _transform.GetMoverCoordinates(user).SnapToGrid(EntityManager);
        if (!_hands.TryDrop(user, ent.Owner, dropCoords))
            return;

        _transform.SetLocalRotation(ent.Owner, args.Direction.ToAngle());
        _transform.AnchorEntity(ent.Owner);
        _physics.SetBodyType(ent.Owner, BodyType.Static);

        ent.Comp.Deployed = true;
        ent.Comp.Direction = args.Direction;
        ClearRodState(ent, updateAppearance: false);
        UpdateRodAppearance(ent);

        _popup.PopupEntity(Loc.GetString("rmc-fishing-deploy-finish"), ent.Owner, user);
    }

    private void OnRodInteractUsing(Entity<RMCFishingRodComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<RMCFishBaitComponent>(args.Used))
            return;

        args.Handled = true;
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.BaitSlotId, out var baseContainer) ||
            baseContainer is not ContainerSlot slot)
        {
            return;
        }

        if (slot.ContainedEntity != null)
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-bait-already"), ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_hands.TryDrop(args.User, args.Used, doDropInteraction: false))
            return;

        if (!_container.Insert(args.Used, slot))
        {
            _hands.TryPickupAnyHand(args.User, args.Used);
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-fishing-bait-loaded", ("bait", args.Used), ("rod", ent.Owner)), ent.Owner, args.User);
    }

    private void OnRodInteractHand(Entity<RMCFishingRodComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !ent.Comp.Deployed)
            return;

        args.Handled = true;
        switch (ent.Comp.State)
        {
            case RMCFishingRodState.Waiting:
                _popup.PopupEntity(Loc.GetString("rmc-fishing-already-waiting"), ent.Owner, args.User, PopupType.SmallCaution);
                return;
            case RMCFishingRodState.Biting:
                TryHookFish(ent, args.User);
                return;
            case RMCFishingRodState.Idle:
            default:
                StartWaiting(ent, args.User);
                return;
        }
    }

    private void OnRodGetAlternativeVerbs(Entity<RMCFishingRodComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.Deployed)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-fishing-pack-verb"),
            Act = () => StartPack(ent, user),
            Icon = new Texture(new ResPath("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png")),
        });
    }

    private void StartPack(Entity<RMCFishingRodComponent> ent, EntityUid user)
    {
        if (!ent.Comp.Deployed)
            return;

        if (ent.Comp.State != RMCFishingRodState.Idle)
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-pack-busy"), ent.Owner, user, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.PackDelay, new RMCFishingPackDoAfterEvent(), ent.Owner, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnRodPackDoAfter(Entity<RMCFishingRodComponent> ent, ref RMCFishingPackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (!ent.Comp.Deployed || ent.Comp.State != RMCFishingRodState.Idle)
            return;

        _transform.Unanchor(ent.Owner);
        _physics.SetBodyType(ent.Owner, BodyType.Dynamic);

        if (!_hands.TryPickupAnyHand(args.User, ent.Owner))
        {
            _transform.AnchorEntity(ent.Owner);
            _physics.SetBodyType(ent.Owner, BodyType.Static);
            _popup.PopupEntity(Loc.GetString("rmc-fishing-pack-no-hand"), ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        ent.Comp.Deployed = false;
        ClearRodState(ent, updateAppearance: false);
        UpdateRodAppearance(ent);
        _popup.PopupEntity(Loc.GetString("rmc-fishing-pack-finish"), args.User, args.User);
    }

    private void OnRodExamined(Entity<RMCFishingRodComponent> ent, ref ExaminedEvent args)
    {
        if (TryGetBait(ent, out var bait))
            args.PushMarkup(Loc.GetString("rmc-fishing-examine-bait", ("bait", bait)));
        else
            args.PushMarkup(Loc.GetString("rmc-fishing-examine-no-bait"));
    }

    private void StartWaiting(Entity<RMCFishingRodComponent> ent, EntityUid user)
    {
        if (!TryGetRodWater(ent, user, out var adjacent, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-need-water"), ent.Owner, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.CurrentFisher = user;
        ent.Comp.State = RMCFishingRodState.Waiting;
        var token = ++ent.Comp.WaitToken;
        Dirty(ent);
        UpdateRodAppearance(ent);
        RefreshFishingLine(ent, adjacent, biting: false);

        _audio.PlayPvs(ent.Comp.StartSound, ent.Owner);
        var doAfter = new DoAfterArgs(EntityManager, user, RandomTime(ent.Comp.WaitMin, ent.Comp.WaitMax), new RMCFishingWaitDoAfterEvent(token), ent.Owner, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            ClearRodState(ent);
    }

    private void OnRodWaitDoAfter(Entity<RMCFishingRodComponent> ent, ref RMCFishingWaitDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.State != RMCFishingRodState.Waiting ||
            args.Token != ent.Comp.WaitToken ||
            ent.Comp.CurrentFisher != args.User)
        {
            return;
        }

        if (args.Cancelled ||
            TerminatingOrDeleted(args.User) ||
            !TryGetRodWater(ent, args.User, out var adjacent, out _))
        {
            ClearRodState(ent);
            return;
        }

        ent.Comp.State = RMCFishingRodState.Biting;
        // The token invalidates stale hook windows from old bites if a do-after is cancelled or restarted late.
        ent.Comp.BiteToken++;
        ent.Comp.BiteEndsAt = _timing.CurTime + RandomTime(ent.Comp.BiteMin, ent.Comp.BiteMax);
        Dirty(ent);
        UpdateRodAppearance(ent);
        RefreshFishingLine(ent, adjacent, biting: true);

        _audio.PlayPvs(ent.Comp.BiteSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("rmc-fishing-bite"), ent.Owner, args.User, PopupType.Small);
    }

    private void TryHookFish(Entity<RMCFishingRodComponent> ent, EntityUid user)
    {
        if (ent.Comp.CurrentFisher != user)
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-not-owner"), ent.Owner, user, PopupType.SmallCaution);
            return;
        }

        if (!TryGetRodWater(ent, user, out _, out var target))
        {
            FailBite(ent, user, "rmc-fishing-invalid-water");
            return;
        }

        var hadBait = TryGetBait(ent, out var baitUid);
        var bait = hadBait && TryComp(baitUid, out RMCFishBaitComponent? baitComp)
            ? baitComp
            : null;

        if (!TryPickLoot(target, ent.Comp.Loot, ent.Comp.CommonWeight, ent.Comp.UncommonWeight, ent.Comp.RareWeight, ent.Comp.UltraRareWeight, bait, out var loot))
        {
            FailBite(ent, user, "rmc-fishing-fail");
            return;
        }

        var caught = Spawn(loot, target);
        _throwing.TryThrow(caught, _transform.GetMoverCoordinates(user), 2f, user, compensateFriction: true);

        _audio.PlayPvs(ent.Comp.SuccessSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("rmc-fishing-success", ("item", caught)), ent.Owner, user, PopupType.Medium);

        if (hadBait)
            QueueDel(baitUid);

        ClearRodState(ent);
    }

    private void FailBite(Entity<RMCFishingRodComponent> ent, EntityUid? user, string locId)
    {
        _audio.PlayPvs(ent.Comp.FailSound, ent.Owner);
        if (user is { } fisher && !TerminatingOrDeleted(fisher))
            _popup.PopupEntity(Loc.GetString(locId), ent.Owner, fisher, PopupType.SmallCaution);

        ClearRodState(ent);
    }

    private void ClearRodState(Entity<RMCFishingRodComponent> ent, bool updateAppearance = true)
    {
        ent.Comp.State = RMCFishingRodState.Idle;
        ent.Comp.CurrentFisher = null;
        ent.Comp.BiteEndsAt = TimeSpan.Zero;
        ent.Comp.WaitToken++;
        ent.Comp.BiteToken++;
        Dirty(ent);

        if (updateAppearance)
            UpdateRodAppearance(ent);

        ClearFishingLine(ent.Owner);
    }

    private void UpdateRodAppearance(Entity<RMCFishingRodComponent> ent)
    {
        _appearance.SetData(ent.Owner, RMCFishingRodVisuals.Deployed, ent.Comp.Deployed);
        _appearance.SetData(ent.Owner, RMCFishingRodVisuals.State, ent.Comp.State);
    }

    private void RefreshFishingLine(Entity<RMCFishingRodComponent> ent, EntityCoordinates adjacent, bool biting)
    {
        ClearFishingLine(ent.Owner);

        _lineVisuals[ent.Owner] = Spawn(GetFishingLineVisualPrototype(ent.Comp.Direction, biting), adjacent);
    }

    private void ClearFishingLine(EntityUid rod)
    {
        if (!_lineVisuals.Remove(rod, out var line))
            return;

        QueueDel(line);
    }

    private static string GetFishingLineVisualPrototype(Direction direction, bool biting)
    {
        var state = biting ? "Bite" : "Cast";
        var directionName = direction switch
        {
            Direction.North => "North",
            Direction.South => "South",
            Direction.East => "East",
            Direction.West => "West",
            _ => "South",
        };

        return $"{FishingLineVisualPrototypePrefix}{state}{directionName}";
    }

    private bool TryGetBait(Entity<RMCFishingRodComponent> ent, out EntityUid bait)
    {
        bait = default;
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.BaitSlotId, out var baseContainer) ||
            baseContainer is not ContainerSlot { ContainedEntity: { } contained })
        {
            return false;
        }

        bait = contained;
        return true;
    }

    private bool TryGetRodWater(Entity<RMCFishingRodComponent> rod, EntityUid user, out EntityCoordinates adjacent, out EntityCoordinates target)
    {
        return TryGetRodWater(rod.Owner, rod.Comp.Direction, user, out adjacent, out target);
    }

    internal bool TryGetRodWater(EntityUid origin, Direction direction, EntityUid user, out EntityCoordinates adjacent, out EntityCoordinates target)
    {
        var originCoords = _transform.GetMoverCoordinates(origin).SnapToGrid(EntityManager);
        adjacent = originCoords.Offset(direction);
        target = adjacent.Offset(direction);

        // CMSS13 only checked the adjacent turf while deploying; RMC checks the final hook turf too.
        return IsFishableWater(adjacent, user) && IsFishableWater(target, user);
    }

    private bool IsFishableWater(EntityCoordinates coordinates, EntityUid user)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (!TryComp(uid, out RMCWaterComponent? water))
                continue;

            return _water.CanCollide((uid, water), user);
        }

        return false;
    }

    private bool TryPickLoot(
        EntityCoordinates coordinates,
        ProtoId<RMCFishingLootPrototype> fallback,
        int commonWeight,
        int uncommonWeight,
        int rareWeight,
        int ultraRareWeight,
        RMCFishBaitComponent? bait,
        out EntProtoId loot)
    {
        loot = default;
        var tableId = fallback;
        if (_area.TryGetArea(coordinates, out var areaEnt, out _) &&
            areaEnt.Value.Comp.FishingLoot is { } areaLoot)
        {
            tableId = areaLoot;
        }

        if (!_prototype.TryIndex(tableId, out var table))
            return false;

        var common = ClampChance(commonWeight + (bait?.CommonModifier ?? 0));
        var uncommon = ClampChance(uncommonWeight + (bait?.UncommonModifier ?? 0));
        var rare = ClampChance(rareWeight + (bait?.RareModifier ?? 0));
        var ultraRare = ClampChance(ultraRareWeight + (bait?.UltraRareModifier ?? 0));

        // CMSS13 used sequential prob() checks, so these chances intentionally do not normalize.
        if (_random.Prob(common) && TryPick(table.Common, out loot))
            return true;
        if (_random.Prob(uncommon) && TryPick(table.Uncommon, out loot))
            return true;
        if (_random.Prob(rare) && TryPick(table.Rare, out loot))
            return true;
        if (_random.Prob(ultraRare) && TryPick(table.UltraRare, out loot))
            return true;

        return TryPick(table.Common, out loot);
    }

    private bool TryPick(IReadOnlyList<EntProtoId> entries, out EntProtoId picked)
    {
        picked = default;
        if (entries.Count == 0)
            return false;

        picked = entries[_random.Next(entries.Count)];
        return true;
    }

    private static float ClampChance(int chance)
    {
        return Math.Clamp(chance, 0, 100) / 100f;
    }

    private TimeSpan RandomTime(TimeSpan min, TimeSpan max)
    {
        if (max <= min)
            return min;

        return TimeSpan.FromSeconds(_random.NextDouble(min.TotalSeconds, max.TotalSeconds));
    }

    private void OnFishMapInit(Entity<RMCFishComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Length <= 0)
            ent.Comp.Length = _random.Next(ent.Comp.MinLength, ent.Comp.MaxLength + 1);

        Dirty(ent);
        UpdateFishAppearance(ent);
    }

    private void OnFishExamined(Entity<RMCFishComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-fishing-fish-length", ("length", ent.Comp.Length)));
        if (ent.Comp.Gutted)
            args.PushMarkup(Loc.GetString("rmc-fishing-fish-gutted"));
    }

    private void OnFishInteractUsing(Entity<RMCFishComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<SharpComponent>(args.Used))
            return;

        args.Handled = true;
        if (!ent.Comp.Guttable)
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-fish-not-guttable"), ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Gutted)
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-fish-already-gutted"), ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(args.User);
        SpawnGutEntries(ent.Comp.BaseGutSpawns, coordinates);

        var sides = Math.Max(0, (int) Math.Floor(ent.Comp.Length / 2f) - ent.Comp.MinLength);
        var extra = sides <= 0 ? 1 : _random.Next(1, sides + 1);
        for (var i = 0; i < extra; i++)
            SpawnGutEntries(ent.Comp.ExtraGutSpawns, coordinates);

        ent.Comp.Gutted = true;
        Dirty(ent);
        UpdateFishAppearance(ent);
        _popup.PopupEntity(Loc.GetString("rmc-fishing-fish-gut-success", ("fish", ent.Owner)), ent.Owner, args.User);
    }

    private void SpawnGutEntries(List<EntitySpawnEntry> entries, EntityCoordinates coordinates)
    {
        foreach (var id in EntitySpawnCollection.GetSpawns(entries, _random))
        {
            if (string.IsNullOrEmpty(id))
                continue;

            Spawn(id, coordinates);
        }
    }

    private void UpdateFishAppearance(Entity<RMCFishComponent> ent)
    {
        _appearance.SetData(ent.Owner, RMCFishVisuals.Gutted, ent.Comp.Gutted);
    }

    private void OnSpearAfterInteract(Entity<RMCFishingSpearComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || ent.Comp.Busy)
            return;

        if (!IsFishableWater(args.ClickLocation.SnapToGrid(EntityManager), args.User))
            return;

        args.Handled = true;
        ent.Comp.Busy = true;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("rmc-fishing-spear-start"), args.User, args.User);
        var ev = new RMCFishingSpearDoAfterEvent(GetNetCoordinates(args.ClickLocation.SnapToGrid(EntityManager)));
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent.Owner, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ent.Comp.Busy = false;
            Dirty(ent);
        }
    }

    private void OnSpearDoAfter(Entity<RMCFishingSpearComponent> ent, ref RMCFishingSpearDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Busy = false;
        Dirty(ent);

        if (args.Cancelled)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!IsFishableWater(coordinates, args.User))
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-invalid-water"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (_random.Prob(ent.Comp.FailChance) ||
            !TryPickLoot(coordinates, ent.Comp.Loot, ent.Comp.CommonWeight, ent.Comp.UncommonWeight, ent.Comp.RareWeight, ent.Comp.UltraRareWeight, null, out var loot))
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-spear-fail"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var caught = Spawn(loot, coordinates);
        if (_hands.TryPickupAnyHand(args.User, caught))
        {
            _popup.PopupEntity(Loc.GetString("rmc-fishing-spear-success-hand", ("item", caught)), args.User, args.User);
            return;
        }

        _throwing.TryThrow(caught, _transform.GetMoverCoordinates(args.User), 2f, args.User, compensateFriction: true);
        _popup.PopupEntity(Loc.GetString("rmc-fishing-spear-success-water", ("item", caught)), args.User, args.User);
    }
}
