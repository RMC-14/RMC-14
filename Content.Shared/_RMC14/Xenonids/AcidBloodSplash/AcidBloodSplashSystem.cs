using Content.Shared.Damage;

namespace Content.Shared._RMC14.Xenonids.AcidBloodSplash;

public sealed class AcidBloodSplashSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AcidBloodSplashComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, AcidBloodSplashComponent comp, ref DamageChangedEvent args)
    {
        var damageDict = args.Damageable.Damage.DamageDict;
    }
}
