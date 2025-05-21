using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Food;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCFoodScoopingComponent : Component
{
    [DataField]
    public string SpoonSolution = "food";

    [DataField]
    public string FoodName = "";

    [DataField]
    public EntityUid? LastFood;
}
