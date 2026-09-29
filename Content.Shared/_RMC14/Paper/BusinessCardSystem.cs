using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared._RMC14.Paper;

public sealed class BusinessCardSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BusinessCardComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BusinessCardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BusinessCardComponent, PaperInputTextMessage>(OnInputText);
    }

    private void OnInit(Entity<BusinessCardComponent> ent, ref ComponentInit args)
    {
        UpdateEditingState(ent);
    }

    private void OnMapInit(Entity<BusinessCardComponent> ent, ref MapInitEvent args)
    {
        // If the card already has content, mark it as written
        if (TryComp<PaperComponent>(ent, out var paper) && !string.IsNullOrWhiteSpace(paper.Content))
        {
            ent.Comp.HasBeenWritten = true;
            Dirty(ent);
            UpdateEditingState(ent);
        }
    }

    private void OnInputText(Entity<BusinessCardComponent> ent, ref PaperInputTextMessage args)
    {
        // After text is input, mark as written and lock
        if (!string.IsNullOrWhiteSpace(args.Text))
        {
            ent.Comp.HasBeenWritten = true;
            Dirty(ent);
            UpdateEditingState(ent);
        }
    }

    private void UpdateEditingState(Entity<BusinessCardComponent> ent)
    {
        if (!TryComp<PaperComponent>(ent, out var paper))
            return;

        if (ent.Comp.HasBeenWritten)
            paper.EditingDisabled = true;
    }
}
