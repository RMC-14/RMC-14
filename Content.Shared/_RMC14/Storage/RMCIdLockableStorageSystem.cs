using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Storage;

/// <summary>
/// Handles ID swipe lock and access checks for reusable RMC lockable storage.
/// </summary>
public sealed class RMCIdLockableStorageSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCIdLockableStorageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCIdLockableStorageComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<RMCIdLockableStorageComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);
        SubscribeLocalEvent<RMCIdLockableStorageComponent, DumpableDoAfterEvent>(OnDumpableDoAfter, before: [typeof(DumpableSystem)]);
        SubscribeLocalEvent<RMCIdLockableStorageComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(Entity<RMCIdLockableStorageComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnInteractUsing(Entity<RMCIdLockableStorageComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_idCard.TryGetIdCard(args.Used, out var idCard))
            return;

        args.Handled = true;

        if (!ent.Comp.Locked)
        {
            // The first valid swipe binds the storage to the printed name on that ID.
            if (!TryGetOwnerName(idCard, out var ownerName))
            {
                _popup.PopupPredicted(Loc.GetString("rmc-id-lockable-storage-id-invalid"), ent.Owner, args.User, PopupType.SmallCaution);
                return;
            }

            ent.Comp.Locked = true;
            ent.Comp.OwnerName = ownerName;
            Dirty(ent);
            UpdateAppearance(ent);
            CloseStorageUi(ent);
            _popup.PopupPredicted(Loc.GetString("rmc-id-lockable-storage-lock", ("storage", ent)), ent.Owner, args.User, PopupType.Small);
            return;
        }

        // Locked storage can only be opened by the matching ID name or a prototype-defined override access.
        if (IsAuthorized(ent, idCard) || HasAccessOverride(ent, args.Used))
        {
            ent.Comp.Locked = false;
            Dirty(ent);
            UpdateAppearance(ent);
            _popup.PopupPredicted(Loc.GetString("rmc-id-lockable-storage-unlock", ("storage", ent)), ent.Owner, args.User, PopupType.Small);
            return;
        }

        _popup.PopupPredicted(Loc.GetString("rmc-id-lockable-storage-access-denied"), ent.Owner, args.User, PopupType.SmallCaution);
    }

    private void OnStorageInteractAttempt(Entity<RMCIdLockableStorageComponent> ent, ref StorageInteractAttemptEvent args)
    {
        // Opening the storage itself never uses override access. Override only applies to the ID swipe unlock action.
        if (args.Cancelled || !ent.Comp.Locked || HasComp<BypassInteractionChecksComponent>(args.User) || IsAuthorized(ent, args.User) || HasContainedOwnerId(ent))
            return;

        args.Cancelled = true;

        _popup.PopupPredicted(
            Loc.GetString("rmc-id-lockable-storage-open-denied",
                ("storage", ent),
                ("owner", ent.Comp.OwnerName ?? Loc.GetString("rmc-id-lockable-storage-owner-unknown"))),
            ent.Owner,
            args.User,
            PopupType.SmallCaution);
    }

    private void OnDumpableDoAfter(Entity<RMCIdLockableStorageComponent> ent, ref DumpableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !ent.Comp.Locked || HasComp<BypassInteractionChecksComponent>(args.User) || IsAuthorized(ent, args.User) || HasContainedOwnerId(ent))
            return;

        args.Handled = true;
        _popup.PopupClient(
            Loc.GetString("rmc-id-lockable-storage-open-denied",
                ("storage", ent),
                ("owner", ent.Comp.OwnerName ?? Loc.GetString("rmc-id-lockable-storage-owner-unknown"))),
            ent,
            args.User,
            PopupType.SmallCaution);
    }

    private void OnExamined(Entity<RMCIdLockableStorageComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCIdLockableStorageComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-id-lockable-storage-examine"));

            if (ent.Comp.OverrideAccesses.Count > 0)
                args.PushMarkup(Loc.GetString("rmc-id-lockable-storage-examine-override"));

            if (ent.Comp.Locked)
            {
                args.PushMarkup(Loc.GetString("rmc-id-lockable-storage-examine-locked",
                    ("owner", ent.Comp.OwnerName ?? Loc.GetString("rmc-id-lockable-storage-owner-unknown"))));
            }
            else
            {
                args.PushMarkup(Loc.GetString("rmc-id-lockable-storage-examine-unlocked"));
            }
        }
    }

    private bool IsAuthorized(Entity<RMCIdLockableStorageComponent> ent, EntityUid user)
    {
        if (!_idCard.TryFindIdCard(user, out var idCard))
            return false;

        return IsAuthorized(ent, idCard);
    }

    private bool IsAuthorized(Entity<RMCIdLockableStorageComponent> ent, Entity<IdCardComponent> idCard)
    {
        return TryGetOwnerName(idCard, out var ownerName) &&
               ownerName == ent.Comp.OwnerName;
    }

    private bool HasAccessOverride(Entity<RMCIdLockableStorageComponent> ent, EntityUid swiped)
    {
        if (ent.Comp.OverrideAccesses.Count == 0)
            return false;

        // Check access on the item used for the swipe so cards, dogtags, and similar ID items work the same way.
        var tags = _accessReader.FindAccessTags(swiped);
        foreach (var access in ent.Comp.OverrideAccesses)
        {
            if (tags.Contains(access))
                return true;
        }

        return false;
    }

    private bool TryGetOwnerName(Entity<IdCardComponent> idCard, out string? ownerName)
    {
        ownerName = NormalizeName(idCard.Comp.FullName);
        return ownerName != null;
    }

    private bool HasContainedOwnerId(Entity<RMCIdLockableStorageComponent> ent)
    {
        if (ent.Comp.OwnerName == null || !TryComp(ent, out StorageComponent? storage))
            return false;

        foreach (var contained in storage.Container.ContainedEntities)
        {
            if (!_idCard.TryGetIdCard(contained, out var idCard))
                continue;

            if (TryGetOwnerName(idCard, out var ownerName) && ownerName == ent.Comp.OwnerName)
                return true;
        }

        return false;
    }

    private static string? NormalizeName(string? value)
    {
        // Match stored ownership on the exact printed name after trimming harmless whitespace.
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private void CloseStorageUi(Entity<RMCIdLockableStorageComponent> ent)
    {
        foreach (var actor in _ui.GetActors(ent.Owner, StorageComponent.StorageUiKey.Key).ToList())
        {
            _ui.CloseUi(ent.Owner, StorageComponent.StorageUiKey.Key, actor);
        }
    }

    private void UpdateAppearance(Entity<RMCIdLockableStorageComponent> ent)
    {
        _appearance.SetData(ent.Owner, LockVisuals.Locked, ent.Comp.Locked);
    }
}
