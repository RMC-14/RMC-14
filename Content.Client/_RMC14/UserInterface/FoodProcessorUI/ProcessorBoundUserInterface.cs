using Content.Shared._RMC14.Kitchen;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.UserInterface.FoodProcessorUI
{
    public sealed class ProcessorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ProcessorMenu? _menu;

        public ProcessorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<ProcessorMenu>();
            _menu.OnProcess += StartProcessing;
            _menu.OnEjectAll += EjectAll;
            _menu.OnEjectChamber += EjectChamberContent;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not ProcessorInterfaceState cState)
                return;

            _menu?.UpdateState(cState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            _menu?.HandleMessage(message);
        }

        public void StartProcessing()
        {
            SendMessage(new ProcessorStartMessage(ProcessorProgram.Process));
        }

        public void EjectAll()
        {
            SendMessage(new ProcessorEjectChamberAllMessage());
        }

        public void EjectChamberContent(EntityUid uid)
        {
            SendMessage(new ProcessorEjectChamberContentMessage(EntMan.GetNetEntity(uid)));
        }
    }
}
