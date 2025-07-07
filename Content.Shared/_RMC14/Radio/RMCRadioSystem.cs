using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Radio;

public sealed class RMCRadioSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EncryptionKeySystem _encryptionKey = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCHeadsetComponent, EncryptionChannelsChangedEvent>(OnHeadsetEncryptionChannelsChanged, before: [typeof(SharedHeadsetSystem)]);
        SubscribeLocalEvent<RMCRadioFilterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        SubscribeLocalEvent<HeadsetAutoSquadComponent, MapInitEvent>(OnHeadsetAutoSquadRefresh);
        SubscribeLocalEvent<HeadsetAutoSquadComponent, GotEquippedEvent>(OnHeadsetAutoSquadRefresh);
        SubscribeLocalEvent<HeadsetAutoSquadComponent, EncryptionChannelsChangedEvent>(OnHeadsetAutoSquadEncryptionChannelsChanged, before: [typeof(SharedHeadsetSystem)]);

        Subs.BuiEvents<RMCRadioFilterComponent>(RMCRadioFilterUI.Key,
            subs =>
            {
                subs.Event<RMCRadioFilterBuiMsg>(OnRadioFilterBuiMsg);
            });
    }

    private void OnHeadsetEncryptionChannelsChanged(Entity<RMCHeadsetComponent> ent, ref EncryptionChannelsChangedEvent args)
    {
        // prevent adding channels and therefore ActiveRadioComponent before map initialized
        if (LifeStage(ent) < EntityLifeStage.MapInitialized)
            return;

        foreach (var channel in ent.Comp.Channels)
        {
            args.Component.Channels.Add(channel);
        }
    }

    private void OnGetAltVerbs(Entity<RMCRadioFilterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Tune Radio",
            IconEntity = GetNetEntity(ent.Owner),
            Act = () =>
            {
                _ui.OpenUi(ent.Owner, RMCRadioFilterUI.Key, user);
            },
        });
    }

    private void OnHeadsetAutoSquadRefresh<T>(Entity<HeadsetAutoSquadComponent> ent, ref T args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (TryComp(ent.Owner, out EncryptionKeyHolderComponent? holder) && holder.KeyContainer != null)
            _encryptionKey.UpdateChannels(ent, holder);
    }

    private void OnHeadsetAutoSquadEncryptionChannelsChanged(Entity<HeadsetAutoSquadComponent> ent, ref EncryptionChannelsChangedEvent args)
    {
        if (!_container.TryGetContainingContainer((ent, null), out var container) ||
            !TryComp(container.Owner, out SquadMemberComponent? member) ||
            !TryComp(member.Squad, out SquadTeamComponent? team) ||
            team.Radio is not { } radio)
        {
            return;
        }

        args.Component.Channels.Add(radio);
    }

    private void OnRadioFilterBuiMsg(Entity<RMCRadioFilterComponent> ent, ref RMCRadioFilterBuiMsg args)
    {
        if (args.Toggle)
        {
            ent.Comp.DisabledChannels.Remove(args.Channel);
        }
        else
        {
            ent.Comp.DisabledChannels.Add(args.Channel);
        }

        Dirty(ent);
    }
}
