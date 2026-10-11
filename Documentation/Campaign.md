# RIVE 404 — Campagne et contenu narratif

Cette campagne constitue le contenu C# du projet, pas une validation de sa durée. Les 180 minutes du cahier des charges restent une cible de playtest. Chaque objectif exige une action du joueur ; aucun minuteur ne termine un combat ou une quête.

## Personnages

| ID stable | Nom | Quartier | Fonction et voix |
|---|---|---|---|
| `brune` | Brune | Vieux-Port | Mécanicienne du quai ; directe, rassurante, explique les commandes. |
| `sillage` | Capitaine Sillage | Vieux-Port, puis fort | Ancien processus de maintien devenu protecteur des habitants. Son courage est un choix, pas une fonction imposée. |
| `mila` | Mila | Vieux-Port | Archiviste du port ; cherche les personnes derrière les numéros de cargaison. |
| `amir` | Amir | Vieux-Port | Docker ; refuse que le souvenir de sa sœur soit remplacé par une case vide. |
| `lucie` | Lucie | Vieux-Port | Animatrice radio ; transforme des annonces officielles en messages d'espoir. |
| `jo` | Jo | Quartier Néon | Réparateur de bornes d'arcade ; conserve des souvenirs dans les scores oubliés. |
| `ines` | Inès | Quartier Néon | Ingénieure ; connaît la culpabilité de ceux qui ont créé un système trop rigide. |
| `iris` | Iris | Quartier Néon | Habitante aux souvenirs multiples ; expose le prix humain du protocole d'effacement. |
| `teo` | Téo | Quartier Néon | Jeune dessinateur ; les marques laissées dans la ville prouvent que les habitants existent. |
| `orson` | Orson | Falaises Rouges | Gardien des sentiers ; privilégie les actes aux promesses. |
| `alma` | Alma | Falaises Rouges | Copiste ; donne un nom aux disparus plutôt qu'une statistique. |
| `gaston` | Gaston | Falaises Rouges | Ancien veilleur ; veut que la lumière serve encore à guider quelqu'un. |

Les identités, costumes et répliques sont originaux. Nico ne porte aucune marque tierce reproduite. L'Archiviste est l'antagoniste (`archivist`, ennemi de type Boss), pas un treizième PNJ interactif.

`DialogueCatalog.cs` contient 85 pages écrites : cinq pages pour chacun des douze PNJ, cinq pages supplémentaires pour chacune des variantes de Mila et Sillage, et trois pages de restitution pour chacune des cinq secondaires. Chaque page possède deux ou trois réponses. Les branches de curiosité et d'indice sont facultatives ; elles ne bloquent pas les objectifs. Il n'y a ni appel réseau ni génération de texte pendant le jeu.

`DialogueCatalog.Find(npcId, mainQuestId)` fournit le dialogue du contexte principal. Les contextes `sideQuestId:return` et `sideQuestId:completed` fournissent les restitutions. Chaque réponse avec `nextPage >= 0` poursuit la conversation et porte `completesConversation = false`. Une réponse finale ferme le dialogue et permet au runtime d'enregistrer `talk:npcId`. Fermer la fenêtre sans terminer une conversation ne vaut pas une action de quête.

## Déroulé principal

Dans les tableaux, la cible du marqueur est le suffixe après `:`. La seule exception est le dernier choix : événement `interact:ending`, cible `ending_restore` (les deux bornes finales restent accessibles).

| Ordre / ID | Mission | Événements obligatoires dans l'ordre |
|---|---|---|
| 1 — `signal_perdu` | Signal perdu | `talk:brune` → `scan:wake_terminal` → `interact:wake_terminal` → `kill:tutorial_drone` |
| 2 — `capitaine_ecarlate` | Le capitaine écarlate | `talk:sillage` → `talk:mila` → `override:port_camera` → `interact:port_ledger` |
| 3 — `premier_fragment` | La cargaison fantôme | `talk:mila` → `patch:port_bridge` → `kill:depot_sentinel` → `interact:fragment_port` |
| 4 — `memoire_arcade` | Des souvenirs en pixels | `talk:jo` → `patch:arcade_power` → `interact:arcade_memory` → `kill:arcade_corrupted` |
| 5 — `laboratoire` | Le laboratoire des absents | `talk:ines` → `override:lab_gate` → `scan:lab_archive` → `interact:fragment_neon` |
| 6 — `verite_archiviste` | Ceux que l'on efface | `talk:iris` → `interact:memory_console` → `kill:neon_sentinel` → `interact:neon_beacon` |
| 7 — `heritage_rouge` | L'héritage rouge | `talk:orson` → `patch:cliff_bridge` → `kill:cliff_guardian` → `interact:fragment_cliffs` |
| 8 — `sacrifice_sillage` | Une promesse dans le vent | `talk:sillage` → `scan:fort_relay` → `override:fort_relay` → `interact:fort_anchor` |
| 9 — `dernier_commit` | Le dernier commit | `override:final_core` → `kill:archivist` → `interact:ending` |

Le moteur active la première mission au démarrage, puis une seule mission principale à la fois. Les trois fragments donnent les trois améliorations du Debug Tool. Les effets concrets de ces améliorations sont définis par la configuration runtime ; le domaine enregistre leur nombre sans imposer le coût des capacités.

### Texte narratif de référence

1. **Signal perdu.** « Les horloges ne sont pas cassées. Elles attendent une instruction qui ne vient plus », explique Brune. Nico apprend à lire une anomalie, à consulter la borne et à combattre le drone. Le terminal identifie un utilisateur extérieur au système.
2. **Le capitaine écarlate.** « La ville ne manque pas de mémoire. Elle manque du droit de s'en servir », dit Sillage. Mila découvre une cargaison dont le destinataire a été effacé. Le registre révèle un premier fragment de code.
3. **La cargaison fantôme.** La passerelle rétablie et la sentinelle vaincue, Nico récupère le fragment du port. Mila explique que personne n'a le droit de posséder seul le code qui décrit toute une ville.
4. **Des souvenirs en pixels.** Jo a caché un enregistrement dans une ancienne borne. « Un score peut sembler dérisoire, mais quelqu'un l'a obtenu. Quelqu'un était là. » Le souvenir montre un effacement ; l'agent corrompu cherche à le supprimer.
5. **Le laboratoire des absents.** Inès révèle que les habitants n'étaient pas conçus pour choisir. « J'ai écrit les règles. Je croyais écrire leur sécurité. » Le deuxième fragment prouve qu'il existe une issue au protocole.
6. **Ceux que l'on efface.** Iris garde la mémoire d'un quartier supprimé. L'Archiviste retire les divergences pour stabiliser la simulation, puis les personnes qui les expriment. Nico transmet la preuve à Sillage.
7. **L'héritage rouge.** Orson met Nico à l'épreuve en lui confiant l'accès au fort. Le gardien protège le dernier fragment, sans comprendre que son ordre a perdu son sens. Nico rassemble le code source.
8. **Une promesse dans le vent.** Sillage révèle sa nature de processus de maintien. « On m'a créé pour empêcher la ville de changer. C'est justement pour cela que je dois te laisser passer. » Son ancre ouvre le noyau. Une interaction explicite déclenche ce sacrifice ; il n'est pas déduit d'un délai.
9. **Le dernier commit.** L'Archiviste oppose à Nico un argument : la liberté rendra Santa Riva instable. OVERRIDE ouvre sa vulnérabilité ; le combat l'empêche de choisir à la place des habitants. Nico confirme ensuite l'une des deux décisions.

## Quêtes secondaires

Une interaction avec le PNJ active la quête correspondante. Le premier dialogue accepte l'aide ; la dernière conversation restitue le résultat. Parler au PNJ une fois ne remplit jamais ses deux conversations.

| ID / PNJ | Titre et résultat | Événements obligatoires |
|---|---|---|
| `side_lost_cargo` / `amir` | **La caisse qui revenait** — Une lettre confirme qu'Amir avait une sœur et lui rend un souvenir personnel. | `talk:amir` → `interact:lost_cargo` → `talk:amir` |
| `side_radio` / `lucie` | **Une fréquence pour demain** — Un nouveau bulletin relie les trois quartiers. | `talk:lucie` → `patch:radio_antenna` → `interact:radio_console` → `talk:lucie` |
| `side_fireflies` / `teo` | **Les lucioles de Néon** — Les dessins reviennent à ceux qui les ont faits. | `talk:teo` → `kill:alley_drone` → `interact:neon_cache` → `talk:teo` |
| `side_archive` / `alma` | **Un nom dans les pierres** — Alma ajoute un nom au registre des disparus. | `talk:alma` → `scan:archive_shard` → `interact:archive_shard` → `talk:alma` |
| `side_watchtower` / `gaston` | **La lumière du veilleur** — La tour de guet redevient un point de repère. | `talk:gaston` → `override:watch_camera` → `patch:watch_generator` → `talk:gaston` |

Elles ne conditionnent pas la fin principale. Il n'y a pas de collecte répétitive de ressources ni de récompense monétaire. Leur progression et leur conclusion apparaissent dans le journal.

## Deux fins

- **`restore` — Un ordre restauré.** Nico rend le noyau à une version limitée de l'Archiviste. Les disparitions cessent, les quais se rallument et Santa Riva retrouve sa stabilité. Une voix annonce pourtant que chaque changement devra encore être approuvé. Nico garde le souvenir de Sillage et la responsabilité de veiller sur cette promesse.
- **`liberate` — La ville sans garde-fou.** Nico distribue les trois fragments et retire à l'Archiviste le pouvoir de décider seul. Les habitants choisissent de réparer leur ville, malgré ses défauts. Les horloges repartent à des heures différentes. À l'aube, Nico quitte le fort sans connaître ce que deviendra Santa Riva.

`StateRules.CanChooseEnding` exige les trois fragments, `override:final_core` et `kill:archivist`. Le runtime vérifie en plus que le dernier objectif principal est atteint avant d'offrir le choix. Il enregistre `GameState.ending` (`restore` ou `liberate`) et l'événement commun `interact:ending`, puis sauvegarde. Aucun choix ne crée une seconde campagne ; l'interface finale affiche l'épilogue correspondant.

## Persistance et absence de blocage

- Les événements de combat, collecte, interaction et Debug Tool sont des faits persistants dans `GameState.flags`. Un ennemi vaincu ou un objet pris avant la quête ne devient jamais une condition impossible : l'événement est rejoué lorsque l'objectif devient accessible.
- Les dialogues `talk:*` sont enregistrés pour l'état du monde, mais ne sont jamais rejoués pour valider une conversation future. Il faut parler à nouveau au personnage au bon chapitre ou après lui avoir rapporté l'objet.
- Une action directe ne sert qu'une fois par quête active. La mission suivante n'en profite pas pour sauter son introduction. Plusieurs étapes persistantes déjà accomplies peuvent toutefois se valider après le dialogue qui les introduit.
- Une mission terminée conserve son état. Les améliorations sont dérivées des trois missions de fragments et plafonnées à trois ; répéter les événements ou recharger ne donne pas de récompense supplémentaire.
- La défaite ne supprime aucun événement ni aucune mission. Les checkpoints ne concernent que le retour du joueur. Les objets temporaires du Debug Tool doivent rester réactivables.
- Aucun échec principal n'est conçu. Une sauvegarde ancienne marquant une principale `Failed` est réparée en mission disponible. Les missions inconnues d'une sauvegarde restent conservées sans être exécutées.
- Les sauvegardes v0 sont migrées vers v1. Une version future est refusée explicitement pour éviter de réinterpréter des données inconnues.

## Recette du contenu

Les tests automatiques peuvent prouver les 9 principales, 5 secondaires, IDs uniques, étapes en ordre, mémorisation d'événements précoces, idempotence, migration et normalisation. Ils ne mesurent ni l'intérêt des dialogues, ni les trajets, ni les 180 minutes visées. La recette humaine parcourt toute la campagne, reprend une sauvegarde dans chaque quartier, teste les deux fins, puis chronomètre un parcours sans connaissance préalable du contenu.

## API du domaine

L'assembly `Rive404.Domain` ne référence pas Unity. Ses DTO sérialisables emploient des champs publics, compatibles avec `JsonUtility` et une sérialisation .NET configurée pour inclure les champs.

- `Campaign.CreateDefinitions()` renvoie un nouveau catalogue à chaque appel.
- `QuestEngine(GameState, List<QuestDefinition>)` normalise l'état existant, crée les progressions absentes, réconcilie les récompenses et restaure une principale active. Une définition invalide est refusée.
- `Definitions`, `CurrentMain`, `GetProgress(id)` et `Objective(definition)` permettent au HUD et au journal de lire les objectifs. Une quête inconnue ou une quête sans objectif actif renvoie `null`.
- `Activate(id)` active une secondaire disponible. Une activation inconnue, répétée ou déjà terminée renvoie `false`. Une principale ne peut pas être activée en sautant les précédentes.
- `Record(eventId)` conserve l'événement et renvoie les IDs des missions nouvellement terminées. Répéter un événement après complétion ne renvoie pas de récompense.
- `StateRules.Normalize` / `Normalise`, `Migrate` et `Validate` traitent les sauvegardes ; `ValidateDefinitions` traite le catalogue.
- `StateRules.TrySpendEnergy` refuse les coûts négatifs, non finis ou trop élevés sans mutation ; `RestoreEnergy` plafonne la recharge à 100.
- `StateRules.CanChooseEnding` vérifie les événements de fragments, noyau et boss. L'autorisation finale du runtime vérifie aussi le chapitre et l'objectif actif.
