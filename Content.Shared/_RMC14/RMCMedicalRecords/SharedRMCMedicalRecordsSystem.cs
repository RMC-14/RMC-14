using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.HUD.Events;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.RMCMedicalRecords;

public abstract class SharedRMCMedicalRecordsSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skills = default!;

    private const int MinimumSkillLvl = 2;
    private static readonly EntProtoId<SkillDefinitionComponent> MedicalSkill = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCLastBodyScanResultComponent, GetVerbsEvent<ExamineVerb>>(OnMedicalRecordExamineVerb);
    }

    private void OnMedicalRecordExamineVerb(Entity<RMCLastBodyScanResultComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        if (!_skills.HasSkill(args.User, MedicalSkill, MinimumSkillLvl))
            return;

        var scanEvent = new HolocardScanEvent(false, SlotFlags.EYES | SlotFlags.HEAD);
        RaiseLocalEvent(args.User, ref scanEvent);
        if (!scanEvent.CanScan)
            return;

        var hasScan = ent.Comp.LastScanTime is not null && ent.Comp.LastScanState is not null;
        var verbMessage = hasScan
            ? Loc.GetString("rmc-records-examine-scan-time", ("time", ent.Comp.LastScanTime!))
            : Loc.GetString("rmc-records-examine-no-scan");

        var target = ent.Owner;
        var verb = new ExamineVerb
        {
            Act = () =>
            {
                if (hasScan)
                    RaiseLocalEvent(new OpenStoredScanEvent(GetNetEntity(target)));
            },
            Text = Loc.GetString("rmc-records-examine-verb-text"),
            Message = verbMessage,
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/_RMC14/Objects/Misc/paper.rsi/folder_blue.png")),
            Disabled = !hasScan,
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Attempts to retrieve the entity-bound medical record component.
    /// </summary>
    public bool TryGetMedicalRecord(EntityUid uid, out RMCLastBodyScanResultComponent record)
    {
        return TryComp(uid, out record!);
    }
}
