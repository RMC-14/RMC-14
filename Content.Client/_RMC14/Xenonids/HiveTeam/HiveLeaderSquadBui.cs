using Content.Shared._RMC14.Xenonids.HiveTeam;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.HiveTeam;

[UsedImplicitly]
public sealed class HiveLeaderSquadBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private HiveLeaderSquadWindow? _window;

    public HiveLeaderSquadBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        EnsureWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not HiveLeaderSquadBuiState s)
            return;

        _window = EnsureWindow();
        _window.UpdateState(s, GetTexture, OnAnnounce, OnAddMember, OnRemoveMember);
    }

    private HiveLeaderSquadWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = this.CreateWindow<HiveLeaderSquadWindow>();
        return _window;
    }

    private Texture? GetTexture(EntProtoId? id)
    {
        if (id == null || !_prototype.TryIndex(id.Value, out var proto))
            return null;
        return _sprite.Frame0(proto);
    }

    private void OnAnnounce(string message) =>
        SendPredictedMessage(new HiveLeaderSquadAnnounceMsg(message));

    private void OnAddMember(NetEntity xeno) =>
        SendPredictedMessage(new HiveLeaderAddMemberMsg(xeno));

    private void OnRemoveMember(NetEntity xeno) =>
        SendPredictedMessage(new HiveLeaderRemoveMemberMsg(xeno));
}
