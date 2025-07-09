using Content.Client._RMC14.UserInterface.FoodProcessorUI;
using Content.Shared._RMC14.Kitchen.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Kitchen.UI
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
            _menu.EjectButton.OnPressed += _ => SendPredictedMessage(new ProcessorEjectMessage());
            _menu.IngredientsList.OnItemSelected += args =>
            {
                SendPredictedMessage(new ProcessorEjectSolidIndexedMessage(EntMan.GetNetEntity(_solids[args.ItemIndex])));
            };

        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not ProcessorUpdateUserInterfaceState cState || _menu == null)
            {
                return;
            }

            _menu.IsBusy = cState.IsProcessorBusy;

            _menu.ToggleBusyDisableOverlayPanel(cState.IsProcessorBusy || cState.ContainedSolids.Length == 0);
            // TODO move this to a component state and ensure the net ids.
            RefreshContentsDisplay(EntMan.GetEntityArray(cState.ContainedSolids));

            //Set the "micowave light" ui color to indicate if the processor is busy or not
            if (cState.IsProcessorBusy && cState.ContainedSolids.Length > 0)
            {
                _menu.IngredientsPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#947300") };
            }
            else
            {
                _menu.IngredientsPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#1B1B1E") };
            }
        }

        private void RefreshContentsDisplay(EntityUid[] containedSolids)
        {
            _reagents.Clear();

            if (_menu == null) return;

            _solids.Clear();
            _menu.IngredientsList.Clear();
            foreach (var entity in containedSolids)
            {
                if (EntMan.Deleted(entity))
                {
                    return;
                }

                // TODO just use sprite view

                Texture? texture;
                if (EntMan.TryGetComponent<IconComponent>(entity, out var iconComponent))
                {
                    texture = EntMan.System<SpriteSystem>().GetIcon(iconComponent);
                }
                else if (EntMan.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
                {
                    texture = spriteComponent.Icon?.Default;
                }
                else
                {
                    continue;
                }

                var solidItem = _menu.IngredientsList.AddItem(EntMan.GetComponent<MetaDataComponent>(entity).EntityName, texture);
                var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                _solids.Add(solidIndex, entity);
            }
        }
    }
}
