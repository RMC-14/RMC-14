using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._CM14.Xenos.Armor;

public sealed class XenoArmorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, DamageModifyEvent>(OnXenoDamageModify);
        SubscribeLocalEvent<XenoArmorComponent, XenoGetArmorEvent>(OnXenoGetArmor);
    }

    private void OnXenoDamageModify(Entity<XenoComponent> ent, ref DamageModifyEvent args)
    {
        var ev = new XenoGetArmorEvent();
        RaiseLocalEvent(ent, ref ev);

        var resist = Math.Pow(1.1, ev.Armor / 5.0);
        args.Damage /= resist;

        var newDamage = args.Damage.GetTotal();
        if (newDamage < ev.Armor * 2)
        {
            var damageWithArmor = FixedPoint2.Max(0, newDamage * 4 - ev.Armor);
            args.Damage *= damageWithArmor / (newDamage * 4);
        }
    }

    private void OnXenoGetArmor(Entity<XenoArmorComponent> ent, ref XenoGetArmorEvent args)
    {
        args.Armor += ent.Comp.Armor;
    }
}
