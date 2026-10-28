namespace Content.Shared._RMC14.Botany;

[RegisterComponent]
public sealed partial class FloraDataDiscComponent : Component
{
    public List<PlantGene> Genes = new();
    public string? GeneSource;
}
