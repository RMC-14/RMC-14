reagent-name-rmc-xenoblood = Acidic Blood
reagent-desc-rmc-xenoblood = A corrosive blood like substance. It appears to be made out of acids and blood plasma.

reagent-name-rmc-xenobloodroyal = Dark Acidic Blood

reagent-name-rmc-plasma = Plasma
reagent-desc-rmc-plasma = A clear extracellular fluid separated from blood.

reagent-name-rmc-xenoplasmapurple = Purple Plasma
reagent-desc-rmc-xenoplasmapurple = A purple-ish plasma...

reagent-name-rmc-xenoplasmapheromone = Pheromone Plasma
reagent-desc-rmc-xenoplasmapheromone = A funny smelling plasma...

reagent-name-rmc-xenoplasmachitin = Chitin Plasma
reagent-desc-rmc-xenoplasmachitin = A very thick fibrous plasma...

reagent-name-rmc-xenoplasmacatecholamine = Catecholamine Plasma
reagent-desc-rmc-xenoplasmacatecholamine = A red-ish plasma...

reagent-name-rmc-xenoplasmaneurotixin = Neurotoxin Plasma
reagent-desc-rmc-xenoplasmaneurotixin = A plasma containing an unknown but potent neurotoxin.

reagent-name-rmc-xenoplasmaantineurotixin = Anti-Neurotoxin
reagent-desc-rmc-xenoplasmaantineurotixin = A counteragent to Neurotoxin Plasma.
reagent-effect-guidebook-neurotoxin-immunity =
    { $chance ->
        [1] Provides
        *[other] provide
    } immunity against alien neurotoxin

reagent-effect-guidebook-excreting =
    { $chance ->
        [1] Removes
        *[other] remove
    } all other reagents from the body

reagent-name-rmc-xenoplasmaegg = Egg Plasma
reagent-desc-rmc-xenoplasmaegg = A white-ish plasma with a high concentration of protein...
reagent-effect-plasma-egg = Your stomach cramps and you suddenly feel very sick!
reagent-effect-guidebook-plasma-egg =
    { $chance ->
        [1] Infects
        *[other] infect
    } an individual with an alien parasite

reagent-name-rmc-xenoplasmaroyal = Royal Plasma
reagent-desc-rmc-xenoplasmaroyal = A dark purple-ish plasma...

reagent-effect-condition-guidebook-blood-threshold =
    { $max ->
        [2147483648] there's at least {NATURALFIXED($min, 2)}u of blood
        *[other] { $min ->
                    [0] there's at most {NATURALFIXED($max, 2)}u of blood
                    *[other] there's between {NATURALFIXED($min, 2)}u and {NATURALFIXED($max, 2)}u of blood
                 }
    }

reagent-effect-condition-guidebook-infected =
    the individual { $infected ->
        [true] hosts
        *[false] does not host
    } an alien parasite
