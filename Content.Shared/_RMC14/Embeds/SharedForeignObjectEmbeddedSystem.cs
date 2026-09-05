using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Alert;
using Content.Shared._RMC14.Medical.Surgery.Tools;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Embeds;

public abstract partial class SharedForeignObjectEmbeddedSystem : EntitySystem
{
    private static readonly ProtoId<AlertPrototype> EmbeddedObjectAlert = "ForeignObjectEmbedded";
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShrapnelChanceProjectileComponent, ProjectileHitEvent>(OnShrapnelChanceProjectileHit);
        SubscribeLocalEvent<EmbeddedMovementDamageComponent, MoveEvent>(OnEmbeddedMovementDamageMove);
        SubscribeLocalEvent<ForeignObjectEmbeddedComponent, ForeignObjectSelfExtractionAlertEvent>(OnSelfExtractionAlert);
        SubscribeLocalEvent<ForeignObjectEmbeddedComponent, ForeignObjectSelfExtractionDoAfterEvent>(OnSelfExtractionDoAfter);
    }

    private void OnSelfExtractionAlert(Entity<ForeignObjectEmbeddedComponent> ent, ref ForeignObjectSelfExtractionAlertEvent args)
    {
        if (ent.Comp.StackCount <= 0 || !_hands.TryGetActiveItem(ent.Owner, out var held) ||
            !TryComp(held, out RMCSurgeryToolComponent? tool) ||
            !tool.ToolTypes.Any(type => type.Kind == RMCSurgeryToolKind.Scalpel))
        {
            return;
        }

        var entries = ent.Comp.Entries.Where(entry => entry.Quantity > 0).ToArray();
        if (entries.Length == 0)
            return;

        var selected = entries[_random.Next(entries.Length)];
        var doAfter = new DoAfterArgs(
            EntityManager,
            ent,
            20f,
            new ForeignObjectSelfExtractionDoAfterEvent(selected.BodyPart, selected.Symmetry),
            ent,
            ent,
            held)
        {
            NeedHand = true,
            BreakOnMove = true,
            TargetEffect = "RMCEffectHealBusy",
            MovementThreshold = 0.5f,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            args.Handled = true;
    }

    private void OnSelfExtractionDoAfter(Entity<ForeignObjectEmbeddedComponent> ent, ref ForeignObjectSelfExtractionDoAfterEvent args)
    {
        if (args.Cancelled || args.Used is not { } used || !TryComp(used, out RMCSurgeryToolComponent? tool) ||
            !_hands.TryGetActiveItem(ent.Owner, out var held) || held != used ||
            !tool.ToolTypes.Any(type => type.Kind == RMCSurgeryToolKind.Scalpel))
        {
            return;
        }

        if (_net.IsClient)
            return;

        var damage = new DamageSpecifier();
        if (!_random.Prob(0.75f))
        {
            damage.DamageDict["Blunt"] = FixedPoint2.New(10);
            _damageable.TryChangeDamage(ent, damage, true, origin: ent, tool: held);
            _popup.PopupEntity(
                Loc.GetString("rmc-embedded-self-extraction-failed"),
                ent,
                ent,
                PopupType.SmallCaution);
            _emote.TryEmoteWithChat(ent, "Scream", forceEmote: true);
            return;
        }

        if (!ForeignObjectEmbeddedUtility.TryRemoveMatchingBodyPart(ent.Comp, args.BodyPart, args.Symmetry, 1))
            return;

        damage.DamageDict["Blunt"] = FixedPoint2.New(5);
        _damageable.TryChangeDamage(ent, damage, true, origin: ent, tool: held);
        _popup.PopupEntity(
            Loc.GetString("rmc-embedded-self-extraction-success"),
            ent,
            ent,
            PopupType.Small);

        if (ent.Comp.StackCount <= 0)
        {
            _alerts.ClearAlert(ent, EmbeddedObjectAlert);
            RemCompDeferred<ForeignObjectEmbeddedComponent>(ent);
            RemCompDeferred<EmbeddedMovementDamageComponent>(ent);
        }

        Dirty(ent, ent.Comp);
    }

    private void OnShrapnelChanceProjectileHit(Entity<ShrapnelChanceProjectileComponent> projectile, ref ProjectileHitEvent args)
    {
        if (!CanBeEmbedded(args.Target))
            return;

        var chance = projectile.Comp.EmbedChance;
        if (chance > 1f)
            chance /= 100f;

        if (_random.Prob(chance))
        {
            var selectedBodyPart = projectile.Comp.RandomizeBodyPart
                ? ForeignObjectEmbeddedUtility.SelectRandomBodyPartAndSymmetry()
                : (BodyPartType.Torso, BodyPartSymmetry.None);
            //Todo: When body part targeting is implmented, extend this to target specifc parts.
            TryEmbed(args.Target, projectile.Comp.SourceId, projectile.Comp.Count, selectedBodyPart.Item1, selectedBodyPart.Item2);
        }
    }

    public bool CanBeEmbedded(EntityUid uid)
    {
        return HasComp<ForeignObjectEmbeddableComponent>(uid) && !HasComp<XenoComponent>(uid);
    }

    public bool TryEmbed(EntityUid target, string sourceId, int count = 1, BodyPartType? bodyPart = null, BodyPartSymmetry symmetry = BodyPartSymmetry.None)
    {
        if (!CanBeEmbedded(target) || count <= 0)
            return false;

        var embedded = EnsureComp<ForeignObjectEmbeddedComponent>(target);
        var selectedBodyPart = bodyPart is { } specifiedBodyPart
            ? (specifiedBodyPart, symmetry)
            : ForeignObjectEmbeddedUtility.SelectRandomBodyPartAndSymmetry();
        ForeignObjectEmbeddedUtility.InitializeTickState(embedded, _timing.CurTime);
        ForeignObjectEmbeddedUtility.AddEntry(
            embedded,
            sourceId,
            selectedBodyPart.Item1,
            count,
            selectedBodyPart.Item2
        );
        EnsureMovementDamageState(target);
        _alerts.ShowAlert(target, EmbeddedObjectAlert);
        Dirty(target, embedded);
        return true;
    }

    public int GetStackCount(EntityUid target)
    {
        if (!TryComp<ForeignObjectEmbeddedComponent>(target, out var embedded))
            return 0;

        return embedded.StackCount;
    }

    public bool CanApplyMovementDamage(EntityUid uid)
    {
        if (!TryComp<ForeignObjectEmbeddedComponent>(uid, out _) || !TryComp<EmbeddedMovementDamageComponent>(uid, out _))
            return false;

        if (TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled)
            return false;

        return true;
    }

    public void EnsureMovementDamageState(EntityUid uid)
    {
        if (!TryComp<ForeignObjectEmbeddedComponent>(uid, out _))
            return;

        EnsureComp<EmbeddedMovementDamageComponent>(uid);
    }

    private void OnEmbeddedMovementDamageMove(Entity<EmbeddedMovementDamageComponent> ent, ref MoveEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.OldPosition == args.NewPosition)
            return;

        if (!CanApplyMovementDamage(ent.Owner))
            return;

        var stackCount = GetStackCount(ent.Owner);
        if (stackCount <= 0)
            return;

        if (args.NewPosition.TryDistance(EntityManager, _transform, args.OldPosition, out var distance))
        {
            ent.Comp.DistanceMoved += (float) distance;
            Dirty(ent, ent.Comp);
        }

        if (ent.Comp.DistanceMoved < ent.Comp.DistanceThreshold)
            return;

        ent.Comp.MovementWarningCounter++;
        if (ent.Comp.MovementWarningCounter >= 5) //The pop ups spam like crazy at 1 per tile, this mitigates to 1 every x tiles.
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-embedded-movement-pain"),
                ent.Owner,
                ent.Owner,
                PopupType.SmallCaution);
            ent.Comp.MovementWarningCounter = 0;
        }

        var damage = new DamageSpecifier();
        damage.DamageDict["Blunt"] = FixedPoint2.New(ent.Comp.DamagePerEmbedded * stackCount);

        _damageable.TryChangeDamage(ent.Owner, damage, true, origin: ent.Owner, tool: ent.Owner);
        ent.Comp.DistanceMoved = 0f;
        Dirty(ent, ent.Comp);
    }

    public bool TryRemoveStacks(EntityUid target, int count = 1)
    {
        if (!TryComp<ForeignObjectEmbeddedComponent>(target, out var embedded) || count <= 0)
            return false;

        if (count >= embedded.StackCount)
        {
            _alerts.ClearAlert(target, EmbeddedObjectAlert);
            RemCompDeferred<ForeignObjectEmbeddedComponent>(target);
            RemCompDeferred<EmbeddedMovementDamageComponent>(target);
            return true;
        }

        embedded.StackCount -= count;
        while (count > 0 && embedded.Entries.Count > 0)
        {
            var entry = embedded.Entries[^1];
            if (entry.Quantity <= count)
            {
                count -= entry.Quantity;
                embedded.Entries.RemoveAt(embedded.Entries.Count - 1);
                continue;
            }

            entry.Quantity -= count;
            embedded.Entries[^1] = entry;
            count = 0;
        }

        if (embedded.StackCount <= 0)
        {
            _alerts.ClearAlert(target, EmbeddedObjectAlert);
            RemCompDeferred<EmbeddedMovementDamageComponent>(target);
        }

        Dirty(target, embedded);
        return true;
    }

}
