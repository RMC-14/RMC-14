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
    private readonly ProtoId<DamageGroupPrototype> _clawsDamageGroup = "Brute";

    public override void Initialize()
    {
        _meleeWeaponQuery = GetEntityQuery<MeleeWeaponComponent>();

        SubscribeLocalEvent<ReceiverXenoClawsComponent, DamageModifyEvent>(OnReceiverDamageModify);
        SubscribeLocalEvent<AirlockReceiverXenoClawsComponent, DamageModifyEvent>(OnAirlockReceiverDamageModify);
    }

    private void OnReceiverDamageModify(Entity<ReceiverXenoClawsComponent> ent, ref DamageModifyEvent args)
    {
        var xeno = args.Tool;
        var receiver = ent.Comp;

        if (!_meleeWeaponQuery.HasComp(xeno))
            return;

        var damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup), 0);

        if (TryComp<XenoClawsComponent>(xeno, out var claws))
        {
            if (claws.ClawType.CompareTo(receiver.MinimumClawStrength) >= 0)
                damage = new DamageSpecifier(_protoManager.Index(_clawsDamageGroup), receiver.MaxHealth / receiver.HitsToDestroy);
        }

        args.Damage = damage;
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

        if (TryComp<XenoClawsComponent>(xeno, out var claws))
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
