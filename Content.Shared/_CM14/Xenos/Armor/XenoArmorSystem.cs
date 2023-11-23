using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._CM14.Xenos.Armor;

public sealed class XenoArmorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, DamageModifyEvent>(OnXenoDamageModify);
        SubscribeLocalEvent<XenoArmorComponent, XenoGetArmorEvent>(OnXenoGetArmor);
    }

    private void OnXenoDamageModify(Entity<XenoComponent> xeno, ref DamageModifyEvent args)
    {
        var ev = new XenoGetArmorEvent();
        RaiseLocalEvent(xeno, ref ev);

        var armor = ev.Armor;
        if (args.Origin is { } origin)
        {
            var originCoords = _transform.GetMapCoordinates(origin);
            var xenoCoords = _transform.GetMapCoordinates(xeno);

            if (originCoords.MapId == xenoCoords.MapId)
            {
                var diff = (originCoords.Position - xenoCoords.Position).ToWorldAngle().GetCardinalDir();
                if (diff == _transform.GetWorldRotation(xeno).GetCardinalDir())
                {
                    armor += ev.FrontalArmor;
                }
            }
        }

        var resist = Math.Pow(1.1, armor / 5.0);
        args.Damage /= resist;

        var newDamage = args.Damage.GetTotal();
        if (newDamage < armor * 2)
        {
            var damageWithArmor = FixedPoint2.Max(0, newDamage * 4 - armor);
            args.Damage *= damageWithArmor / (newDamage * 4);
        }
    }

    private void OnXenoGetArmor(Entity<XenoArmorComponent> xeno, ref XenoGetArmorEvent args)
    {
        args.Armor += xeno.Comp.Armor;
    }
}
