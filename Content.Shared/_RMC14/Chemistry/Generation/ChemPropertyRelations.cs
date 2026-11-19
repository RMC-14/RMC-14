namespace Content.Shared._RMC14.Chemistry.Generation;

public static class ChemPropertyRelations
{
    public static readonly List<(string A, string B)> Conflicts = new()
    {
        ("Nutritious", "Hemorrhaging"), ("Nutritious", "Hemolytic"), ("Toxic", "Antitoxic"),
        ("Corrosive", "Anticorrosive"), ("Biocidic", "Neogenetic"), ("Hyperthermic", "Hypothermic"),
        ("Nutritious", "Ketogenic"), ("Neuropathic", "Painkilling"), ("Hallucinogenic", "Antihallucinogenic"),
        ("Hepatotoxic", "Hepatopeutic"), ("Nephrotoxic", "Nephropeutic"), ("Pneumotoxic", "Pneumopeutic"),
        ("Oculotoxic", "Oculopeutic"), ("Cardiotoxic", "Cardiopeutic"), ("Neurotoxic", "Neuropeutic"),
        ("Fluxing", "Repairing"), ("Antispasmodic", "Musclestimulating"), ("Hemogenic", "Hemolytic"),
        ("Hemogenic", "Hemorrhaging"), ("Nutritious", "Emetic"), ("Hypergenetic", "Neogenetic"),
        ("Hypergenetic", "Hepatopeutic"), ("Hypergenetic", "Nephropeutic"), ("Hypergenetic", "Pneumopeutic"),
        ("Hypergenetic", "Oculopeutic"), ("Hypergenetic", "Cardiopeutic"), ("Hypergenetic", "Neuropeutic"),
        ("Addictive", "Antiaddictive"), ("Neuroshielding", "Neurotoxic"), ("Hypometabolic", "Hypermetabolic"),
        ("Hyperthrottling", "Neuroinhibiting"), ("Thermostabilizing", "Hyperthermic"),
        ("Thermostabilizing", "Hypothermic"), ("Aiding", "Neuroinhibiting"), ("Oxygenating", "Hypoxemic"),
        ("Anticarcinogenic", "Carcinogenic"), ("Transformative", "Antitoxic"),
        ("Intravenous", "Hypermetabolic"), ("Intravenous", "Hypometabolic"),
        ("Musclestimulating", "Nervestimulating"), ("Hemositic", "Nutritious"),
        // TODO RMC14: remove / change
        // ("Ciphering", "CipheringPredator"),
    };

    public static readonly Dictionary<string, List<string>> Combines = new()
    {
        ["Defibrillating"] = new List<string> { "Musclestimulating", "Cardiopeutic" },
        ["Thanatometabolizing"] =new List<string> { "Hypoxemic", "Cryometabolizing", "Neurocryogenic" },
        ["Hyperdensificating"] = new List<string> { "Musclestimulating", "Bonemending", "Carcinogenic" },
        ["Hyperthrottling"] = new List<string> { "Psychostimulating", "Hallucinogenic" },
        ["Neuroshielding"] = new List<string> { "Alcoholic", "Atrichogenic" },
        ["Antiaddictive"] = new List<string> { "Psychostimulating", "Antihallucinogenic" },
        ["Addictive"] = new List<string> { "Psychostimulating", "Neurotoxic" },
        // TODO RMC14: remove / change
        // ["CipheringPredator"] = new List<string> { "Ciphering", "Crossmetabolizing" },
        ["FirePenetrating"] = new List<string> { "Oxygenating", "Explosive" },
        ["Bonemending"] = new List<string> { "Crystallization", "Nutritious" },
    };

    public static bool AreConflicting(string a, string b)
    {
        foreach (var (x, y) in Conflicts)
        {
            if ((x == a && y == b) || (x == b && y == a))
                return true;
        }

        return false;
    }
}
