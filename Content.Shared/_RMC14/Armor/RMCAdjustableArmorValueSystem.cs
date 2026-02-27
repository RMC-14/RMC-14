using System.Text;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Examine;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Armor;

public sealed partial class RMCAdjustableArmorValueSystem : EntitySystem
{
    [Dependency] private readonly ActivatableUISystem _activatableUI = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCAdjustableArmorValueComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<RMCAdjustableArmorValueComponent, CMGetArmorEvent>(OnGetArmor);
        SubscribeLocalEvent<RMCAdjustableArmorValueComponent, AdjustableArmorSetValueMessage>(OnAdjustableArmorSetValue);
        SubscribeLocalEvent<RMCAdjustableArmorValueComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
        SubscribeLocalEvent<RMCAdjustableArmorValueComponent, ExaminedEvent>(OnExamine);
    }

    private void OnAltVerb(EntityUid uid, RMCAdjustableArmorValueComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () => _activatableUI.InteractUI(args.User, uid),
            Text = Loc.GetString("rmc-adjustable-armor-set"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        };
        args.Verbs.Add(verb);
    }

    private void OnAdjustableArmorSetValue(EntityUid uid, RMCAdjustableArmorValueComponent component, AdjustableArmorSetValueMessage args)
    {
        if (!int.TryParse(args.Value.Trim(), out var value))
            return;

        value = Math.Clamp(value, 0, component.MaxArmor);

        switch (args.Type)
        {
            case ArmorType.Melee:
                component.MeleeArmor = value;
                break;
            case ArmorType.Bullet:
                component.BulletArmor = value;
                break;
            case ArmorType.Explosion:
                component.ExplosionArmor = value;
                break;
            case ArmorType.Bio:
                component.BioArmor = value;
                break;
        }

        EnsureComp<CMArmorComponent>(uid);
        Dirty(uid, component);
    }

    private void OnGetArmor(Entity<RMCAdjustableArmorValueComponent> ent, ref CMGetArmorEvent args)
    {
        args.Melee += ent.Comp.MeleeArmor;
        args.Bullet += ent.Comp.BulletArmor;
        args.ExplosionArmor += ent.Comp.ExplosionArmor;
        args.Bio += ent.Comp.BioArmor;
    }

    private void AfterUIOpen(EntityUid uid, RMCAdjustableArmorValueComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!_uiSystem.HasUi(uid, AdjustableArmorUiKey.Key))
            return;

        var state = new AdjustableArmorBoundUserInterfaceState(component.MeleeArmor.ToString(),
            component.BulletArmor.ToString(),
            component.ExplosionArmor.ToString(),
            component.BioArmor.ToString());
            _uiSystem.SetUiState(uid, AdjustableArmorUiKey.Key, state);
    }

    private void OnExamine(Entity<RMCAdjustableArmorValueComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CMArmorSystem), -10))
        {
            var armorRatings = new[]
            {
                ("rmc-adjustable-armor-examine-bullet", ent.Comp.BulletArmor),
                ("rmc-adjustable-armor-examine-melee", ent.Comp.MeleeArmor),
                ("rmc-adjustable-armor-examine-explosion", ent.Comp.ExplosionArmor),
                ("rmc-adjustable-armor-examine-bio", ent.Comp.BioArmor),
            };

            var examine = new StringBuilder();
            foreach (var (locId, value) in armorRatings)
            {
                if (value == 0)
                    continue;

                examine.AppendLine(Loc.GetString(locId, ("armor", value)));
            }

            if (examine.Length == 0)
                return;

            examine.Insert(0, $"{Loc.GetString("rmc-adjustable-armor-examine")}\n", 1);
            args.AddMarkup(examine.ToString());
        }
    }
}

[Serializable, NetSerializable]
public enum AdjustableArmorUiKey : byte
{
    Key,
}


[Serializable, NetSerializable]
public sealed class AdjustableArmorBoundUserInterfaceState : BoundUserInterfaceState
{
    public string MeleeArmor { get; }
    public string BulletArmor { get; }
    public string ExplosionArmor { get; }
    public string BioArmor { get; }

    public AdjustableArmorBoundUserInterfaceState(
        string melee,
        string bullet,
        string explosion,
        string bio)
    {
        MeleeArmor = melee;
        BulletArmor = bullet;
        ExplosionArmor = explosion;
        BioArmor = bio;
    }
}

[Serializable, NetSerializable]
public sealed class AdjustableArmorSetValueMessage : BoundUserInterfaceMessage
{
    public ArmorType Type { get; }
    public string Value { get; }

    public AdjustableArmorSetValueMessage(ArmorType type, string value)
    {
        Type = type;
        Value = value;
    }
}

[Serializable, NetSerializable]
public enum ArmorType : byte
{
    Bio,
    Bullet,
    Explosion,
    Melee,
}
