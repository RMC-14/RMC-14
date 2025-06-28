using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.RMCCustomHoliday;

[Prototype]
public sealed class CustomHolidayPrototype : IPrototype
{
    [IdDataField] 
    public string ID { get; private set; } = default!;
    
    [DataField] 
    public string Name { get; private set; } = default!;
    
    [DataField] 
    public int BeginDay { get; private set; }
    
    [DataField] 
    public string BeginMonth { get; private set; } = default!;
    
    [DataField] 
    public string Description { get; private set; } = default!;
}
