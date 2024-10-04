mindslave-briefing-slave = Ваша воля сломлена инородным ИИ, теперь вы подчиняетесь { $master }.
mindslave-removed-slave = Ваш разум вновь помутнен... Вы забираете контроль над своей волей, однако забываете всё, что произошло после потери контроля!
mindslave-briefing-slave-master = Вы получили контроль над { $name }! Теперь {SUBJECT($ent)} подчиняется вашей воле.
mindslave-removed-slave-master = Вы потеряли контроль над { $name }! {CAPITALIZE(SUBJECT($ent))} забывает всё, что произошло после потери контроля.
mindslave-removed-slave-master-unknown = Вы потеряли контроль над одним из своих подчиненных разумов!
# хз че еще тут написать можно было
mindslave-unknown-master = ЛИЧНОСТЬ СКРЫТА
mindslave-enslaving-yourself-attempt = Нельзя подчинить разум самому себе!
mindslave-target-already-enslaved = Цель уже подчинена!
mindslave-target-mindshielded = Разум цели сопротивляется!
mindslave-master-dead = Ваш подчинитель погиб! Вам необходимо как можно быстрее вернуть его к жизни!
mindslave-master-crit = Ваш подчинитель находится в критическом состоянии! Вам необходимо срочно помочь ему!

#ui
mindslave-notification-window-title = Подчиненный разум
mindslave-notification-window-text-enslaved =
  Ваш разум был подчинён! Отныне, вы обязаны исполнять ВСЕ приказы { $name }.
  Однако, вы не можете:
  навредить себе (инстинкт самосохранения выше),
  навредить своему подчинителю,
  раскрывать личность своего подчинителя.
mindslave-notification-window-text-freed =
  Вы освободились от оков подчинения!
  Вы забываете всё, что произошло после того, как вы стали подчиненным.
  Как имя подчинителя, так и сами действия.
mindslave-notification-window-accept = Понятно

#entities
ent-MindSlaveImplanter = имплантер Подчинитель разума
    .desc = Одноразовый имплантер, содержащий имплант, который подчиняет разум владельца тому, кто установил имплант.
ent-MindSlaveImplant = Подчинитель разума
    .desc = Этот имплант подчиняет разум владельца тому, кто установил имлпант. При извлечении импланта контроль над разумом теряется.

#alert
alerts-mindslave-name = Подчиненный разум
alerts-mindslave-desc = Ваш разум был подчинен

#role
roles-antag-syndicate-mindslave-name = Подчиненный разум
roles-antag-syndicate-mindslave-objective = Ваш разум подчинен! Вы обязаны исполнять волю своего подчинителя.
objective-condition-mindslave-obey-master = Подчиняться воле { $targetName }, { $job }.
ent-MindSlaveObeyObjective = Подчиняться воле другого.
    .desc = Ваш разум теперь находится под контролем другого, следуйте его воле и сохраните его жизнь.
