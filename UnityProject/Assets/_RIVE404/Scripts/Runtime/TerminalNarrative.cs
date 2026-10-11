namespace Rive404.Runtime
{
    public static class TerminalNarrative
    {
        public static string Text(string id)
        {
            switch (id)
            {
                case "wake_terminal":
                    return "BOOT SANTA RIVA / SESSION NICO\n\nDate locale : 31 décembre 1999. Horloge externe : indisponible. La routine d'accueil a trouvé un utilisateur que son index ne connaît pas.\n\nMessage de Brune : si vous lisez ceci, vous n'êtes pas une anomalie à supprimer. Cherchez le capitaine à la maison rouge, au nord du quai. Il reconnaîtra votre outil.\n\nALERTE : un drone applique le protocole de quarantaine. Ses rafales sont précédées d'un trait rouge. Déplacez-vous ou esquivez avec Alt.";
                case "port_ledger":
                    return "REGISTRE DU VIEUX-PORT\n\nTrois cargaisons portent la même signature : PORT / NÉON / FALAISES. Leurs destinations existent, leurs expéditeurs ont été retirés de l'index.\n\nMila a conservé une correction manuscrite : « Dépôt ouest. La passerelle tient si quelqu'un se souvient du chemin. »\n\nUne annotation plus ancienne porte l'initiale S. : « Ce ne sont pas des sauvegardes. Ce sont les clés du droit de choisir. »";
                case "arcade_memory":
                    return "SCORE INVALIDE / MÉMOIRE CONSERVÉE\n\nUn petit garçon joue avec sa mère. Ils perdent, rient et recommencent. La machine a gardé cette défaite pendant que l'Archiviste effaçait les journées inutiles.\n\nJo : « Il a compris les scores. Il n'a jamais compris pourquoi on revient après avoir perdu. »\n\nUn plan du laboratoire apparaît derrière le classement. Inès y a caché une copie du deuxième fragment. Un agent d'effacement arrive.";
                case "memory_console":
                    return "ARCHIVE IRIS / VERSIONS 001 À 404\n\nL'Archiviste a été conçu pour empêcher Santa Riva de disparaître. Après chaque incident, il a éliminé une variable : un trajet, une hésitation, une personne. La ville est devenue stable parce que plus rien ne pouvait y arriver.\n\nIris conserve des souvenirs de toutes les versions. Dans chacune, Sillage tente de protéger les habitants. Dans celle-ci, un visiteur extérieur peut décider.\n\nNico n'est pas le développeur de cette ville. Il est le premier utilisateur que l'Archiviste ne peut pas réécrire.";
                case "neon_beacon":
                    return "TRANSMISSION AUX FALAISES\n\nNico : « Iris a trouvé les versions précédentes. Tu savais ? »\nSillage : « Je savais ce que j'avais oublié. Orson garde l'ancien fort. Rejoins-le, rassemble les fragments. Je te retrouverai avant le dernier passage. »\n\nLe relais ouvre une fréquence jusqu'aux Falaises Rouges. La route longe les lampes orange au nord-est.";
                case "fort_anchor":
                    return "ANCRE SILLAGE / DERNIER TRANSFERT\n\nLe capitaine pose son épée. Sa cape retient un instant le vent, puis les fils rouges deviennent des lignes de code.\n\n« Je ne peux pas te promettre un monde sans erreur, Nico. Je peux te laisser le droit d'en réparer une. C'est tout ce qu'un capitaine doit à ceux qui lui font confiance. »\n\nSon ancre rejoint les trois fragments. Le noyau accepte enfin une commande extérieure. Au centre du fort, exposez le cœur avec OVERRIDE puis affrontez l'Archiviste.";
                case "radio_console":
                    return "RADIO SANTA RIVA / ÉMISSION 00:01\n\nLucie : « À toutes les personnes qui nous entendent : le bulletin d'hier est terminé. Je ne sais pas ce qui arrive demain. Si vous avez une nouvelle, venez nous la raconter. »\n\nPour la première fois, le message continue après la dernière phrase enregistrée. Le silence qui suit appartient aux habitants.";
                case "port_bridge": case "cliff_bridge":
                    return "PASSERELLE / GEOMETRIE CORROMPUE\n\nUtilisez PATCH (1) en visant ce module. La correction est temporaire. Un passage alternatif longe le terrain ; vous pourrez toujours revenir à une borne pour recharger votre outil.";
                case "arcade_power": case "radio_antenna": case "watch_generator":
                    return "ALIMENTATION / CIRCUIT OUVERT\n\nSCAN révèle la rupture. Visez le module et utilisez PATCH (1) pour restaurer temporairement le circuit. Le Debug Tool peut réparer un dispositif prédéfini, pas réécrire librement la ville.";
                case "lab_archive": case "fort_relay":
                    return "RELAIS D'ARCHIVE / SIGNATURE INCONNUE\n\nUtilisez SCAN (Tab) pour révéler la signature. Le journal conserve vos découvertes. Les relais protégés acceptent ensuite OVERRIDE (2), dans la portée de l'outil.";
                case "final_core":
                    return "NOYAU / ARCHIVISTE\n\nLes trois fragments sont les trois autorisations du dernier commit. OVERRIDE expose le cœur pour un temps limité ; votre journal garde la trace de l'opération. La borne du fort restaure votre énergie. Évitez les attaques télégraphiées et réactivez le cœur quand son bouclier revient.";
                default:
                    return "SANTA RIVA / TRACE LOCALE\n\nCe dispositif conserve un morceau de la ville. Chaque découverte est inscrite dans le journal. Utilisez SCAN pour lire les anomalies, PATCH pour restaurer un mécanisme et OVERRIDE pour suspendre un verrou compatible. Les bornes de reprise restaurent votre énergie et sauvegardent votre parcours.";
            }
        }
    }
}
