using Content.Shared._CM14.Medical;
using Content.Shared._CM14.Medical.Events;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Medical.HUD.Holocard
{
    [UsedImplicitly]
    public sealed class HolocardChangeBoundUserInterface : BoundUserInterface
    {
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
            SendMessage(new HolocardChangeEvent(newHolocardStatus));
        }

    }
}
