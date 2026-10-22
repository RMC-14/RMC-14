using System.Linq;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Effects.Negative;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCReactionIndicatorEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChemicalReactionSystem _reaction = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId GlowProto = "RMCChemGlow";
    private static readonly EntProtoId FireProto = "RMCChemFire";
    private static readonly EntProtoId SmokeProto = "RMCChemSmoke";

    private static readonly ProtoId<ReagentPrototype> WaterReagent = "Water";

    private static readonly SoundSpecifier BubblingSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
    private static readonly SoundSpecifier SplashSound = new SoundCollectionSpecifier("XenoAcidSizzle");

    private static readonly TimeSpan EndothermicDuration = TimeSpan.FromSeconds(2);

    public override void Initialize()
    {
        SubscribeLocalEvent<ExecuteEntityEffectEvent<RMCReactionIndicatorEffect>>(OnReactionIndicator);
        SubscribeLocalEvent<RMCEndothermicLockoutComponent, ReactionAttemptEvent>(OnReactionAttempt);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCEndothermicLockoutComponent, SolutionComponent>();
        while (query.MoveNext(out var uid, out var lockout, out var solution))
        {
            if (_timing.CurTime < lockout.LockedUntil)
                continue;

            RemComp<RMCEndothermicLockoutComponent>(uid);
            _reaction.FullyReactSolution((uid, solution));
        }
    }

    private void OnReactionIndicator(ref ExecuteEntityEffectEvent<RMCReactionIndicatorEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs reagentArgs)
            return;

        var uid = reagentArgs.TargetEntity;
        var coords = Transform(uid).Coordinates;

        switch (args.Effect.Indicator)
        {
            case ReactionIndicator.Glowing:
                Spawn(GlowProto, coords);
                _popup.PopupEntity("The reaction starts to glow!", uid, PopupType.Medium);
                break;
            case ReactionIndicator.Fire:
                HandleFire(uid, coords, reagentArgs);
                break;
            case ReactionIndicator.Smoking:
                Spawn(SmokeProto, coords);
                _popup.PopupEntity("The reaction gives off heavy fumes!", uid, PopupType.Medium);
                break;
            case ReactionIndicator.Endothermic:
                var lockout = EnsureComp<RMCEndothermicLockoutComponent>(uid);
                lockout.LockedUntil = _timing.CurTime + EndothermicDuration;
                Dirty(uid, lockout);
                _popup.PopupEntity("The reaction slows and cools!", uid, PopupType.Medium);
                break;
            case ReactionIndicator.Bubbling:
                HandleBubbling(uid, coords, reagentArgs);
                break;
        }
    }

    private void HandleFire(EntityUid uid, EntityCoordinates coords, EntityEffectReagentArgs args)
    {
        // if holder already has at least as much water as the volume of reagent that
        // was just created, the mix boils & settles instead of igniting
        if (args.Source is { } source && source.GetTotalPrototypeQuantity(WaterReagent) >= args.Quantity)
        {
            _popup.PopupEntity("The reaction begins to boil before settling down.", uid, PopupType.Medium);
            return;
        }

        _flammable.SpawnFireDiamond(FireProto, coords, 1);
        _popup.PopupEntity("The reaction starts to smoke before a small combustion!", uid, PopupType.MediumCaution);
    }

    private void HandleBubbling(EntityUid uid, EntityCoordinates coords, EntityEffectReagentArgs args)
    {
        if (TryComp<OpenableComponent>(uid, out var openable) && !openable.Opened)
            return;

        _audio.PlayPvs(BubblingSound, uid);
        _popup.PopupEntity("The solution begins to bubble aggressively!", uid, PopupType.MediumCaution);

        if (args.Source is not { } source || source.Volume <= FixedPoint2.Zero)
            return;

        var nearby = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(coords, 1.5f, nearby);

        foreach (var mob in nearby)
        {
            if (source.Volume <= FixedPoint2.Zero)
                break;

            var armorEv = new CMGetArmorEvent(SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING);
            RaiseLocalEvent(mob.Owner, ref armorEv);

            if (_random.Prob(Math.Min(armorEv.Bio * 2, 100) / 100f))
            {
                _popup.PopupEntity("Your gear protects you from a chunk of foam and bubbles!", mob.Owner, mob.Owner);
                continue;
            }

            var splash = source.SplitSolution(FixedPoint2.Min(FixedPoint2.New(5), source.Volume));
            _reactive.DoEntityReaction(mob.Owner, splash, ReactionMethod.Touch);
            _popup.PopupEntity("Chemicals splash on you!", mob.Owner, mob.Owner, PopupType.LargeCaution);
            _audio.PlayPvs(SplashSound, mob.Owner);

            if (ContainsCorrosive(splash))
            {
                var filter = Filter.Pvs(mob.Owner, entityManager: EntityManager);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { mob.Owner }, filter);
            }
        }
    }

    private bool ContainsCorrosive(Solution solution)
    {
        foreach (var quantity in solution.Contents)
        {
            if (_rmcReagent.HasProperty<Corrosive>(quantity.Reagent.Prototype))
                return true;
        }

        return false;
    }

    private void OnReactionAttempt(Entity<RMCEndothermicLockoutComponent> ent, ref ReactionAttemptEvent args)
    {
        if (_timing.CurTime < ent.Comp.LockedUntil)
            args.Cancelled = true;
    }
}
