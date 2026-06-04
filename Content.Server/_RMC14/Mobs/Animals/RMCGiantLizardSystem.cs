using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Atmos;
using Content.Server._RMC14.Barricade;
using Content.Server._RMC14.NPC;
using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Spider;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly DirectionalAttackBlockSystem _directionalBlock = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private readonly Dictionary<EntityUid, EntityUid> _lastFoodHolder = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGiantLizardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCGiantLizardComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCGiantLizardComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCGiantLizardComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<RMCGiantLizardComponent, InteractHandEvent>(OnInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCGiantLizardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCGiantLizardComponent, DisarmedEvent>(OnDisarmed);
        SubscribeLocalEvent<RMCGiantLizardComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCGiantLizardComponent, RMCGiantLizardPounceActionEvent>(OnPounceAction);
        SubscribeLocalEvent<RMCGiantLizardComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RMCGiantLizardComponent, PhysicsSleepEvent>(OnPhysicsSleep);
        SubscribeLocalEvent<RMCGiantLizardComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<RMCGiantLizardComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<FoodComponent, GotEquippedHandEvent>(OnFoodPickedUp);
        SubscribeLocalEvent<FoodComponent, GotUnequippedHandEvent>(OnFoodDropped);
        SubscribeLocalEvent<FoodComponent, EntityTerminatingEvent>(OnFoodTerminating);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCGiantLizardComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var lizard, out var xform))
        {
            if (!MobState.IsAlive(uid))
                continue;

            var ent = (uid, lizard, xform);
            UpdatePossession(ent);
            UpdateBleedTrail((uid, lizard));
            UpdateStatusRecovery((uid, lizard));

            if (ActorQuery.HasComp(uid))
                continue;

            if (lizard.Leaping)
            {
                UpdatePounce(ent);
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (UpdateRavage((uid, lizard)))
            {
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (UpdateRetreat(ent))
            {
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (UpdateSkirmish(ent))
            {
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (lizard.NextUpdateAt > now)
                continue;

            lizard.NextUpdateAt = now + lizard.UpdateCooldown;

            UpdateTongueFlick((uid, lizard));
            UpdateLizardVisuals((uid, lizard));

            if (TryFirePanic((uid, lizard)))
                continue;

            DecayAggression((uid, lizard));

            var target = PickLizardTarget(ent);
            if (target == null)
            {
                if (WarnOrAggroCloseThreat(ent))
                {
                    StopRoam((uid, lizard), false);
                    continue;
                }

                if (TryAiFeed(ent))
                    continue;

                UpdateIdleRest(ent);
                if (!lizard.Resting)
                    UpdateCalmRoam(ent);

                continue;
            }

            StopRoam((uid, lizard), false);

            if (lizard.FoodTarget != null || lizard.EatingFood)
                LoseFoodTarget((uid, lizard));

            WakeRest((uid, lizard));

            if (TryStartDesperateRetreat(ent, target.Value))
                continue;

            TryAggro(uid, target.Value, lizard);
            AlertPack(uid, target.Value, lizard);
            TryBreakNearbyObstacle(ent);

            var lizardCoords = Transform.GetMoverCoordinates(uid);
            var targetCoords = Transform.GetMoverCoordinates(target.Value);
            if (!lizardCoords.TryDistance(EntityManager, targetCoords, out var distance))
                continue;

            if (CanRavageTarget(target.Value))
            {
                TryStartRavage((uid, lizard), target.Value, true);
                continue;
            }

            if (distance < lizard.MinPounceRange || distance > lizard.MaxPounceRange)
                continue;

            TryPounce((uid, lizard), targetCoords);
        }
    }
}
