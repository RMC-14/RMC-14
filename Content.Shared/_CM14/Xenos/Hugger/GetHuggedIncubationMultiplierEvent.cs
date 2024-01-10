namespace Content.Shared._CM14.Xenos.Hugger;

[ByRefEvent]
public record struct GetHuggedIncubationMultiplierEvent(float Multiplier = 1)
{
    public void Add(float multiplier)
    {
        Multiplier += multiplier;
    }

    public void Multiply(float multiplier)
    {
        Multiplier *= multiplier;
    }
}
