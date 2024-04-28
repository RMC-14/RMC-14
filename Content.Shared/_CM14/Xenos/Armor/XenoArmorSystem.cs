using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Armor;

public sealed class XenoArmorSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [ValidatePrototypeId<DamageGroupPrototype>]
    private const string DamageGroup = "Brute";

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoArmorComponent, MapInitEvent>(OnXenoMapInit);
        SubscribeLocalEvent<XenoArmorComponent, ComponentRemove>(OnXenoRemove);
        SubscribeLocalEvent<XenoComponent, DamageModifyEvent>(OnXenoDamageModify);
        SubscribeLocalEvent<XenoArmorComponent, XenoGetArmorEvent>(OnXenoGetArmor);
        SubscribeLocalEvent<XenoArmorComponent, GetExplosionResistanceEvent>(OnXenoGetExplosionResistance);
        SubscribeLocalEvent<CMArmorPiercingComponent, XenoGetArmorEvent>(OnPiercingXenoGetArmor);
    }

    private void OnXenoMapInit(Entity<XenoArmorComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, AlertType.XenoArmor, 0);
    }

    private void OnXenoRemove(Entity<XenoArmorComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlertCategory(ent, AlertCategory.XenoArmor);
    }

    private void OnXenoDamageModify(Entity<XenoComponent> xeno, ref DamageModifyEvent args)
    {
        var ev = new XenoGetArmorEvent();
        RaiseLocalEvent(xeno, ref ev);

        if (args.Tool != null)
            RaiseLocalEvent(args.Tool.Value, ref ev);

        var armor = Math.Max(ev.Armor, 0);
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

        if (armor <= 0)
            return;

        var resist = Math.Pow(1.1, armor / 5.0);
        var types = _prototypes.Index<DamageGroupPrototype>(DamageGroup).DamageTypes;

        foreach (var type in types)
        {
            if (args.Damage.DamageDict.TryGetValue(type, out var amount) &&
                amount > FixedPoint2.Zero)
            {
                args.Damage.DamageDict[type] = amount / resist;
            }
        }

        var newDamage = args.Damage.GetTotal();
        if (newDamage != FixedPoint2.Zero && newDamage < armor * 2)
        {
            var damageWithArmor = FixedPoint2.Max(0, newDamage * 4 - armor);

            foreach (var type in types)
            {
                if (args.Damage.DamageDict.TryGetValue(type, out var amount) &&
                    amount > FixedPoint2.Zero)
                {
                    args.Damage.DamageDict[type] = amount * damageWithArmor / (newDamage * 4);
                }
            }
        }
    }

    private void OnXenoGetArmor(Entity<XenoArmorComponent> xeno, ref XenoGetArmorEvent args)
    {
        args.Armor += xeno.Comp.Armor;
    }

    private void OnXenoGetExplosionResistance(Entity<XenoArmorComponent> ent, ref GetExplosionResistanceEvent args)
    {
        // TODO CM14 unhalve this when we can calculate explosion damage better
        var armor = ent.Comp.ExplosionArmor / 2;

        if (armor <= 0)
            return;

        var resist = (float) Math.Pow(1.1, armor / 5.0);
        args.DamageCoefficient /= resist;
    }

    private void OnPiercingXenoGetArmor(Entity<CMArmorPiercingComponent> ent, ref XenoGetArmorEvent args)
    {
        args.Armor -= ent.Comp.Amount;
    }
}
