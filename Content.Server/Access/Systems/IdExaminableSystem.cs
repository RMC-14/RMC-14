using Content.Server.Access.Components;
using Content.Shared.Access.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Access.Systems;

public sealed class IdExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, IdExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);
        var info = GetInfo(uid) ?? Loc.GetString("id-examinable-component-verb-no-id");

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = FormattedMessage.FromMarkup(info);
                _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("id-examinable-component-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("id-examinable-component-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/character.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    private string? GetPdaOrIdInfo(EntityUid uid)
    {
        // PDA
        if (EntityManager.TryGetComponent(uid, out PdaComponent? pda) &&
            TryComp<IdCardComponent>(pda.ContainedId, out var id))
        {
            return GetNameAndJob(id);
        }

        // ID Card
        if (EntityManager.TryGetComponent(uid, out id))
        {
            return GetNameAndJob(id);
        }

        return null;
    }

    private string? GetInfo(EntityUid uid)
    {
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) &&
            GetPdaOrIdInfo(idUid.Value) is { } idInfo)
            return idInfo;

        if (_inventorySystem.TryGetSlotEntity(uid, "neck", out var neckUid) &&
            GetPdaOrIdInfo(neckUid.Value) is { } neckInfo)
            return neckInfo;

        return null;
    }

    private string GetNameAndJob(IdCardComponent id)
    {
        var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

        var val = string.IsNullOrWhiteSpace(id.FullName)
            ? Loc.GetString(id.NameLocId,
                ("jobSuffix", jobSuffix))
            : Loc.GetString(id.FullNameLocId,
                ("fullName", id.FullName),
                ("jobSuffix", jobSuffix));

        return val;
    }
}
