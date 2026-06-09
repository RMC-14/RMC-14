using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Doors.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ClawSharpness;

public sealed class XenoClawsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private EntityQuery<MeleeWeaponComponent> _meleeWeaponQuery;
    private EntityQuery<XenoClawsComponent> _xenoClawsQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private readonly ProtoId<DamageGroupPrototype> _clawsDamageGroup = "Brute";

    public override void Initialize()
    {
        _meleeWeaponQuery = GetEntityQuery<MeleeWeaponComponent>();
        _xenoClawsQuery = GetEntityQuery<XenoClawsComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<ReceiverXenoClawsComponent, DamageModifyEvent>(OnReceiverDamageModify);
        SubscribeLocalEvent<AirlockReceiverXenoClawsComponent, DamageModifyEvent>(OnAirlockReceiverDamageModify);
    }

    /*
    REFERENCES:

    // Determines how xenos interact with walls, normal nothing, sharp can destroy normal walls and window frame, very sharp reinforced ones.
    #define CLAW_TYPE_NORMAL 1
    #define CLAW_TYPE_SHARP 2
    #define CLAW_TYPE_VERY_SHARP 3

    #define XENO_HITS_TO_DESTROY_WALL 20
    #define XENO_HITS_TO_DESTROY_WINDOW_FRAME 3
    #define XENO_HITS_TO_DESTROY_R_WINDOW_FRAME 5
    #define XENO_HITS_TO_DESTROY_BOLTED_DOOR 10
    #define XENO_HITS_TO_DESTROY_WELDED_DOOR 15
    #define XENO_HITS_TO_EXPOSE_WIRES_MIN 3
    #define XENO_HITS_TO_EXPOSE_WIRES_MAX 4
    #define XENO_HITS_TO_CUT_WIRES 10

    FROM CM-SS13
    */

    private void OnReceiverDamageModify(Entity<ReceiverXenoClawsComponent> ent, ref DamageModifyEvent args)
    {
        var xeno = args.Tool;
        var receiver = ent.Comp;
        if (!_meleeWeaponQuery.HasComp(xeno) || !_xenoClawsQuery.TryComp(xeno, out var claws))
            return;

        var hasRequiredClaws = claws.ClawType.CompareTo(receiver.MinimumClawStrength) >= 0;
        bool hasRequiredTier = false;

        if (receiver.MinimumXenoTier != null)
        {
            hasRequiredTier = _xenoQuery.TryComp(xeno, out var xenoComp) &&
                              xenoComp.Tier >= receiver.MinimumXenoTier;
        }

        if (hasRequiredClaws || hasRequiredTier)
        {
            args.Damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup),
                                              receiver.MaxHealth / receiver.HitsToDestroy);
        }
        else
        {
            args.Damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup), 0);
        }
    }

    private void OnAirlockReceiverDamageModify(Entity<AirlockReceiverXenoClawsComponent> ent, ref DamageModifyEvent args)
    {
        var xeno = args.Tool;
        var receiver = ent.Comp;

        if (!_meleeWeaponQuery.HasComp(xeno))
            return;

        if (!TryComp<DoorComponent>(ent, out var door))
            return;

        if (!TryComp<DoorBoltComponent>(ent, out var bolt))
            return;

        var damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup), 0);

        if (_xenoClawsQuery.TryComp(xeno, out var claws))
        {
            if (claws.ClawType.CompareTo(receiver.MinimumClawStrength) >= 0)
            {
                if (bolt.BoltsDown)
                    damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup), receiver.MaxHealth / receiver.HitsToDestroyBolted);

                if (door.State == DoorState.Welded)
                    damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup), receiver.MaxHealth / receiver.HitsToDestroyWelded);
            }
        }

        args.Damage = damage;
    }
}
