using Content.Shared._RMC14.Kitchen.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.UserInterface.FoodProcessorUI
{
    [UsedImplicitly]
    public sealed class ProcessorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ProcessorMenu? _menu;

        [ViewVariables]
        private readonly Dictionary<int, EntityUid> _solids = new();

        [ViewVariables]
        private readonly Dictionary<int, ReagentQuantity> _reagents = new();

        public ProcessorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ProcessorMenu>();
            _menu.StartButton.OnPressed += _ => SendPredictedMessage(new ProcessorStartCookMessage());

            SendPredictedMessage(new ProcessorSelectCookTimeMessage(5, 0));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not ProcessorUpdateUserInterfaceState cState || _menu == null)
            {
                return;
            }

            _menu.IsBusy = cState.IsProcessorBusy;
            _menu.CurrentCooktimeEnd = cState.CurrentCookTimeEnd;

            _menu.ToggleBusyDisableOverlayPanel(cState.IsProcessorBusy || cState.ContainedSolids.Length == 0);

            //Set the cook time info label
            var cookTime = cState.ActiveButtonIndex == 0
                ? Loc.GetString("processor-menu-instant-button")
                : cState.CurrentCookTime.ToString();

            _menu.StartButton.Disabled = cState.IsProcessorBusy || cState.ContainedSolids.Length == 0;

        }
    }
}
