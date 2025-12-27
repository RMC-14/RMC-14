using System.Linq;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.IconLabel;

public abstract class SharedRMCIconLabelSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> PillCanisterTag = "PillCanister";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IconLabelComponent, GetVerbsEvent<InteractionVerb>>(OnSetIconLabelGetVerbs);
    }

    private void OnSetIconLabelGetVerbs(Entity<IconLabelComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;
        if (HasComp<XenoComponent>(args.User))
            return;
        if (!_tag.HasTag(ent, PillCanisterTag))
            return;

        var user = args.User;
        var target = args.Target;
        var maxLength = ent.Comp.LabelMaxSize;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("rmc-set-icon-label-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_RMC14/Interface/eraser.svg.png")),
            Priority = -1,
            Act = () => TrySetIconLabel(user, target, maxLength),
        });
    }

    public void SetIconLabel(EntityUid user, EntityUid target, string label)
    {
        if (!TryComp<IconLabelComponent>(target, out var iconLabelComp))
            return;

        var maxLength = iconLabelComp.LabelMaxSize;
        label = label.Trim();
        if (label.Length > maxLength)
            label = label[..maxLength];

        if (string.IsNullOrWhiteSpace(label))
        {
            iconLabelComp.LabelTextLocId = null;
            iconLabelComp.LabelTextParams.Clear();
            Dirty(target, iconLabelComp);

            _popup.PopupEntity(Loc.GetString("rmc-set-icon-label-cleared", ("item", target)), target, user);
            _adminLog.Add(LogType.RMCIconLabel, $"{ToPrettyString(user):user} cleared icon label on {ToPrettyString(target):target}");
            return;
        }

        Label((target, iconLabelComp), "rmc-custom-container-label-text", ("customLabel", label));
        _popup.PopupEntity(Loc.GetString("rmc-set-icon-label-set", ("item", target), ("label", label)), target, user);
        _adminLog.Add(LogType.RMCIconLabel, $"{ToPrettyString(user):user} set icon label on {ToPrettyString(target):target} to '{label}'");
    }

    public void Label(Entity<IconLabelComponent?> ent, LocId newLocId, List<(string, object)> newParams)
    {
        ent.Comp = EnsureComp<IconLabelComponent>(ent);
        ent.Comp.LabelTextLocId = newLocId;
        ent.Comp.LabelTextParams = new List<(string, object)>(newParams);
        Dirty(ent);
    }

    public void Label(Entity<IconLabelComponent?> ent, LocId newLocId, params (string, object)[] newParams)
    {
        Label(ent, newLocId, newParams.ToList());
    }

    protected virtual void TrySetIconLabel(EntityUid user, EntityUid target, int maxLength)
    {
    }
}
