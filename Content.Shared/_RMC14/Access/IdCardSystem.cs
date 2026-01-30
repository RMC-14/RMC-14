using Content.Shared._RMC14.Marines;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Database;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Access;

public sealed class IdCardSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IdCardComponent, UseInHandEvent>(OnUseInHand, before: [typeof(ClothingSystem)] );
        SubscribeLocalEvent<IdCardComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MarineComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<MarineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp(args.Used, out IdCardComponent? idCard) || idCard.OriginalOwner != null)
            return;
        idCard.OriginalOwner = args.Target;
        var popupMessage = $"{Name(args.User)} bound an ID to {Name(args.Target)}.";
        _popup.PopupPredicted(popupMessage, args.Target, args.User, PopupType.Small);
        _adminLogger.Add(LogType.RMCIdModify,
            LogImpact.High,
            $"{ToPrettyString(args.User):player} has bound the ID {ToPrettyString(args.Used):entity} to {ToPrettyString(args.Target):player}");
        args.Handled = true;
        Dirty(ent);
    }

    private void OnExamine(Entity<IdCardComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.OriginalOwner == null)
        {
            args.PushMarkup("[color=orange]To claim ownership, interact with the card or another person to bind it to them.[/color]");
        }
    }

    private void OnUseInHand(Entity<IdCardComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.OriginalOwner != null || args.Handled)
            return;

        ent.Comp.OriginalOwner = args.User;
        args.Handled = true;
        var popupMessage = $"Bound ID to yourself.";
        _popup.PopupClient(popupMessage, args.User, PopupType.Small);
        _adminLogger.Add(LogType.RMCIdModify,
            LogImpact.Medium,
            $"{ToPrettyString(args.User):player} has bound the ID {ToPrettyString(ent):entity} to themselves.");
        Dirty(ent);
    }
}
