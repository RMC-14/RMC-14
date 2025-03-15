using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.IconLabel;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Labels.EntitySystems;

public abstract class SharedHandLabelerSystem : EntitySystem
{
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;
    [Dependency] protected readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedLabelSystem _labelSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    protected ISawmill _logLoader = default!;

    public static event Action<EntityUid, string>? OnUpdateBottleLabel;

    public override void Initialize()
    {
        base.Initialize();

        _logLoader = Logger.GetSawmill("loader");

        SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        // Bound UI subscriptions
        SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
        SubscribeLocalEvent<HandLabelerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<HandLabelerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<HandLabelerComponent, ChangeExistingPillBottleColorMessage>(OnChangeExistingPillBottleColorMessage1);
    }

    private void OnGetState(Entity<HandLabelerComponent> ent, ref ComponentGetState args)
    {
        args.State = new HandLabelerComponentState(ent.Comp.AssignedLabel)
        {
            Bottle = ent.Comp.Bottle,
            MaxLabelChars = ent.Comp.MaxLabelChars,
        };
    }

    private void OnHandleState(Entity<HandLabelerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not HandLabelerComponentState state)
            return;

        ent.Comp.Bottle = state.Bottle;
        ent.Comp.MaxLabelChars = state.MaxLabelChars;

        if (ent.Comp.AssignedLabel == state.AssignedLabel)
            return;

        ent.Comp.AssignedLabel = state.AssignedLabel;
        UpdateUI(ent);
    }

    protected virtual void UpdateUI(Entity<HandLabelerComponent> ent)
    {
    }

    private void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            result = null;
            return;
        }

        string newLabel = handLabeler.AssignedLabel;

        if (_netManager.IsServer)
        {
            _labelSystem.Label(target,
                newLabel == string.Empty
                    ? null
                    : handLabeler.AssignedLabel);
            if (TryComp(target, out IconLabelComponent? iconLabelComponent))
            {
                UpdateIconLabel(
                    target,
                    iconLabelComponent,
                    newLabel
                );
            }


        }
        else if (IsPillBottle(target))
        {
            OnUpdateBottleLabel?.Invoke(target, newLabel);
        }

        if (handLabeler.AssignedLabel == string.Empty)
        {
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }

        result = Loc.GetString("hand-labeler-successfully-applied");
    }

    private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Target is not { Valid: true } target || _whitelistSystem.IsWhitelistFail(handLabeler.Whitelist, target) || !args.CanAccess)
            return;

        // Verb action for adding or removing a label
        var labelVerb = new UtilityVerb()
        {
            Act = () =>
            {
                Labeling(uid, target, args.User, handLabeler);
            },
            Text = Loc.GetString(
                handLabeler.AssignedLabel == string.Empty
                ? Loc.GetString("hand-labeler-remove-label-text")
                : Loc.GetString("hand-labeler-add-label-text"))
        };

        args.Verbs.Add(labelVerb);


        // Verb action for changing pill bottle colors
        if (IsPillBottle(target))
        {
            var bottleVerb = new UtilityVerb()
            {
                Act = () =>
                {
                    ChangingBottleColor(uid, target, args.User, handLabeler);
                },
                Text = Loc.GetString("hand-labeler-edit-pill-bottle-text")
            };

            args.Verbs.Add(bottleVerb);
        }
    }

    private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target || _whitelistSystem.IsWhitelistFail(handLabeler.Whitelist, target) || !args.CanReach)
            return;

        Labeling(uid, target, args.User, handLabeler);
    }

    // Applies the current label to the entity
    private void Labeling(EntityUid uid, EntityUid target, EntityUid User, HandLabelerComponent handLabeler)
    {
        AddLabelTo(uid, handLabeler, target, out var result);
        if (result == null)
            return;

        _popupSystem.PopupClient(result, User, User);

        // Log labeling TODO: entity log pill bottle changes
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(User):user} labeled {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
    }

    // Opens a UI to change a pill bottle's color
    private void ChangingBottleColor(EntityUid uid, EntityUid target, EntityUid User, HandLabelerComponent handLabeler)
    {
        handLabeler.Bottle = GetNetEntity(target);
        UserInterfaceSystem.OpenUi(uid, ChangeExistingPillBottleUIKey.Key, User);
    }

    private bool IsPillBottle(EntityUid uid)
    {
        if (TryComp(uid, out TagComponent? tagComponent))
        {
            var tags = tagComponent.Tags;
            if (tags.Contains(Loc.GetString("rmc-bottle-tag")))
            {
                return true;
            }
        }

        return false;
    }

    // Changes a pill bottle's color based on a message received
    private void OnChangeExistingPillBottleColorMessage1(Entity<HandLabelerComponent> handLabeler, ref ChangeExistingPillBottleColorMessage message)
    {
        if (message.NewColor > PillbottleColor.Black || handLabeler.Comp.Bottle == NetEntity.Invalid)
            return;

        // If the hand labeler has a bottle it's editing, change it's colour
        NetEntity netBottle = handLabeler.Comp.Bottle;

        var bottle = GetEntity(netBottle);

        if (TryComp(bottle, out ItemComponent? itemComponent))
        {
            _appearance.SetData(bottle, PillBottleVisuals.Color, message.NewColor);
            handLabeler.Comp.Bottle = NetEntity.Invalid;
        }

        ClickSound(handLabeler);

        if (_netManager.IsClient)
        {
        }
    }

    protected virtual void ClickSound(Entity<HandLabelerComponent> handLabeler)
    {

    }

    protected virtual void UpdateIconLabel(
        EntityUid owner,
        IconLabelComponent iconLabelComponent,
        string customLabel
    ) {

    }

    private void OnHandLabelerLabelChanged(EntityUid uid, HandLabelerComponent handLabeler, HandLabelerLabelChangedMessage args)
    {
        var label = args.Label.Trim();
        handLabeler.AssignedLabel = label[..Math.Min(handLabeler.MaxLabelChars, label.Length)];
        UpdateUI((uid, handLabeler));
        Dirty(uid, handLabeler);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");
    }
}

[Serializable, NetSerializable]
public enum ChangeExistingPillBottleUIKey
{
    Key
};
