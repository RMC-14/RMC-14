**Delete this file when you are done**
**STANDARD TODO**
~~X~~ means its done
The list will mimic file path. If you change something update it here please.

Icon Command is probabblyyy broken now `Content.Server/_RMC14/Marines/Icons/IconCommand.cs`
decide final file structure (run through Whisper most likely)
Make sure no unneeded .rsi ending
Ensure end of tree files do have .rsi
This all errors out but kinda expected since its not done
test and test and test (I will spawn every role in if I have to) - Bailey
The "other" folder here is terrible and we maybe want to find a better solution for it
Delete `interface/marine_hud` and move any usages to either just base interface or into job_icons. Its entirely depreciated

**META NEEDS CREATED**
~~CLF~~
CMB
Freelancer
Halcyon
MISC
    fireteam
    mutiny
    orders # - Not even partially done like some of the others now
    ~~Colonist Lobby~~ # Maybe needs clarification on if they are based off anything
other
SPP - Some of this has been moved over but states need done and the copyright needs done too
    Kommissar
    Standard # Rename UPP to SPP and update meta too
TSE
    RCM
        paramarine
        pve
        standard
    TSEPA
    other
UNMC
    auxilary_support
    command
    engineering
    ~~high_command~~
    marine
    medical
    military_police
    other
    provost
    requsitions
    Marsoc
WE-YA
    goon
    liaison_ranks
    pmc
    other
whiskey_outpost
~~xeno~~

**CODE SIDE RENAME/UPDATE NEEDS DONE FOR EACH PNG**
~~CLF~~
~~CMB~~
~~Freelancer~~
~~Halcyon~~
~~MISC~~
    ~~fireteam~~ # These aren't used anywhere in code, we can probably remove these from this folder, add to the todo later?
    ~~mutiny~~
    ~~orders~~ # Thesse aren't used anywhere from this folder but that seems wrong? Maybe its fine to remove these too
    ~~Colonist Lobby~~
SPP
    ~~Kommissar~~
    Standard # Rename from UPP to SPP before doing, probably do the meta alongside to prevent confusion
~~TSE~~
    ~~RCM~~
        ~~paramarine~~
        ~~pve~~
        ~~standard~~
    ~~TSEPA~~
    ~~other~~ # File search claims this isn't used anywhere but I don't buy it
UNMC
    ~~auxilary_support~~
    ~~command~~
    ~~engineering~~
    ~~high_command~~
    marine # This one is only partially done
    ~~medical~~
    ~~military_police~~
    ~~other~~
    ~~provost~~ # (Move this somewhere else?)
    requsitions
    Marsoc
~~WE-YA~~
    ~~goon~~
    ~~liaison_ranks~~
    ~~pmc~~
    ~~other~~
~~whiskey_outpost~~
~~xeno~~
