using Content.Shared._CM14.Medical;
using Content.Shared._CM14.Medical.HUD.Events;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Medical.HUD.Holocard
{
    [UsedImplicitly]
    public sealed class HolocardChangeBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private HolocardChangeWindow? _window;
        public HolocardChangeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new HolocardChangeWindow(this);

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
        public void ChangeHolocard(HolocardStaus newHolocardStatus)
        {
            if (_entities.GetNetEntity(_player.LocalEntity) is NetEntity viewer)
            {
                SendMessage(new HolocardChangeEvent(viewer, newHolocardStatus));
            }
        }

    }
}
