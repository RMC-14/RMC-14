using Content.Shared._RMC14.Medical.HUD;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.HUD.Holocard;

[UsedImplicitly]
public sealed class HolocardChangeBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private HolocardChangeWindow? _window;

    public HolocardChangeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HolocardChangeWindow>();

        _window.HolocardStateList.OnItemSelected += obj =>
        {
            var newSelectedHolocard = (HolocardStatus?) obj.ItemList[obj.ItemIndex].Metadata;
            if (newSelectedHolocard is { } newHolocard)
            {
                if (_entities.GetNetEntity(_player.LocalEntity) is { } viewer)
                    SendMessage(new HolocardChangeEvent(viewer, newHolocard));

                Close();
            }
        };
    }
}
