using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.RMCCustomHoliday;

[Prototype("customHoliday")]
public sealed class CustomHolidayPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField("name")] public string Name { get; private set; } = default!;
    [DataField("beginDay")] public int BeginDay { get; private set; }
    [DataField("beginMonth")] public string BeginMonth { get; private set; } = default!;
    [DataField("description")] public string Description { get; private set; } = default!;
}
