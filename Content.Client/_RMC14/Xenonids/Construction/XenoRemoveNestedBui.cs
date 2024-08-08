using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Construction.Nest;

namespace Content.Client._RMC14.Xenonids.Construction
{
    public sealed class XenoRemoveNestedBui : BoundUserInterface
    {
        [ViewVariables]
        private XenoRemoveNestedWindow? _window;

        public XenoRemoveNestedBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new XenoRemoveNestedWindow();
            _window.OnClose += Close;

            _window.ConfirmButton.OnPressed += _ => Close();

            _window.OpenCentered();
        }

        private void RemoveNested()
        {
            if (_window == null)
                return;
            var msg = new XenoRemoveNestedBuiMsg(true);
            SendPredictedMessage(msg);
            _window.Close();
        }
    }
}
