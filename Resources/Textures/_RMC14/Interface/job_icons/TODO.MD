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
The "other" folder in root is terrible and we maybe want to find a better solution for it
Delete `interface/marine_hud` and move any usages to either just base interface or into job_icons. Its entirely depreciated
File search claims orders and fireteams aren't used in code, check and delete them?
File search claims that the external faction icons aren't used here, check and delete them?
File Search claims hudmarineboardercommand is unused, remove it?
Do we want to move the provost folder outside of UNMC?

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
