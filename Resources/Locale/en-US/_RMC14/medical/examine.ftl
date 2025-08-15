rmc-medical-examine-unrevivable = [color=purple][italic]{CAPITALIZE(POSS-ADJ($victim))} eyes have gone blank, there are no signs of life.[/italic][/color]

rmc-medical-examine-headless = [color=purple][italic]{CAPITALIZE(SUBJECT($victim))} {CONJUGATE-BE($victim)} definitely dead.[/italic][/color]

rmc-medical-examine-unconscious = [color=lightblue]{ CAPITALIZE(SUBJECT($victim)) } { GENDER($victim) ->
    [epicene] seem
    *[other] seems
  } to be unconscious.[/color]

rmc-medical-examine-dead = [color=red]{CAPITALIZE(SUBJECT($victim))} {CONJUGATE-BE($victim)} not breathing.[/color]

rmc-medical-examine-dead-simple-mob = [color=red]{CAPITALIZE(SUBJECT($victim))} {CONJUGATE-BE($victim)} DEAD. Kicked the bucket.[/color]

rmc-medical-examine-dead-xeno = [color=red]{CAPITALIZE(SUBJECT($victim))} {CONJUGATE-BE($victim)} DEAD. Kicked the bucket. Off to that great hive in the sky.[/color]

rmc-medical-examine-alive = [color=green]{CAPITALIZE(SUBJECT($victim))} {CONJUGATE-BE($victim)} alive and breathing.[/color]

rmc-medical-examine-bleeding = [color=#d10a0a]{CAPITALIZE(SUBJECT($victim))} {CONJUGATE-HAVE($victim)} bleeding wounds on {POSS-ADJ($victim)} body.[/color]

rmc-medical-examine-verb = Show medical actions
