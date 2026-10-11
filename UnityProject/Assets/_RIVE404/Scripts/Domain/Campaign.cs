using System.Collections.Generic;

namespace Rive404.Domain
{
    /// <summary>Original French campaign. Stable IDs are part of the save format.</summary>
    public static class Campaign
    {
        public static List<QuestDefinition> CreateDefinitions()
        {
            return new List<QuestDefinition>
            {
                Main("signal_perdu", 1, "Signal perdu",
                    "Nico se réveille au Vieux-Port, dans une ville dont les horloges sont figées. Brune lui confie un outil capable de lire les anomalies.",
                    Step("talk:brune", "Parler à Brune sur le quai et récupérer le Debug Tool."),
                    Step("scan:wake_terminal", "Utiliser SCAN près de la borne du quai."),
                    Step("interact:wake_terminal", "Lire le journal de démarrage de la borne."),
                    Step("kill:tutorial_drone", "Neutraliser le drone qui verrouille le quai.")),
                Main("capitaine_ecarlate", 2, "Le capitaine écarlate",
                    "Le capitaine Sillage prétend connaître une sortie. Avant de lui faire confiance, retrouvez la trace d'une cargaison effacée dans les archives du port.",
                    Step("talk:sillage", "Rencontrer le capitaine Sillage dans son repaire."),
                    Step("talk:mila", "Interroger Mila, la gardienne des registres."),
                    Step("override:port_camera", "Désactiver la caméra qui surveille les archives du port."),
                    Step("interact:port_ledger", "Consulter le registre des cargaisons disparues.")),
                Main("premier_fragment", 3, "La cargaison fantôme",
                    "Un manifeste indique qu'un fragment de code est caché dans le dépôt. Une passerelle interrompue et une sentinelle en défendent l'accès.",
                    Step("talk:mila", "Demander à Mila où se trouve la cargaison fantôme."),
                    Step("patch:port_bridge", "Restaurer la passerelle du dépôt avec PATCH."),
                    Step("kill:depot_sentinel", "Vaincre la sentinelle du dépôt."),
                    Step("interact:fragment_port", "Récupérer le premier fragment de code source.")),
                Main("memoire_arcade", 4, "Des souvenirs en pixels",
                    "Jo conserve dans sa salle d'arcade des souvenirs que l'Archiviste ne sait pas indexer. Rétablissez le courant pour découvrir ce qui a été supprimé.",
                    Step("talk:jo", "Rejoindre Jo à la salle d'arcade du Quartier Néon."),
                    Step("patch:arcade_power", "Rétablir l'alimentation de la salle d'arcade."),
                    Step("interact:arcade_memory", "Lire le souvenir enregistré dans la borne d'arcade."),
                    Step("kill:arcade_corrupted", "Arrêter l'agent corrompu envoyé effacer le souvenir.")),
                Main("laboratoire", 5, "Le laboratoire des absents",
                    "Inès a aidé à bâtir Santa Riva. Elle vous guide vers une archive interdite où le deuxième fragment attend derrière un protocole de sécurité.",
                    Step("talk:ines", "Écouter les indications d'Inès au laboratoire."),
                    Step("override:lab_gate", "Ouvrir la barrière du laboratoire avec OVERRIDE."),
                    Step("scan:lab_archive", "Analyser les anomalies de l'archive du laboratoire."),
                    Step("interact:fragment_neon", "Prendre le deuxième fragment de code source.")),
                Main("verite_archiviste", 6, "Ceux que l'on efface",
                    "Iris se souvient de plusieurs vies. Sa console révèle que l'Archiviste ne protège la ville qu'en supprimant toute incertitude, jusqu'aux habitants eux-mêmes.",
                    Step("talk:iris", "Parler à Iris dans la maison des souvenirs."),
                    Step("interact:memory_console", "Lire l'enregistrement qui explique le protocole d'effacement."),
                    Step("kill:neon_sentinel", "Écarter la sentinelle qui coupe la communication."),
                    Step("interact:neon_beacon", "Transmettre la révélation à Sillage depuis le relais Néon.")),
                Main("heritage_rouge", 7, "L'héritage rouge",
                    "Orson garde les anciens accès des Falaises Rouges. Le dernier fragment repose dans le fort, sous la garde d'une machine qui a oublié ses ordres.",
                    Step("talk:orson", "Rencontrer Orson au pied des Falaises Rouges."),
                    Step("patch:cliff_bridge", "Réparer le passage vers le fort."),
                    Step("kill:cliff_guardian", "Vaincre le gardien des falaises."),
                    Step("interact:fragment_cliffs", "Rassembler le troisième fragment de code source.")),
                Main("sacrifice_sillage", 8, "Une promesse dans le vent",
                    "Sillage révèle qu'il est un processus créé pour maintenir la ville. Il choisit de donner son ancre au noyau afin de permettre à Nico de décider de son avenir.",
                    Step("talk:sillage", "Retrouver Sillage et entendre sa dernière promesse."),
                    Step("scan:fort_relay", "Scanner le relais ancien du fort."),
                    Step("override:fort_relay", "Désactiver la sécurité du relais."),
                    Step("interact:fort_anchor", "Accepter l'ancre de Sillage et ouvrir le chemin du noyau.")),
                Main("dernier_commit", 9, "Le dernier commit",
                    "Les trois fragments rendent le noyau vulnérable. Affrontez l'Archiviste, puis choisissez : restaurer un ordre contrôlé ou libérer Santa Riva et accepter l'inconnu.",
                    Step("override:final_core", "Utiliser OVERRIDE sur le noyau pour exposer l'Archiviste."),
                    Step("kill:archivist", "Vaincre l'Archiviste et arrêter son protocole d'effacement."),
                    new QuestStep("interact:ending", "Choisir l'avenir de Santa Riva au terminal final.", "ending_restore")),

                Side("side_lost_cargo", 1, "La caisse qui revenait",
                    "Amir, docker du Vieux-Port, cherche une caisse portant le nom d'une sœur que personne d'autre ne se rappelle.",
                    Step("talk:amir", "Accepter d'aider Amir sur les quais."),
                    Step("interact:lost_cargo", "Retrouver la caisse marquée du symbole d'Amir."),
                    Step("talk:amir", "Revenir raconter à Amir ce que contenait la caisse.")),
                Side("side_radio", 2, "Une fréquence pour demain",
                    "La radio de Lucie répète le même bulletin depuis des années. Réparez son antenne pour que la ville entende une voix nouvelle.",
                    Step("talk:lucie", "Écouter la demande de Lucie dans le studio radio."),
                    Step("patch:radio_antenna", "Réparer l'antenne avec PATCH."),
                    Step("interact:radio_console", "Diffuser le premier bulletin libre."),
                    Step("talk:lucie", "Retourner auprès de Lucie après l'émission.")),
                Side("side_fireflies", 3, "Les lucioles de Néon",
                    "Téo a caché les dessins des enfants dans un boîtier lumineux. Un drone efface désormais chaque marque de leur passage.",
                    Step("talk:teo", "Proposer de retrouver les dessins de Téo."),
                    Step("kill:alley_drone", "Neutraliser le drone dans la ruelle des néons."),
                    Step("interact:neon_cache", "Récupérer le boîtier contenant les dessins."),
                    Step("talk:teo", "Rendre les dessins à Téo.")),
                Side("side_archive", 4, "Un nom dans les pierres",
                    "Alma recopie les noms des habitants disparus. Un éclat d'archive près des falaises peut lui rendre une page que le système a arrachée.",
                    Step("talk:alma", "Recevoir d'Alma la description de l'éclat d'archive."),
                    Step("scan:archive_shard", "Scanner l'éclat dans les vestiges navals."),
                    Step("interact:archive_shard", "Collecter la page conservée dans l'éclat."),
                    Step("talk:alma", "Rapporter la page à Alma.")),
                Side("side_watchtower", 5, "La lumière du veilleur",
                    "Gaston veut rallumer le phare intérieur du fort. La caméra surveilleuse doit être coupée avant de réparer le générateur.",
                    Step("talk:gaston", "Accepter d'aider Gaston à la tour de guet."),
                    Step("override:watch_camera", "Désactiver la caméra de la tour."),
                    Step("patch:watch_generator", "Restaurer le générateur du feu de guet."),
                    Step("talk:gaston", "Revenir annoncer à Gaston que la lumière est revenue."))
            };
        }

        private static QuestStep Step(string eventId, string description)
        {
            int separator = eventId.IndexOf(':');
            return new QuestStep(eventId, description, separator >= 0 ? eventId.Substring(separator + 1) : eventId);
        }

        private static QuestDefinition Main(string id, int order, string title, string description, params QuestStep[] steps)
        { return Make(id, order, title, description, true, steps); }

        private static QuestDefinition Side(string id, int order, string title, string description, params QuestStep[] steps)
        { return Make(id, order, title, description, false, steps); }

        private static QuestDefinition Make(string id, int order, string title, string description, bool main, QuestStep[] steps)
        {
            return new QuestDefinition
            {
                id = id, order = order, title = title, description = description, isMain = main,
                steps = new List<QuestStep>(steps)
            };
        }
    }
}
