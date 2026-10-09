using Content.Shared._RMC14.PetNaming;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using System.Text.RegularExpressions;

namespace Content.Server._RMC14.PetNamingSystem;

public sealed class PetNamingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PetNamingItemComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PetNamingItemComponent, PetNamingSetNameBuiMsg>(OnSetName);
        SubscribeLocalEvent<PetNamingItemComponent, BoundUIClosedEvent>(OnClosed);
    }

    private static readonly Regex NumberSuffix = new(@"\s*\([^)]*\)\s*$", RegexOptions.Compiled);

    private static string DefaultNameFieldValue(string name)
    {
        var stripped = NumberSuffix.Replace(name, "").Trim();
        return stripped.Length > 0 ? stripped : name;
    }

    private void OnAfterInteract(Entity<PetNamingItemComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!HasComp<PetNamingTargetComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("rmc-name-pet-invalid-target"), target, args.User, PopupType.SmallCaution);
            return;
        }

        ent.Comp.Target = target;

        if (!_ui.TryOpenUi(ent.Owner, PetNamingUiKey.Key, args.User))
            return;

        _ui.SetUiState(ent.Owner, PetNamingUiKey.Key,
            new PetNamingBuiState(DefaultNameFieldValue(Name(target)), ent.Comp.MaxLength));
    }

    private void OnSetName(Entity<PetNamingItemComponent> ent, ref PetNamingSetNameBuiMsg args)
    {
        var user = args.Actor;

        if (ent.Comp.Target is not { } target ||
            TerminatingOrDeleted(target) ||
            !HasComp<PetNamingTargetComponent>(target))
        {
            _ui.CloseUi(ent.Owner, PetNamingUiKey.Key, user);
            return;
        }

        if (!_interaction.InRangeUnobstructed(user, target, ent.Comp.Range))
        {
            _popup.PopupEntity(Loc.GetString("rmc-name-pet-too-far"), user, user, PopupType.SmallCaution);
            return;
        }

        var name = args.Name.Trim();
        if (name.Length > ent.Comp.MaxLength)
        {
            _popup.PopupEntity(Loc.GetString("rmc-name-pet-too-long"), user, user, PopupType.SmallCaution);
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
            return;

        _meta.SetEntityName(target, name);
        _popup.PopupEntity(Loc.GetString("rmc-name-pet-success", ("name", name)), target, user);

        _adminLog.Add(LogType.RMCPetNamed, LogImpact.Low, $"{ToPrettyString(user):user} named his pet '{name}'");

        if (ent.Comp.ConsumeOnUse)
            QueueDel(ent);
        else
            _ui.CloseUi(ent.Owner, PetNamingUiKey.Key, user);
    }

    private void OnClosed(Entity<PetNamingItemComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is PetNamingUiKey)
            ent.Comp.Target = null;
    }
}