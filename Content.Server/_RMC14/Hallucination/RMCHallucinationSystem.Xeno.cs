using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared._RMC14.MotionDetector;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Doors.Components;
using Content.Shared.Humanoid;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly string[] MinorPainSymptoms = ["stomach", "chest"];
    private static readonly string[] ThroatSymptoms = ["sore", "mucous"];
    private static readonly string[] MajorPainSymptoms = ["chest", "breathing", "heart"];

    private static readonly (EntProtoId Prototype, float Speed)[] XenoSightings =
    [
        ("RMCHallucinationRunner", 6),
        ("RMCHallucinationDrone", 4),
        ("RMCHallucinationLurker", 5),
    ];

    private static readonly EntProtoId ParasiteVisual = "RMCHallucinationParasite";
    private static readonly EntProtoId ParasiteFaceVisual = "RMCHallucinationParasiteFace";
    private static readonly TimeSpan ParasiteLeapDelay = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan ParasiteLeapTime = TimeSpan.FromSeconds(0.35);

    private static readonly EntProtoId LurkerVisual = "RMCHallucinationLurker";
    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";
    private static readonly Vector2i[] WeedDirections = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];
    private static readonly ProtoId<TagPrototype> SpreaderIgnoreTag = "SpreaderIgnore";

    private readonly HashSet<Entity<DoorComponent>> _doors = new();

    private const float DoorPryMinDistance = 2;
    private const float FakeBlipStep = 0.75f;
    private const int XenoSightingMaxTiles = 10;
    private const float ParasiteRunSpeed = 5;
    private const float ParasiteLeapRange = 2;

    private void InitializeXeno()
    {
        SubscribeLocalEvent<RMCHallucinatingComponent, MotionDetectorScanEvent>(OnMotionDetectorScan);

        SubscribeLocalEvent<RMCHallucinationDoorPryComponent, RMCHallucinateEvent>(OnDoorPry);
        SubscribeLocalEvent<RMCHallucinationFakeChestburstComponent, RMCHallucinateEvent>(OnFakeChestburst);
        SubscribeLocalEvent<RMCHallucinationEdgeLurkerComponent, RMCHallucinateEvent>(OnEdgeLurker);
        SubscribeLocalEvent<RMCHallucinationFakeWeedsComponent, RMCHallucinateEvent>(OnFakeWeeds);

        SubscribeLocalEvent<RMCHallucinationXenoDisguiseComponent, RMCHallucinateEvent>(OnXenoDisguise);
        SubscribeLocalEvent<RMCHallucinationFakeInfectionComponent, RMCHallucinateEvent>(OnFakeInfection);
        SubscribeLocalEvent<RMCHallucinationFakeQueenComponent, RMCHallucinateEvent>(OnFakeQueen);
        SubscribeLocalEvent<RMCHallucinationMotionBlipsComponent, RMCHallucinateEvent>(OnMotionBlips);
        SubscribeLocalEvent<RMCHallucinationXenoSightingComponent, RMCHallucinateEvent>(OnXenoSighting);
        SubscribeLocalEvent<RMCHallucinationFakeParasiteComponent, RMCHallucinateEvent>(OnFakeParasite);
    }

    private void OnXenoDisguise(Entity<RMCHallucinationXenoDisguiseComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeXenoDisguise(args.Victim);
    }

    private void OnFakeInfection(Entity<RMCHallucinationFakeInfectionComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeInfection(args.Victim);
    }

    private void OnFakeQueen(Entity<RMCHallucinationFakeQueenComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeQueen(args.Victim);
    }

    private void OnMotionBlips(Entity<RMCHallucinationMotionBlipsComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeMotionBlips(args.Victim);
    }

    private void OnXenoSighting(Entity<RMCHallucinationXenoSightingComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeXenoSighting(args.Victim);
    }

    private void OnFakeParasite(Entity<RMCHallucinationFakeParasiteComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeParasite(args.Victim);
    }

    private void OnDoorPry(Entity<RMCHallucinationDoorPryComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeDoorPry(args.Victim);
    }

    private void OnFakeChestburst(Entity<RMCHallucinationFakeChestburstComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeChestburst(args.Victim);
    }

    private void OnEdgeLurker(Entity<RMCHallucinationEdgeLurkerComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeEdgeLurker(args.Victim);
    }

    private void OnFakeWeeds(Entity<RMCHallucinationFakeWeedsComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeWeeds(args.Victim);
    }

    private bool FakeDoorPry(Entity<RMCHallucinatingComponent> ent)
    {
        var victim = _transform.GetMapCoordinates(ent);
        var doors = new List<EntityUid>();
        _doors.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.ViewRange, _doors);
        foreach (var door in _doors)
        {
            if (door.Comp.State != DoorState.Closed ||
                (_transform.GetMapCoordinates(door).Position - victim.Position).Length() < DoorPryMinDistance)
            {
                continue;
            }

            doors.Add(door);
        }

        if (doors.Count == 0)
            return false;

        var coords = Transform(_random.Pick(doors)).Coordinates;
        AddStep(ent, TimeSpan.Zero, new RMCHallucinationStep { Coordinates = coords, Sound = ent.Comp.DoorPrySound });
        AddStep(ent, RandomTime(1, 1.5f), new RMCHallucinationStep { Coordinates = coords, Sound = ent.Comp.DoorPrySound });
        AddStep(ent, RandomTime(2.5f, 3.5f), new RMCHallucinationStep { Coordinates = coords, Sound = ent.Comp.HissSound });
        return true;
    }

    private bool FakeChestburst(Entity<RMCHallucinatingComponent> ent)
    {
        var screamer = ent.Owner;
        var screamers = new List<EntityUid>();
        _humanoids.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.BodyCloneRange, _humanoids);
        foreach (var humanoid in _humanoids)
        {
            if (humanoid.Owner != ent.Owner && HasComp<VocalComponent>(humanoid))
                screamers.Add(humanoid);
        }

        if (screamers.Count > 0)
            screamer = _random.Pick(screamers);

        var coords = OffsetFromVictim(ent, 6, 9);
        if (TryComp(screamer, out VocalComponent? vocal) &&
            vocal.EmoteSounds is { } soundsId &&
            _prototypes.TryIndex(soundsId, out var sounds) &&
            (sounds.Sounds.GetValueOrDefault(ScreamEmote) ?? sounds.FallbackSound) is { } scream)
        {
            AddStep(ent, TimeSpan.Zero, new RMCHallucinationStep { Coordinates = coords, Sound = scream });
        }

        AddStep(ent, RandomTime(1.5f, 2.5f), new RMCHallucinationStep { Coordinates = coords, Sound = ent.Comp.ChestburstSound });
        return true;
    }

    private bool FakeEdgeLurker(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.LurkerEnd || !TryPickFloorInView(ent, out var coords, ent.Comp.ViewRange - 1.5f))
            return false;

        ent.Comp.LurkerEnd = time + ent.Comp.LurkerDuration;
        SendToVictim(ent, new RMCHallucinationLurkerEvent(
            LurkerVisual,
            GetNetCoordinates(coords),
            ent.Comp.LurkerDuration,
            ent.Comp.LurkerVanishRange,
            ent.Comp.LurkerHoverRange,
            ent.Comp.LurkerFade));
        return true;
    }

    private bool FakeWeeds(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.WeedsEnd ||
            !_prototypes.TryIndex(ent.Comp.WeedsNodePrototype, out var nodePrototype) ||
            !nodePrototype.TryGetComponent(out XenoWeedsComponent? nodeWeeds, _compFactory) ||
            !_prototypes.TryIndex(ent.Comp.WeedsPrototype, out var weedsPrototype) ||
            !weedsPrototype.TryGetComponent(out XenoWeedsSpreadingComponent? spreading, _compFactory) ||
            !TryPickFloorInView(ent, out var origin, 2) ||
            !TryComp(origin.EntityId, out MapGridComponent? gridComp))
        {
            return false;
        }

        var grid = new Entity<MapGridComponent>(origin.EntityId, gridComp);
        var node = _map.TileIndicesFor(grid, gridComp, origin);
        if (!CanFakeWeedsGrow(grid, node))
            return false;

        var range = nodeWeeds.Range;
        var steps = new Dictionary<Vector2i, int> { [node] = 0 };
        var queue = new Queue<Vector2i>();
        queue.Enqueue(node);

        var tiles = new List<Vector2i>();
        var tileSteps = new List<int>();
        var lastStep = 0;
        while (queue.TryDequeue(out var tile))
        {
            var step = steps[tile];
            tiles.Add(tile);
            tileSteps.Add(step);
            lastStep = Math.Max(lastStep, step);

            foreach (var dir in WeedDirections)
            {
                var next = tile + dir;
                var diff = next - node;
                if (steps.ContainsKey(next) ||
                    Math.Abs(diff.X) >= range ||
                    Math.Abs(diff.Y) >= range ||
                    !CanFakeWeedsGrow(grid, next))
                {
                    continue;
                }

                steps[next] = step + 1;
                queue.Enqueue(next);
            }
        }

        var fadeAt = spreading.SpreadDelay * lastStep + ent.Comp.WeedsLinger;
        ent.Comp.WeedsEnd = time + fadeAt + ent.Comp.WeedsFade;

        _audio.PlayStatic(ent.Comp.WeedsPlantSound, ent, origin);
        SendToVictim(ent, new RMCHallucinationWeedsEvent(ent.Comp.WeedsPrototype, GetNetEntity(grid), node, tiles, tileSteps, spreading.SpreadDelay, fadeAt, ent.Comp.WeedsFade));
        return true;
    }

    private bool CanFakeWeedsGrow(Entity<MapGridComponent> grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(grid, grid.Comp, tile, out var tileRef) ||
            tileRef.Tile.IsEmpty ||
            _turf.IsSpace(tileRef) ||
            !_xenoWeeds.CanSpreadWeedsPopup(grid, tile, null, null))
        {
            return false;
        }

        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid.Comp, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<XenoWeedsComponent>(uid))
                return false;

            if (TryComp(uid, out AirtightComponent? airtight) &&
                airtight.AirBlocked &&
                !_tag.HasTag(uid.Value, SpreaderIgnoreTag) &&
                !HasComp<AllowWeedSpreadComponent>(uid))
            {
                return false;
            }
        }

        return true;
    }

    private bool FakeXenoDisguise(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.DisguiseEnd)
            return false;

        var targets = new List<EntityUid>();
        _humanoids.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.ViewRange, _humanoids);
        foreach (var humanoid in _humanoids)
        {
            if (humanoid.Owner == ent.Owner ||
                !_mobState.IsAlive(humanoid) ||
                !_examine.InRangeUnOccluded(ent, humanoid, ent.Comp.ViewRange + 1))
            {
                continue;
            }

            targets.Add(humanoid);
        }

        if (targets.Count == 0)
            return false;

        ent.Comp.DisguiseEnd = time + ent.Comp.DisguiseDuration;
        var (xeno, _) = _random.Pick(XenoSightings);
        SendToVictim(ent, new RMCHallucinationDisguiseEvent(GetNetEntity(_random.Pick(targets)), xeno, ent.Comp.DisguiseDuration));
        return true;
    }

    private bool FakeInfection(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.FakeInfectionEnd || HasComp<VictimInfectedComponent>(ent))
            return false;

        ent.Comp.FakeInfectionStart = time;
        ent.Comp.FakeInfectionEnd = time + ent.Comp.FakeInfectionDuration;
        ent.Comp.NextFakeSymptom = time + RandomTime(2, 5);
        return true;
    }

    private void UpdateFakeInfection(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (ent.Comp.NextFakeSymptom is not { } next || time < next)
            return;

        if (time >= ent.Comp.FakeInfectionEnd)
        {
            ent.Comp.NextFakeSymptom = null;
            _popup.PopupEntity(Loc.GetString("rmc-xeno-infection-burst-soon-self"), ent, ent, PopupType.MediumCaution);
            return;
        }

        ent.Comp.NextFakeSymptom = time + RandomTime(8, 15);
        var progress = (time - ent.Comp.FakeInfectionStart) / ent.Comp.FakeInfectionDuration;
        var (message, popup) = progress switch
        {
            < 0.33 => (Loc.GetString("rmc-xeno-infection-minorpain-" + _random.Pick(MinorPainSymptoms)), PopupType.SmallCaution),
            < 0.66 => (_random.Prob(0.5f)
                ? Loc.GetString("rmc-xeno-infection-throat-" + _random.Pick(ThroatSymptoms))
                : Loc.GetString("rmc-xeno-infection-muscle-ache"), PopupType.SmallCaution),
            < 0.9 => (Loc.GetString("rmc-xeno-infection-majorpain-" + _random.Pick(MajorPainSymptoms)), PopupType.SmallCaution),
            _ => (Loc.GetString("rmc-xeno-infection-shakes-self"), PopupType.MediumCaution),
        };

        _popup.PopupEntity(message, ent, ent, popup);
    }

    private bool FakeQueen(Entity<RMCHallucinatingComponent> ent)
    {
        if (ent.Comp.NextQueenStep != null ||
            Transform(ent).GridUid is not { } grid ||
            !TryGetLocalPosition(ent, grid, out var victim))
        {
            return false;
        }

        ent.Comp.QueenGrid = grid;
        ent.Comp.QueenPosition = victim + _random.NextAngle().ToVec() * ent.Comp.QueenStartDistance;
        ent.Comp.QueenStepsLeft = (int) ent.Comp.QueenStartDistance * 2;
        ent.Comp.NextQueenStep = _timing.CurTime;
        return true;
    }

    private void UpdateFakeQueen(Entity<RMCHallucinatingComponent> ent)
    {
        if (ent.Comp.NextQueenStep is not { } next || _timing.CurTime < next)
            return;

        if (ent.Comp.QueenGrid is not { } grid ||
            ent.Comp.QueenStepsLeft <= 0 ||
            !TryGetLocalPosition(ent, grid, out var victim))
        {
            EndFakeQueen(ent);
            return;
        }

        if (Vector2.Distance(ent.Comp.QueenPosition, victim) <= 1)
        {
            _audio.PlayStatic(ent.Comp.QueenGrowlSound, ent, new EntityCoordinates(grid, ent.Comp.QueenPosition));
            EndFakeQueen(ent);
            return;
        }

        ent.Comp.QueenStepsLeft--;
        ent.Comp.NextQueenStep = next + ent.Comp.QueenStepEvery;
        ent.Comp.QueenPosition += Vector2.Normalize(victim - ent.Comp.QueenPosition);
        _audio.PlayStatic(ent.Comp.QueenStepSound, ent, new EntityCoordinates(grid, ent.Comp.QueenPosition));
    }

    private void EndFakeQueen(Entity<RMCHallucinatingComponent> ent)
    {
        ent.Comp.NextQueenStep = null;
        ent.Comp.QueenGrid = null;
    }

    private bool FakeMotionBlips(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.FakeBlipsEnd || !IsUsingMotionDetector(ent))
            return false;

        ent.Comp.FakeBlipsEnd = time + RandomTime(10, 20);
        ent.Comp.FakeBlips.Clear();
        var count = _random.Next(1, 5);
        for (var i = 0; i < count; i++)
        {
            ent.Comp.FakeBlips.Add(_random.NextAngle().ToVec() * _random.NextFloat(4, 7));
        }

        return true;
    }

    private bool IsUsingMotionDetector(EntityUid uid)
    {
        var query = EntityQueryEnumerator<MotionDetectorComponent>();
        while (query.MoveNext(out var detector))
        {
            if (detector.Enabled && detector.LastUser == uid)
                return true;
        }

        return false;
    }

    private void OnMotionDetectorScan(Entity<RMCHallucinatingComponent> ent, ref MotionDetectorScanEvent args)
    {
        if (_timing.CurTime >= ent.Comp.FakeBlipsEnd || ent.Comp.FakeBlips.Count == 0)
            return;

        var center = _transform.GetMapCoordinates(ent);
        for (var i = 0; i < ent.Comp.FakeBlips.Count; i++)
        {
            var offset = ent.Comp.FakeBlips[i];
            if (offset.Length() > args.Range)
                offset = Vector2.Normalize(offset) * args.Range;

            args.Blips.Add(new Blip(new MapCoordinates(center.Position + offset, center.MapId), false));

            if (offset.Length() > FakeBlipStep + 1)
                offset -= Vector2.Normalize(offset) * FakeBlipStep;

            ent.Comp.FakeBlips[i] = offset;
        }
    }

    private bool FakeXenoSighting(Entity<RMCHallucinatingComponent> ent)
    {
        if (Transform(ent).GridUid is not { } grid ||
            !TryComp(grid, out MapGridComponent? gridComp) ||
            !TryGetLocalPosition(ent, grid, out var victim) ||
            !TryPickFloorInView(ent, out var start, 4))
        {
            return false;
        }

        var away = start.Position - victim;
        var side = new Vector2(-away.Y, away.X);
        if (_random.Prob(0.5f))
            side = -side;

        side = Vector2.Normalize(side);
        var end = start.Position;
        var length = 0;
        while (length < XenoSightingMaxTiles && !IsBlocked((grid, gridComp), end, side))
        {
            end += side;
            length++;
        }

        if (length < 3)
            return false;

        var (prototype, speed) = _random.Pick(XenoSightings);
        var travel = TimeSpan.FromSeconds(length / speed);
        SendToVictim(ent, new RMCHallucinationMovingVisualEvent(
            prototype,
            null,
            GetNetCoordinates(start),
            GetNetCoordinates(new EntityCoordinates(grid, end)),
            TimeSpan.Zero,
            travel,
            TimeSpan.Zero));

        return true;
    }

    private bool FakeParasite(Entity<RMCHallucinatingComponent> ent)
    {
        if (ent.Comp.Parasite != null ||
            Transform(ent).GridUid is not { } grid ||
            !TryPickFloorInView(ent, out var start, ParasiteLeapRange + 2))
        {
            return false;
        }

        var giveUpAt = _timing.CurTime + ent.Comp.ParasiteGiveUp;
        ent.Comp.ParasiteGrid = grid;
        ent.Comp.Parasite = new RMCHallucinationParasiteChase(start.Position, ParasiteRunSpeed, ParasiteLeapRange, ParasiteLeapDelay, ParasiteLeapTime, giveUpAt);

        _audio.PlayStatic(ent.Comp.SkitterSound, ent, start);
        SendToVictim(ent, new RMCHallucinationParasiteEvent(
            ParasiteVisual,
            GetNetCoordinates(start),
            ParasiteRunSpeed,
            ParasiteLeapRange,
            ParasiteLeapDelay,
            ParasiteLeapTime,
            ent.Comp.ParasiteGiveUp));

        return true;
    }

    private void UpdateFakeParasite(Entity<RMCHallucinatingComponent> ent, float frameTime)
    {
        if (ent.Comp.Parasite is not { } parasite)
            return;

        var time = _timing.CurTime;
        if (time >= parasite.GiveUpAt ||
            ent.Comp.ParasiteGrid is not { } grid ||
            !TryGetLocalPosition(ent, grid, out var victim))
        {
            ent.Comp.Parasite = null;
            return;
        }

        if (!parasite.Update(victim, time, frameTime))
            return;

        ent.Comp.Parasite = null;
        if (_mobState.IsIncapacitated(ent))
            return;

        _stun.TryStun(ent, ent.Comp.ParasiteKnockdown, true);
        _stun.TryKnockdown(ent, ent.Comp.ParasiteKnockdown, true);
        _popup.PopupEntity(Loc.GetString("rmc-hallucination-parasite-latch"), ent, ent, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("rmc-hallucination-parasite-collapse", ("user", ent.Owner)), ent, Filter.PvsExcept(ent), true, PopupType.MediumCaution);

        if (TryComp(ent, out InfectableComponent? infectable) &&
            TryComp(ent, out HumanoidAppearanceComponent? appearance) &&
            infectable.Sound.TryGetValue(appearance.Sex, out var sound))
        {
            _audio.PlayGlobal(sound, ent);
        }

        SendToVictim(ent, new RMCHallucinationFaceEvent(ParasiteFaceVisual, ent.Comp.ParasiteFaceDuration, ent.Comp.ParasiteFaceFade));
    }
}
