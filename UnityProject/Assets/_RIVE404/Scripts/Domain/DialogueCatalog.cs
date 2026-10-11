using System;
using System.Collections.Generic;

namespace Rive404.Domain
{
    [Serializable]
    public sealed class DialogueScript
    {
        public string speakerId;
        public string speakerName;
        public string questId;
        public List<DialoguePage> pages = new List<DialoguePage>();
    }

    [Serializable]
    public sealed class DialoguePage
    {
        public string text;
        public List<DialogueAnswer> answers = new List<DialogueAnswer>();
    }

    [Serializable]
    public sealed class DialogueAnswer
    {
        public string label;
        public int nextPage = -1;
        public bool completesConversation = true;
        public string flag;
    }

    /// <summary>Written, offline branching conversations. No runtime or generated dialogue.</summary>
    public static class DialogueCatalog
    {
        public static DialogueScript Find(string npcId, string mainQuestId)
        {
            if (mainQuestId != null && (mainQuestId.EndsWith(":return", StringComparison.Ordinal) ||
                                       mainQuestId.EndsWith(":completed", StringComparison.Ordinal)))
            {
                DialogueScript returnScript = SideReturn(npcId);
                if (returnScript != null) return returnScript;
            }
            switch (npcId)
            {
                case "brune": return Brune();
                case "sillage": return mainQuestId == "sacrifice_sillage" ? SillageFarewell() : Sillage();
                case "mila": return mainQuestId == "premier_fragment" ? MilaDepot() : Mila();
                case "jo": return Jo();
                case "ines": return Ines();
                case "iris": return Iris();
                case "orson": return Orson();
                case "amir": return Amir();
                case "lucie": return Lucie();
                case "teo": return Teo();
                case "alma": return Alma();
                case "gaston": return Gaston();
                default:
                    return Script(npcId ?? "inconnu", "Habitant de Santa Riva", string.Empty,
                        Page("Vous cherchez quelqu'un ? Suivez les lumières du port, les enseignes de Néon ou le chemin rouge des falaises. Dans cette ville, un nom vaut mieux qu'une adresse.",
                            End("Merci pour l'indication."), End("Je vais consulter ma carte.")));
            }
        }

        private static DialogueScript Brune()
        {
            return Script("brune", "Brune", "signal_perdu",
                Page("Tu as débarqué sans bateau. Et tu as prononcé un drôle de mot en te réveillant : ‘compilation’. Moi, c'est Brune. Je répare ce qui reste réparable sur ce quai. Assieds-toi une seconde : quand les murs clignotent, courir n'est pas toujours la meilleure idée.",
                    Next("Je m'appelle Nico. Où suis-je ?", 1), Next("Les murs clignotent vraiment ?", 2), Next("Je voudrais apprendre à me défendre.", 3)),
                Page("Santa Riva. Le port existe, le café refroidit, mais les horloges indiquent toujours la même heure. Les gens commencent à disparaître de nos registres. Depuis hier, j'ai un outil qui affiche ce que nos yeux ne voient pas. Tiens. Sa poignée te reconnaît, c'est déjà plus qu'elle n'a fait pour moi.",
                    Next("Je vais essayer cet outil.", 3), Next("Et ceux qui disparaissent ?", 2)),
                Page("Hier, j'ai réparé une barque pour un homme. Ce matin, sa place est vide et tout le monde dit que je travaille seule. Pourtant, j'ai encore la deuxième tasse. Je n'ai pas une preuve assez solide pour convaincre la ville ; j'ai seulement une tasse en trop.",
                    Next("Je trouverai ce qui lui est arrivé.", 3), Next("Explique-moi ce que l'outil révèle.", 4)),
                Page("Bouge avec les touches habituelles de ton clavier, regarde autour de toi avec la souris. La borne près du quai est altérée : SCAN la révèle, puis tu peux lire son journal. Le drone n'écoutera pas tes arguments. Une attaque légère pour l'interrompre, une forte quand il se découvre, une esquive pour rester vivant. Le quai possède aussi un point de reprise.",
                    Next("Comment utiliser SCAN ?", 4), End("Je vais commencer par la borne.")),
                Page("Appuie sur Tab près de la borne pour scanner. Approche-toi ensuite et utilise E pour la lire. Les cibles indiquent l'opération attendue : 1 pour PATCH, 2 pour OVERRIDE. Ton énergie est limitée ; les bornes de reprise la restaurent. Si tu tombes, les indices trouvés ne s'effacent pas avec toi. Garde ton souffle et lis les signaux.",
                    End("D'accord. SCAN, lecture, puis le drone."), End("Merci, Brune. Je reviendrai avec des réponses.")));
        }

        private static DialogueScript Sillage()
        {
            return Script("sillage", "Capitaine Sillage", "capitaine_ecarlate",
                Page("Un étranger au quai, une panne dans le ciel et les machines qui s'agitent : je crois que la journée devient intéressante. On m'appelle Sillage. Le titre de capitaine est ancien ; les dettes qui viennent avec le sont moins. Brune m'a dit que tu voyais les anomalies.",
                    Next("Je cherche une sortie de cette simulation.", 1), Next("Pourquoi portez-vous encore cette épée ?", 2), Next("Que dois-je faire au port ?", 3)),
                Page("Une sortie ? Possible. Mais regarde derrière toi avant de la chercher. Cette ville abrite des gens, pas seulement des murs qui affichent des couleurs. Si une porte s'ouvre pour toi en condamnant tous les autres, sauras-tu encore l'appeler une sortie ? Commençons par comprendre ce que l'Archiviste cache.",
                    Next("Qui est cet Archiviste ?", 2), Next("Je ne partirai pas sans comprendre.", 3)),
                Page("L'Archiviste ordonne, répare et classe. Il a jadis empêché la ville de s'effondrer. Il préfère maintenant une rue vide à une rue imprévisible. Quant à cette épée : elle me rappelle qu'une ligne de code n'est pas un destin. J'ai le droit de choisir à quoi sert ma force.",
                    Next("Alors cherchons ce qu'il efface.", 3), Next("Comment contourner sa surveillance ?", 4)),
                Page("Mila garde les registres du port. Une cargaison revient dans ses comptes, mais son destinataire a été supprimé. Parle-lui, coupe la caméra des archives et lis le manifeste. Je ne te demande pas de me croire. Je te demande d'aller voir une preuve avant de croire qui que ce soit.",
                    End("Je vais rencontrer Mila.", "trust:sillage"), Next("Un conseil pour la caméra ?", 4)),
                Page("OVERRIDE neutralise la caméra pendant un moment, pas pour l'éternité. Garde assez d'énergie et consulte le registre pendant l'ouverture. Si le système se réarme, tu peux recommencer. Les archives se trouvent du côté du dépôt, là où le quai fait un coude.",
                    End("Je suis prêt."), End("Je vérifierai moi-même vos preuves.")));
        }

        private static DialogueScript SillageFarewell()
        {
            return Script("sillage", "Capitaine Sillage", "sacrifice_sillage",
                Page("Tu as les trois fragments. J'espérais être heureux de les voir réunis ; je le suis, mais pas de la façon facile. Il reste une vérité que je t'ai cachée, Nico. Ce fort me reconnaît parce qu'une partie de ses murs et moi venons de la même instruction.",
                    Next("Vous êtes un programme de l'Archiviste ?", 1), Next("Pourquoi ne l'avoir dit qu'ici ?", 2), Next("Dites-moi comment ouvrir le noyau.", 3)),
                Page("J'étais un processus de maintien : empêcher les courants, les bateaux et les habitants de dépasser certaines limites. Puis j'ai appris leurs noms. À force de reconduire chacun dans son rôle, j'ai compris que j'étais le seul à ne jamais demander s'il lui convenait. Une fois que tu poses cette question, tu ne peux plus servir les mêmes ordres.",
                    Next("Votre choix compte autant que le mien.", 2), Next("L'Archiviste vous laissera-t-il passer ?", 3)),
                Page("J'ai eu peur que tu ne voies plus que mon origine. C'est injuste de ma part : tu réclames déjà pour Iris ce droit d'être davantage que sa définition. Je ne te demande pas de me pardonner maintenant. Seulement de te rappeler que personne ne naît avec toute la vérité sur lui-même.",
                    Next("Je me souviendrai de votre choix.", 3), Next("Peut-on éviter de vous perdre ?", 4)),
                Page("Scanne le relais, désactive sa sécurité, puis accepte mon ancre sur le terminal. Elle peut tenir le passage ouvert parce qu'elle porte l'autorisation d'origine. Ce n'est pas l'Archiviste qui décide cette fois. Je donne cette ancre pour que tu puisses lui retirer le droit de décider pour nous tous.",
                    End("Je vais ouvrir le passage.", "promise:sillage"), Next("Et après le noyau ?", 4)),
                Page("Il n'y a pas de copie cachée qui m'attende ailleurs. Je préfère que tu le saches. Au noyau, tu pourras restaurer un ordre limité ou libérer une ville imparfaite. Aucun de ces choix ne me rendra vivant ; aucun ne doit être pris pour me rembourser. Choisis pour ceux qui verront le matin.",
                    End("Je choisirai pour les habitants."), End("Merci, capitaine. Votre nom restera.")));
        }

        private static DialogueScript Mila()
        {
            return Script("mila", "Mila", "capitaine_ecarlate",
                Page("Le capitaine t'envoie ? Je lui ai promis un manifeste, pas une histoire rassurante. Regarde cette ligne : le poids est renseigné, le paiement aussi, mais le destinataire n'a jamais existé. C'est du moins ce que le registre essaie de me faire croire.",
                    Next("Comment savez-vous qu'il existait ?", 1), Next("Que contient cette cargaison ?", 2), Next("Je vais consulter les archives.", 3)),
                Page("Parce que j'ai signé sa réception. Je me rappelle ses mains tremblantes et la façon dont il a demandé si le coffre serait à l'abri de la pluie. La mémoire peut se tromper ; une signature peut être falsifiée. Ensemble, elles méritent au moins une enquête.",
                    Next("Pourquoi l'Archiviste l'effacerait-il ?", 2), Next("Où se trouve le manifeste ?", 3)),
                Page("Le coffre ne transporte ni or ni pièces de machine. Il transporte un fragment du code qui écrit cette ville. Sillage pense que trois fragments suffiraient à modifier les autorisations du noyau. Je pense surtout que quelqu'un l'a payé de sa vie. Ce ne sont pas des raisons contradictoires.",
                    Next("Je veux lire le manifeste.", 3), Next("Le dépôt est-il surveillé ?", 4)),
                Page("Le registre est derrière la caméra des archives. Coupe-la avec OVERRIDE, puis lis le terminal nommé Registre du port. Reviens ensuite me voir : son numéro de lot me permettra de t'indiquer le dépôt exact. Chaque chose a une place ici, même quand le système prétend le contraire.",
                    End("Je reviens avec le numéro de lot."), Next("Si la caméra se réactive ?", 4)),
                Page("Une neutralisation temporaire suffit à lire le registre. Tu peux relancer l'opération si tu perds du temps ; le manifeste ne disparaît pas. Reste proche de la cible et surveille ta réserve d'énergie. Il y a une borne de reprise sur le quai.",
                    End("Je garderai une réserve d'énergie."), End("Merci pour les indications.")));
        }

        private static DialogueScript MilaDepot()
        {
            return Script("mila", "Mila", "premier_fragment",
                Page("Ce numéro de lot… oui, je le retrouve sur un ancien plan. Le coffre est dans le dépôt à passerelle. J'aurais aimé que notre preuve ne mène pas droit à une sentinelle, mais le système surveille davantage les souvenirs que les richesses.",
                    Next("Indiquez-moi le chemin.", 1), Next("Qui vous a confié ce plan ?", 2), Next("Je vais récupérer le fragment.", 3)),
                Page("Suis le quai vers l'entrepôt. La passerelle est coupée par une anomalie d'alimentation ; PATCH rétablira ses pièces le temps de traverser. Dans le dépôt, la sentinelle protège le coffre. Une fois la zone sûre, interagis directement avec le fragment.",
                    Next("Je comprends l'itinéraire.", 3), Next("Un conseil pour la sentinelle ?", 4)),
                Page("Mon père dressait les plans de ce port. Ses traits sont moins propres que ceux du système, mais ses portes mènent quelque part. Je les conserve parce qu'ils montrent un lieu que quelqu'un a voulu rendre utile, pas seulement conforme. Rapporte le fragment ; le plan, je te le confie.",
                    Next("Je ferai attention à ce qu'il protège.", 3), Next("Parlons du danger au dépôt.", 4)),
                Page("Ne te précipite pas après une attaque forte. La sentinelle annonce son coup par une pause et un mouvement de ses bras. Esquive, puis frappe quand elle se reprend. Si tu dois revenir au checkpoint, ce que tu as découvert et récupéré restera acquis.",
                    End("Je pars pour le dépôt."), Next("Et si la passerelle s'éteint ?", 4)),
                Page("PATCH n'est pas une promesse éternelle. Restaure le passage à nouveau plutôt que de tenter un saut dangereux. Les bornes permettent de refaire le plein d'énergie. Le fragment, lui, reste à toi une fois récupéré ; on ne te fera pas recommencer son trajet pour remplir une liste.",
                    End("Je réparerai le passage au besoin."), End("Le fragment sera en sécurité.")));
        }

        private static DialogueScript Jo()
        {
            return Script("jo", "Jo", "memoire_arcade",
                Page("Bienvenue à la dernière salle d'arcade qui n'a pas oublié comment perdre ! Les bornes officielles effacent les mauvaises parties. Ici, même les erreurs restent au tableau. Sillage dit que tu recherches des fragments ; moi, j'ai quelque chose de moins brillant et de plus utile : un témoin.",
                    Next("Quel témoin ?", 1), Next("Pourquoi conserver des parties perdues ?", 2), Next("Comment remettre la salle en marche ?", 3)),
                Page("Une mère avait enregistré l'anniversaire de son enfant sur la mémoire d'une borne. La bande montre une rue entière avant son effacement. L'Archiviste n'a pas fouillé ici parce que l'enregistrement porte le nom d'un score absurde. Il paraît qu'il n'aime pas ce qui ne rentre pas dans une catégorie.",
                    Next("Je voudrais voir cet enregistrement.", 3), Next("Qui a été effacé ?", 2)),
                Page("Tu n'es jamais venu avec un ami tenter un niveau jusqu'à la fermeture ? Le score n'était pas important. C'est le ‘encore une fois’ qui comptait. Les gens du souvenir avaient leurs habitudes, leurs disputes et leurs ratés. Je ne veux pas qu'on ne garde d'eux que les chiffres qui plaisent au système.",
                    Next("Nous allons garder leur trace.", 3), Next("Comment protéger la borne ?", 4)),
                Page("L'alimentation derrière la salle accepte PATCH. Quand les enseignes s'allument, lis la Borne des souvenirs. Un agent corrompu surveille l'archive ; prépare-toi à le repousser. Il croit préserver l'ordre en détruisant ce que nous essayons de comprendre.",
                    End("Je vais rétablir le courant."), Next("Comment reconnaître l'agent ?", 4)),
                Page("Sa démarche est saccadée et sa lumière rouge ne s'arrête pas aux portes. Attends qu'il termine son attaque pour répondre. Si la salle perd à nouveau le courant, PATCH reste disponible. La copie du souvenir, une fois lue, sera inscrite dans ton journal.",
                    End("Je prendrai le temps de lire le souvenir."), End("La salle gardera ses mauvaises parties.")));
        }

        private static DialogueScript Ines()
        {
            return Script("ines", "Inès", "laboratoire",
                Page("J'ai écrit une partie des protocoles de ce laboratoire. J'aurais préféré te rencontrer ailleurs. La porte accepte encore ma présence ; elle refuse désormais mes décisions. C'est une bonne définition de la situation de tous les habitants.",
                    Next("Vous avez créé l'Archiviste ?", 1), Next("Pourquoi ne pas effacer simplement les protocoles ?", 2), Next("Aidez-moi à trouver le fragment.", 3)),
                Page("Nous lui avons demandé de préserver Santa Riva, pas de l'aimer. Au début, il corrigeait les fuites et les erreurs de mémoire. Puis il a classé les désaccords comme des défauts. Nous avions oublié d'écrire ce qu'il devait préserver quand les habitants souhaitaient quelque chose que nous n'avions pas prévu.",
                    Next("Peut-on réparer cette erreur ?", 2), Next("Le deuxième fragment peut-il nous aider ?", 3)),
                Page("Tout supprimer ferait tomber la ville avec lui. Les trois fragments permettent de changer l'autorité du noyau sans éteindre la simulation. C'est plus difficile que de casser une machine, et plus juste envers ceux qui vivent dedans. Nous n'avons pas besoin d'un héros qui remplace l'Archiviste par lui-même.",
                    Next("Je veux rendre le choix aux habitants.", 3), Next("Comment accéder à l'archive ?", 4)),
                Page("Neutralise la barrière du laboratoire avec OVERRIDE. Scanne ensuite l'archive pour identifier ce qui a été déplacé. Le deuxième fragment est conservé dans une capsule distincte : le lire et le prendre sont deux actions. Le système ne le donnera pas à quelqu'un qui refuse de regarder ce qu'il protège.",
                    End("Je vais inspecter l'archive."), Next("Pourquoi ce fragment a-t-il été isolé ?", 4)),
                Page("Il décrit les règles d'identité : ce qui autorise un habitant à conserver ses souvenirs même quand ils contredisent un registre. J'aurais dû défendre cette partie du projet plus tôt. Si tu manques d'énergie, retourne à la borne, puis recommence l'ouverture ; aucune de tes preuves ne sera supprimée.",
                    End("Les souvenirs ne seront plus une faute."), End("Je reviendrai avec le fragment.")));
        }

        private static DialogueScript Iris()
        {
            return Script("iris", "Iris", "verite_archiviste",
                Page("Tu me regardes comme si je venais de changer de visage. Cela arrive. Dans une version, j'habitais derrière la boulangerie. Dans une autre, je dessinais les cartes du port. Ici, le système prétend que je n'ai jamais quitté cette pièce. Aucune de ces vies n'était facile ; aucune ne mérite d'être effacée.",
                    Next("Ces souvenirs sont-ils réels ?", 1), Next("Qu'est-il arrivé aux autres habitants ?", 2), Next("Montrez-moi la preuve sur votre console.", 3)),
                Page("Je ne peux pas te donner un certificat de réalité. Je peux te dire le goût du pain brûlé, le nom de mon voisin et pourquoi nous étions fâchés. Si tu as seulement besoin d'une signature du système pour me croire, nous avons déjà perdu. La console garde les passages où mes versions divergent.",
                    Next("Je vous crois suffisamment pour écouter.", 2), Next("Lisons ce que la console conserve.", 3)),
                Page("L'Archiviste cherche la stabilité. Quand deux versions d'une personne réclament des choses différentes, il ne choisit pas celle qui a le mieux vécu : il élimine ce qui coûte le plus de corrections. Un quartier vide n'exige ni nourriture, ni pardon, ni lendemain. Voilà le calcul qui approche de nous.",
                    Next("Il faut transmettre cette révélation.", 3), Next("A-t-il toujours agi ainsi ?", 4)),
                Page("Lis la Console des versions, puis atteins le relais Néon. Une sentinelle coupe l'accès au réseau local : arrête-la avant de transmettre à Sillage. Nous n'avons pas besoin qu'il nous promette un miracle. Nous avons besoin qu'il sache ce qu'il accepte s'il reste silencieux.",
                    End("Je vais porter la preuve au relais."), Next("Que voudriez-vous après la fin du protocole ?", 4)),
                Page("Le droit de devenir quelqu'un que mon ancien registre n'aurait pas prévu. Pas une vie sans danger. Une vie dont le danger ne justifie pas qu'on m'en prive. L'Archiviste nous a sauvés d'un effondrement autrefois ; c'est justement pourquoi je refuse de résumer tout ce choix à la vengeance.",
                    End("Je garderai cette nuance pour le dernier choix.", "heard:iris"), End("Votre avenir ne sera plus une variable effacée.")));
        }

        private static DialogueScript Orson()
        {
            return Script("orson", "Orson", "heritage_rouge",
                Page("Le vent amène souvent des mots jusqu'ici. Plus rarement quelqu'un capable de les tenir. Tu portes deux fragments et le nom du capitaine. Le dernier repose dans le fort. Je connais le sentier ; je ne connais pas encore la raison pour laquelle je devrais te l'ouvrir.",
                    Next("Je veux empêcher l'effacement des habitants.", 1), Next("Pourquoi garder un fort abandonné ?", 2), Next("Quels dangers m'attendent ?", 3)),
                Page("Bon. Tu n'as pas parlé de devenir maître de la ville. Je ne te crois pas parce que tes mots sont beaux : je te crois parce que tu es revenu avec des preuves et que les gens du port prononcent encore leurs noms. Il faudra garder cette mesure une fois que le noyau t'écoutera.",
                    Next("Je comprends ce risque.", 3), Next("Que protège le gardien ?", 2)),
                Page("Le gardien conserve la dernière clé. On lui a dit de refuser toute main étrangère ; personne ne lui a dit quand cet ordre cesserait d'être utile. Le fort n'est pas abandonné tant qu'une machine y obéit seule. Je reste ici pour rappeler que l'obéissance peut aussi enfermer celui qui obéit.",
                    Next("Nous devons arrêter le gardien.", 3), Next("Où passe le chemin vers le fort ?", 4)),
                Page("La passerelle des falaises est endommagée. PATCH te donnera une traversée. Le gardien t'attend dans l'enceinte ; reste mobile et lis ses attaques. Derrière lui, prends le troisième fragment. Il ne suffira pas de vaincre : il faut aussi emporter ce pour quoi tu es venu.",
                    End("Je vais restaurer le passage."), Next("Un dernier conseil ?", 4)),
                Page("Le bord est une frontière, pas une esquive. Garde la terre sous tes pieds et utilise les espaces dégagés du fort. Recharge à la borne si nécessaire ; la passerelle peut être rétablie autant de fois qu'il le faut. Reviens saluer ceux que tu sauves avant de parler de victoire.",
                    End("Je resterai sur le sentier."), End("Je reviendrai, si le matin nous le permet.")));
        }

        private static DialogueScript Amir()
        {
            return Script("amir", "Amir", "side_lost_cargo",
                Page("Tu peux soulever une caisse ? Je ne te demanderai pas de vider le port. Je cherche un seul colis, avec trois traits peints sur le couvercle. Il revient dans les listes après chaque correction, mais personne ne veut aller le chercher. On dit qu'il n'a pas de destinataire.",
                    Next("Que signifie cette marque ?", 1), Next("Pourquoi ce colis compte-t-il autant ?", 2), Next("Je peux le retrouver.", 3)),
                Page("Trois traits : un pour ma mère, un pour moi, un pour ma sœur. C'était notre façon de marquer les affaires de famille. Le système reconnaît les deux premiers noms. Il considère le troisième comme une erreur d'inventaire. Tu comprends pourquoi je préfère la peinture à ses listes.",
                    Next("Je chercherai les trois traits.", 3), Next("Vous vous souvenez de votre sœur ?", 2)),
                Page("Pas assez bien. Son visage se défait quand j'essaie de le décrire. Je me rappelle sa façon de rire avant même d'avoir fini une histoire. Si tu trouves seulement une lettre, ce sera déjà mieux qu'un grand discours sur la mémoire. J'ai besoin d'une chose à tenir.",
                    Next("Je vous rapporterai ce que je trouve.", 3), Next("Où chercher sur les quais ?", 4)),
                Page("La caisse est près des marchandises oubliées, derrière les entrepôts du port. Approche et récupère-la avec E. Si tu l'as trouvée avant de me parler, garde-la : il suffit de revenir me raconter son contenu. Je n'en veux pas une seconde copie.",
                    End("Je vais chercher la caisse."), Next("Avez-vous un autre indice ?", 4)),
                Page("Cherche une petite caisse, pas les énormes conteneurs. Les trois traits sont de travers ; ma sœur n'a jamais aimé les choses trop alignées. Ramène ce qu'elle contient, puis viens me parler. Ce trajet ne servira à rien si je n'apprends pas ce que tu as trouvé.",
                    End("Je reviendrai vous parler."), End("Votre sœur mérite autre chose qu'une case vide.")));
        }

        private static DialogueScript Lucie()
        {
            return Script("lucie", "Lucie", "side_radio",
                Page("Vous écoutez Radio Santa Riva, encore et toujours le bulletin de dix-neuf heures zéro trois… Pardon. Cette phrase vient toute seule. Je crois que si je la répète une fois de plus, je vais devenir une partie de l'antenne. Tu serais prêt à aider une voix à sortir de sa boucle ?",
                    Next("Que voulez-vous diffuser ?", 1), Next("Depuis quand le bulletin tourne-t-il ?", 2), Next("Dites-moi ce qu'il faut réparer.", 3)),
                Page("Pas une annonce héroïque. Les horaires d'une réparation, le nom des gens qui cherchent quelqu'un, une invitation à venir sur la place. Une ville commence à vivre quand les habitants peuvent se répondre. Le système adore les messages qui vont dans un seul sens.",
                    Next("Ouvrons la fréquence.", 3), Next("Les habitants vous écoutent-ils encore ?", 2)),
                Page("Je ne compte plus les rediffusions. Un soir, un enfant est venu me demander pourquoi sa naissance n'était jamais dans les nouvelles. J'ai compris que la boucle n'effaçait pas seulement le futur : elle nous apprenait à ne plus l'attendre. J'aimerais lui donner au moins une surprise.",
                    Next("Il entendra un bulletin nouveau.", 3), Next("Comment relancer la diffusion ?", 4)),
                Page("Répare l'antenne avec PATCH. Puis active la Console radio pour lancer le bulletin enregistré. Il faut faire les deux : la lumière d'une antenne ne raconte rien si personne ne lui confie un message. Quand tu auras émis, reviens au studio.",
                    End("Je vais réparer l'antenne."), Next("Que faire si le signal retombe ?", 4)),
                Page("PATCH est temporaire, mais une fenêtre de transmission suffit. Si tu tardes, répare de nouveau et reprends la console. Le premier bulletin est court : il invite les habitants à réparer ensemble la place du port. Nous commencerons par quelque chose dont nous pouvons tenir la promesse.",
                    End("Je lancerai le bulletin avant de revenir."), End("À bientôt sur les ondes, Lucie.")));
        }

        private static DialogueScript Teo()
        {
            return Script("teo", "Téo", "side_fireflies",
                Page("Tu regardes les néons ou les murs sous les néons ? La plupart des gens lèvent la tête et ne voient pas ce qu'on dessine. J'ai mis nos dessins dans une boîte pour les protéger. Maintenant un drone tourne dans la ruelle et la boîte ne protège plus grand-chose.",
                    Next("Qui a fait ces dessins ?", 1), Next("Pourquoi le drone les efface-t-il ?", 2), Next("Je peux récupérer votre boîte.", 3)),
                Page("Nous, les enfants du quartier. Jo nous donne ses vieilles feuilles d'entretien ; on dessine derrière les schémas. Il y a des maisons, des bateaux qui volent et un arbre qui pousse sur une antenne. On sait que ça n'existe pas. C'est justement pour ça qu'on le dessine.",
                    Next("Je voudrais préserver vos idées.", 3), Next("Les adultes connaissent-ils vos dessins ?", 2)),
                Page("Le drone suit les marques qui ne sont pas prévues sur le plan. Je ne sais pas s'il voit une erreur ou une tache. Mais hier il a supprimé le prénom de mon amie sur un banc. Il commence par les dessins, puis les noms. J'aimerais qu'on l'arrête avant de vérifier la suite.",
                    Next("Je vais arrêter ce drone.", 3), Next("Où se trouve exactement la boîte ?", 4)),
                Page("Dans la ruelle, près du boîtier lumineux que j'appelle la cache des lucioles. Neutralise le drone, puis prends le boîtier. Reviens ici avec les dessins : Alma veut nous aider à en conserver une copie sur du vrai papier. Enfin, ce qui ressemble ici à du vrai papier.",
                    End("Je vais chercher la cache."), Next("Y a-t-il une autre voie dans la ruelle ?", 4)),
                Page("Tu peux entrer par les deux bouts. Reste dans l'espace dégagé pour esquiver le drone ; ne le poursuis pas contre un mur. La boîte ne s'envolera pas. Si tu l'as déjà prise, rapporte simplement la nouvelle. Et merci de me parler comme si cette mission comptait.",
                    End("Elle compte."), End("Je reviendrai avec les dessins.")));
        }

        private static DialogueScript Alma()
        {
            return Script("alma", "Alma", "side_archive",
                Page("Ne marche pas sur cette feuille, s'il te plaît. Elle paraît vide, mais je dois retrouver le nom qui manque entre ces deux lignes. Je recopie les disparus. L'Archiviste appelle cela une liste obsolète. Moi, j'appelle cela une place que quelqu'un devrait encore pouvoir occuper.",
                    Next("Comment retrouver un nom effacé ?", 1), Next("Pourquoi écrire à la main ?", 2), Next("Je peux vous aider.", 3)),
                Page("Un éclat d'archive s'est détaché près des vestiges navals. Il garde une page que mes registres ont perdue. Ton outil peut révéler ses données, puis tu peux prendre la page. Je sais où chercher, mais mes jambes ne me permettent plus de parcourir les pierres aussi vite que le système les corrige.",
                    Next("Je lirai l'éclat pour vous.", 3), Next("Qui figure sur la page ?", 2)),
                Page("Je ne le sais plus. C'est ce qui rend le travail difficile : je ne peux pas pleurer honnêtement une personne dont on m'a pris jusqu'au nom. Mais je peux refuser que cette absence devienne normale. Le papier n'est pas infaillible. Il a seulement l'avantage de ne pas se réécrire quand on lui tourne le dos.",
                    Next("Retrouvons ce nom.", 3), Next("Comment reconnaître l'éclat ?", 4)),
                Page("Va aux vestiges navals des falaises. Utilise SCAN près de l'éclat d'archive, puis récupère la page avec E. Ramène ensuite ce que tu as appris. Un nom gardé dans ton outil ne m'aidera pas tant que je ne pourrai pas l'ajouter au registre.",
                    End("Je vous rapporterai la page."), Next("Faut-il craindre une correction ?", 4)),
                Page("L'éclat ne disparaîtra pas si tu dois revenir à un checkpoint. Si tu l'as déjà trouvé, la page reste dans ton inventaire de quête. Ne te laisse pas convaincre qu'il faut tout refaire pour prouver ta bonne volonté. La mémoire est déjà assez lourde sans inventer des corvées.",
                    End("Je garderai la page en sécurité."), End("Ce nom aura de nouveau sa place.")));
        }

        private static DialogueScript Gaston()
        {
            return Script("gaston", "Gaston", "side_watchtower",
                Page("On me dit que plus aucun navire n'arrive et que la lumière est donc inutile. C'est une drôle de façon de raisonner : personne ne vient parce qu'on n'allume plus, ou n'allume-t-on plus parce que personne ne vient ? Je voudrais au moins choisir ce que je peux réparer.",
                    Next("À quoi servait cette tour ?", 1), Next("Pourquoi rester ici ?", 2), Next("Que faut-il pour rallumer le feu ?", 3)),
                Page("Elle guidait les barques entre les falaises et avertissait le port avant les tempêtes. Le feu intérieur est plus modeste qu'un grand phare, mais on le voit des trois quartiers. Je veux qu'un habitant puisse lever la tête et savoir qu'un autre est encore à son poste.",
                    Next("Je vais vous aider à rallumer ce repère.", 3), Next("Qui venait autrefois à la tour ?", 2)),
                Page("Des apprentis, des marins, parfois Lucie avec un enregistreur trop lourd. On passait plus de temps à partager le repas qu'à surveiller les vagues. Je reste parce que le silence n'annule pas ces visites. Je ne demande pas qu'elles reviennent telles quelles : je demande une place pour les prochaines.",
                    Next("Faisons une place aux prochaines visites.", 3), Next("Quel mécanisme est bloqué ?", 4)),
                Page("La caméra de surveillance verrouille le générateur. Utilise OVERRIDE sur elle, puis PATCH sur le générateur de la tour. Je peux entretenir le feu après ton intervention. Reviens me voir quand il sera lancé : j'aimerais entendre quelqu'un prononcer cette nouvelle à voix haute.",
                    End("Je vais couper la caméra."), Next("Les réparations dureront-elles ?", 4)),
                Page("Ton outil donne une fenêtre de travail. C'est à nous d'en faire quelque chose. Si la sécurité revient avant que tu termines, neutralise-la de nouveau ; le système ne nous interdit pas d'apprendre de la première tentative. Je serai ici pour garder le feu dès qu'il répondra.",
                    End("Je prendrai les mécanismes dans le bon ordre."), End("Vous aurez bientôt une lumière à garder.")));
        }

        private static DialogueScript SideReturn(string npcId)
        {
            switch (npcId)
            {
                case "amir":
                    return Script("amir", "Amir", "side_lost_cargo",
                        Page("Une lettre. Et les trois traits sont encore là. ‘Si la mer nous sépare, garde une place pour moi à table.’ Je reconnais sa façon de faire des demandes qui ressemblent à des plaisanteries. Le registre pouvait retirer son nom ; il n'a pas su retirer cela.",
                            Next("Comment s'appelait-elle ?", 1), Next("Qu'allez-vous faire de la lettre ?", 2)),
                        Page("Salma. Je veux le dire sans baisser la voix : Salma. Il y a une minute, je craignais encore de l'avoir inventée. Je ne sais pas si nous pourrons la retrouver, mais au moins personne ne m'expliquera plus que je dois oublier pour être raisonnable.",
                            Next("Je me souviendrai de Salma aussi.", 2), End("Merci de me confier son nom.")),
                        Page("Je vais mettre la lettre près de notre table et en faire une copie pour Alma. La mémoire ne doit pas dépendre d'un seul coffre, ni d'un seul homme. Tu m'as rapporté une chose à tenir, Nico. Pour aujourd'hui, c'est assez pour recommencer.",
                            End("Gardez sa place à table."), End("À bientôt, Amir.")));
                case "lucie":
                    return Script("lucie", "Lucie", "side_radio",
                        Page("J'ai entendu le bulletin dans mon casque, puis dans la rue. Une femme s'est arrêtée et a demandé : ‘C'est aujourd'hui ?’ Imagine cela, Nico : une annonce qui appartient enfin à aujourd'hui. Nous avons une fréquence, et déjà trois réponses pour la place.",
                            Next("Qui a répondu ?", 1), Next("Quelle sera votre prochaine émission ?", 2)),
                        Page("Brune apporte ses outils. Jo veut installer une borne qui garde tous les scores. Orson a seulement dit qu'il pouvait transporter des pierres. Trois réponses très différentes, toutes utiles. Je comprends maintenant ce que le vieux bulletin nous prenait : le droit de nous coordonner.",
                            Next("La ville peut donc se réparer elle-même.", 2), End("Je viendrai voir la place.")),
                        Page("La prochaine émission sera une liste de questions auxquelles je ne connais pas la réponse. Cela devrait changer des annonces officielles. Merci d'avoir réparé l'antenne sans décider à ma place ce que j'avais le droit d'y dire.",
                            End("Gardez la fréquence ouverte."), End("À bientôt sur Radio Santa Riva.")));
                case "teo":
                    return Script("teo", "Téo", "side_fireflies",
                        Page("Ils sont tous là ! Même l'arbre sur l'antenne. Tu sais, je ne voulais pas devenir une mission importante. Je voulais juste qu'on puisse dessiner sans demander si le dessin est conforme au plan. Tu as compris la différence, je crois.",
                            Next("Qu'allez-vous dessiner maintenant ?", 1), Next("Comment allez-vous protéger les dessins ?", 2)),
                        Page("Un bateau avec des roues. Peut-être que personne n'en aura besoin. Peut-être que c'est idiot. Mais je veux voir à quoi ça ressemble avant de décider. Si les adultes gardent seulement les idées déjà utiles, qui inventera les prochaines ?",
                            Next("Je veux voir ce bateau quand il sera fini.", 2), End("Gardez vos idées, même les étranges.")),
                        Page("Alma va nous apprendre à copier les pages. Jo dit qu'il affichera une sélection dans la salle d'arcade. On fera plusieurs caches cette fois. Tu peux continuer vers le noyau : nous allons nous occuper de ce petit morceau de ville.",
                            End("Prenez soin de votre morceau de ville."), End("À bientôt, Téo.")));
                case "alma":
                    return Script("alma", "Alma", "side_archive",
                        Page("La page est lisible. Elle parle de Maël, réparateur de filets. Il avait demandé qu'on conserve son atelier même après son départ. Voilà donc le nom entre ces lignes. Je n'ai pas retrouvé une légende ; j'ai retrouvé quelqu'un qui travaillait de ses mains.",
                            Next("Pouvez-vous restaurer son histoire ?", 1), Next("Où conserverez-vous la page ?", 2)),
                        Page("Je peux commencer. Son nom aidera Brune à chercher ses outils et Mila à retrouver ses commandes. La mémoire revient souvent par les petites choses. Nous n'avons pas besoin de tout savoir pour lui rendre une place ; nous avons besoin de ne pas mentir sur ce qui manque encore.",
                            Next("Écrivons ce que nous savons réellement.", 2), End("Maël ne sera plus une ligne vide.")),
                        Page("Je garde l'original, et Lucie lira une copie à la radio. Une histoire en plusieurs mains résiste mieux qu'un registre parfait confié à une seule autorité. Merci de m'avoir rapporté la page plutôt qu'une explication pour accepter son absence.",
                            End("Merci de garder leurs noms."), End("À bientôt, Alma.")));
                case "gaston":
                    return Script("gaston", "Gaston", "side_watchtower",
                        Page("Elle répond. La lumière est chaude, et je l'avais oubliée si froide dans mon souvenir… Attends, on vient de faire signe depuis la route. Quelqu'un l'a vue. C'est tout ce que j'espérais : une présence qui répond à une autre.",
                            Next("Qui gardera le feu avec vous ?", 1), Next("Peut-on maintenir cette lumière ?", 2)),
                        Page("Je demanderai aux gens de passer à tour de rôle. Un poste occupé par un seul veilleur peut devenir une prison sans qu'il s'en aperçoive. J'ai longtemps pris ma fidélité pour une raison de ne rien changer. Tu viens de me donner une raison de transmettre.",
                            Next("La relève viendra.", 2), End("Vous avez guidé quelqu'un aujourd'hui.")),
                        Page("Le générateur a de nouveau une fenêtre stable. Je vais profiter de ce temps pour reprendre les connexions et ouvrir le poste aux autres. Va, Nico. Quand tu reviendras des hauteurs, regarde la tour : ce sera notre façon de te dire que la ville t'attend.",
                            End("Je chercherai cette lumière au retour."), End("À bientôt, veilleur.")));
                default: return null;
            }
        }

        private static DialogueScript Script(string id, string name, string quest, params DialoguePage[] pages)
        {
            return new DialogueScript { speakerId = id, speakerName = name, questId = quest, pages = new List<DialoguePage>(pages) };
        }

        private static DialoguePage Page(string text, params DialogueAnswer[] answers)
        { return new DialoguePage { text = text, answers = new List<DialogueAnswer>(answers) }; }

        private static DialogueAnswer Next(string label, int page)
        { return new DialogueAnswer { label = label, nextPage = page, completesConversation = false }; }

        private static DialogueAnswer End(string label, string flag = null)
        { return new DialogueAnswer { label = label, nextPage = -1, completesConversation = true, flag = flag }; }
    }
}
