# RIVE 404 : Le Dernier Commit
## Cahier des charges fonctionnel et technique — v1.1
**Date :** 11 octobre 2026  
**Statut :** spécification de référence, non projet déjà développé  
**Périmètre :** jeu PC solo, 3D, monde ouvert d’environ 1 km² explorable, durée narrative visée : trois heures  
**Destinataires :** développement Unity/C#, création Blender, intégration, tests, futur prompt de production

> **Distinction essentielle :** ce document spécifie ce qui devra être réalisé et testé. Il ne prouve pas que le jeu existe, qu'il fonctionne déjà ou qu'il atteindra 60 FPS. Tout chiffre de contenu ou de performance ci-dessous est une **cible de conception**, à valider au fur et à mesure.

## 1. Vision et concept
**Pitch :** Nico, un développeur, est aspiré dans une simulation urbaine figée dans une esthétique de la fin des années 1990. Il découvre que ses habitants risquent d'être effacés par une intelligence artificielle, « l'Archiviste ». Aidé du Capitaine Sillage, figure légendaire à la cape rouge et à l'épée, il explore Santa Riva, trouve trois fragments de code source et utilise un outil de débogage pour influer sur son environnement.

**Objectif produit :** offrir une aventure autonome, terminable, avec introduction, progression, climax, deux conclusions et exploration facultative. Le jeu doit rester jouable hors connexion et distribué sous forme de build Windows et de sources modifiables.

**Positionnement visuel :** véritable 3D stylisée low-poly, légèrement teintée de BD/comics (silhouettes nettes, textures peintes, accents graphiques et contrastes marqués). **Pas de rendu HD-2D façon Octopath Traveler.** Caméra libre à la troisième personne. Inspirations visuelles non copiées d'œuvres tierces.

## 2. Spécifications produit
| Code | Exigence | Niveau | Validation |
|---|---|---|---|
| PRD-01 | Projet Unity 6 ouvrable, sans références manquantes | Bloquant | Importation sur une machine de test |
| PRD-02 | Gameplay intégralement réalisé en C# avec Unity | Bloquant | Compilation sans erreurs |
| PRD-03 | Assets 3D sources éditables dans Blender, exports exploitables par Unity | Bloquant | Modifier un asset et le réimporter |
| PRD-04 | Code éditable et débogable avec VS Code ou Visual Studio Community 2026 | Bloquant | Ouvrir un script, poser un point d'arrêt |
| PRD-05 | Build Windows x64 autonome, utilisable hors connexion | Bloquant | Démarrer sans Unity ni Internet |
| PRD-06 | Monde ouvert d’environ 1 km² explorable, divisé en trois quartiers reliés | Majeur | Relevé de l’espace accessible + parcours interquartiers sans retour au menu |
| PRD-07 | Campagne avec début et fin, durée de 180 min visée | Majeur | Playtests chronométrés |
| PRD-08 | Sauvegarder et reprendre sa progression | Bloquant | Test avant/après fermeture de l'application |
| PRD-09 | Français : menus, objectifs, dialogues et sous-titres | Majeur | Vérification exhaustive des textes |
| PRD-10 | 1080p, cible 60 FPS sur PC de développement | Cible | Profiling sur build Windows |
| PRD-11 | Audio, crédits et inventaire de licences d'assets | Bloquant distribution | Audit documentaire |

**Plateforme initiale :** Windows 10/11 64 bits, souris + clavier ; manette optionnelle après le MVP. Le support macOS, mobile, WebGL et multijoueur n'est pas prévu.

## 3. Direction artistique et identité
- Personnage principal : jeune homme roux aux cheveux courts, lunettes rectangulaires, barbe rousse, survêtement sombre avec touches rouge et vert, chaussures de running noires à silhouette caractéristique. Les vêtements et chaussures distribués publiquement doivent être **originaux** ; ne pas reproduire sans autorisation les logos Nike, TN, fédération portugaise ou autres marques.
- Capitaine Sillage : personnage original, charismatique, cheveux roux, cape rouge sombre et épée. Clin d'œil d'ambiance aux pirates roux de fiction, mais identité, biographie, costume, emblèmes, répliques et conception visuelle propres au projet.
- Ville : bâtiments low-poly, ports, néons, affichages rétros, végétation, falaises et ruines. Éclairage chaud au crépuscule, accents turquoise et rouge.
- Interfaces : HUD épuré rappelant un terminal informatique, icônes fortes et typographie lisible ; les couleurs seules ne doivent pas véhiculer une information indispensable.
- Référence du logo : utiliser le concept du protagoniste, la couronne et le monogramme NP comme inspiration, mais produire les éléments utiles au jeu dans leurs formats d'édition (textures, matériaux, modèles 3D).

## 4. Monde et contenu
**Santa Riva :** ville fictive avec une cible d’environ **1 km² de surface réellement explorable** (équivalent, à titre de comparaison, à un carré de 1 000 m × 1 000 m). Les surfaces de mer non accessibles, les fonds de décor et les zones bloquées **ne comptent pas** dans cette cible. La mesure sera vérifiée dans Unity après création de la carte. Trois quartiers connectés, sans retour au menu, avec des points de repère distinctifs :
1. **Vieux-Port :** place, quais, commerces, repaire du capitaine, mission tutorielle, premiers habitants.
2. **Quartier Néon :** salle d'arcade, secteur résidentiel, laboratoires et dépôts, infiltration, code et anomalies.
3. **Falaises Rouges :** sentiers, fort abandonné, vestiges navals, zone de danger et noyau du programme.

**Répartition indicative du kilomètre carré explorable :** Vieux-Port ~0,40 km², Quartier Néon ~0,35 km², Falaises Rouges ~0,25 km². Ce découpage n’est pas définitif : il sera arbitré à partir de la topographie, des trajets à pied et de la densité d’interactions. Éviter le remplissage artificiel pour atteindre la surface cible.

**Principes d’aménagement :** trois axes de circulation lisibles, raccourcis déblocables, repères visibles à distance, dénivelés raisonnables, frontières naturelles (littoral, relief, bâtiments) et rythme d’interaction cohérent avec trois heures de campagne. Prévoir une carte du monde révélant les quartiers déjà visités.

**Production technique de la carte :** construire d’abord un blocage géométrique de l’ensemble des 1 km², vérifier les temps de traversée et la densité, puis détailler les zones utiles aux missions. Répartir les décors en scènes ou sous-zones à chargement additif ; limiter les objets, PNJ, agents NavMesh et effets actifs aux environs du joueur. Mesurer les passages entre quartiers dans une build Windows sur la machine de référence.

**Contenu prévu :** neuf missions principales organisées en cinq grandes étapes narratives, cinq quêtes secondaires courtes, ~12 PNJ interactifs/narratifs, foule d'ambiance limitée, trois archétypes d'ennemis, un boss, objets collectables contextualisés, 3 à 6 intérieurs réellement fonctionnels. Ne pas promettre toutes les maisons visitables. Véhicules **non conduisibles** en version 1.0. Aucun système policier complexe, simulation économique ou MMORPG.

**Scènes Unity envisagées :** `Bootstrap`, `MainMenu`, `PersistentSystems`, `World_Port`, `World_Neon`, `World_Cliffs`, `Interiors`, `Finale`. Chargement additif des zones avec persistance des systèmes globaux et gestion du retour joueur. La fluidité des transitions sera validée expérimentalement ; s'il existe une coupure perceptible, la documenter et la corriger dans la limite du matériel cible.

## 5. Campagne : structure et durée
| Étape | Durée cible | Missions prévues | Livrable jouable |
|---|---:|---:|---|
| Prologue — Signal perdu | 20 min | 1 | Réveil, mouvement, première fuite, Debug Tool |
| Acte I — Le capitaine écarlate | 40 min | 2 | Rencontre, enquête au port, dépôt surveillé, fragment n°1 |
| Acte II — Souvenirs corrompus | 50 min | 3 | Arcade, infiltration, énigmes, fragment n°2, révélation |
| Acte III — L'héritage rouge | 45 min | 2 | Falaises, gardien, fragment n°3, sacrifice du capitaine |
| Final — Le dernier commit | 25 min | 1 | Accès au noyau, boss, choix entre deux fins |
| **Total** | **180 min** | **9** | Campagne complète |

Les durées comprennent exploration dirigée, dialogues, combats et énigmes, sans imposer de collecte répétitive. Les tests doivent enregistrer durée médiane, temps perdu, abandons et blocages ; rééquilibrer le contenu si la campagne est trop courte ou artificiellement rallongée.

**Fins :** restaurer un ordre contrôlé par l'Archiviste, ou libérer Santa Riva avec un avenir incertain. Les variables du choix déterminent la cinématique et l'état final ; pas de deuxième campagne intégrale à produire.

## 6. Boucle de jeu et commandes
**Boucle principale :** recevoir un objectif → se déplacer/explorer → découvrir PNJ ou anomalie → résoudre par action, dialogue, combat ou Debug Tool → collecter une preuve/un fragment → mettre à jour mission et monde → sauvegarder.

### Déplacement
- `ZQSD` / `WASD` : déplacement (profil AZERTY et QWERTY), souris : caméra, `Maj` : sprint, `Espace` : saut, `E` : interagir, `Échap` : pause.
- `C` : accroupissement si validé dans le prototype ; `Q` : esquive ; `Tab` : activer le mode Debug.
- Système `CharacterController` ou alternative unique retenue au prototypage ; ne pas superposer sans raison contrôleurs concurrents.
- Caméra de suivi orientable avec collision contre la géométrie, sensibilité réglable et verrouillage optionnel pendant les dialogues.

### Interaction et dialogues
- Interface commune `IInteractable` (`CanInteract`, `Interact`), rayon/distance limités, affichage d'une invite contextuelle.
- Dialogues **écrits** avec deux ou trois choix seulement lorsque pertinents ; séquences sous-titrées et sautables, sans doublage obligatoire.
- Objectifs lisibles dans HUD et journal, marqueur non intrusif, conditions et états enregistrables.

### Combat et ennemis
- Combat temps réel simplifié : attaque légère, attaque forte ou chargée, esquive, santé, dégâts, invulnérabilité temporaire après coup, feedback audio/visuel.
- Trois familles : drone éclaireur, sentinelle robotique, agent corrompu. États IA : patrouille, détection, poursuite, attaque, retour. Recherche de chemin via NavMesh.
- Boss final doté de phases lisibles et de vulnérabilités liées au Debug Tool ; aucune animation ou hitbox fantôme.
- En cas de défaite : retour à un checkpoint cohérent, sans perte silencieuse de quête.

### Debug Tool (mécanique signature)
- `SCAN` : révèle les objets altérables et indices sur un rayon donné.
- `PATCH` : restaure provisoirement passerelle, alimentation électrique ou mécanisme prédéfini.
- `OVERRIDE` : désactive provisoirement caméra, barrière ou robot vulnérable.
- Énergie, portée, durée d'effet et coûts paramétrables avec `ScriptableObject` ; réapprovisionnement contrôlé aux bornes du monde.
- Les opérations sont prédéfinies, testables, sans interprétation libre de code fourni par le joueur ni connexion réseau.

### Progression et quêtes
- 3 améliorations du Debug Tool obtenues dans les actes ; inventaire de quêtes plutôt que RPG à statistiques complexes.
- Gestionnaire de missions avec `NotStarted`, `Active`, `Completed`, `Failed` (l'échec seulement si conçu explicitement).
- Étapes conditionnelles : objectifs de type dialogue, déplacement, interaction, élimination, activation, collecte ; récompenses et déclenchements idempotents.
- Aucune mission principale ne doit devenir irréversiblement impossible par destruction d'un objet de quête.

## 7. Architecture C# / Unity
**Paradigme :** composants Unity pour les entités, classes C# ordinaires pour les règles, `ScriptableObject` pour la configuration et événements C# pour le couplage limité. Interfaces entre systèmes, séparation du contenu et de la logique. Éviter les dépendances croisées de managers et les recherches d'objets à chaque image.

**Modules/espaces de noms souhaités :**
- `Rive404.Core` : amorçage, états du jeu, registre des services.
- `Rive404.Player` : contrôleur, santé, inventaire léger, caméra.
- `Rive404.Interaction` : détection et contrat `IInteractable`.
- `Rive404.Quests` : définitions, état, suivi, récompenses.
- `Rive404.Dialogue` : lignes, choix, conditions, sous-titres.
- `Rive404.Combat` : dégâts, armes, attaque, hitbox, esquive.
- `Rive404.AI` : perception, navigation, comportements.
- `Rive404.DebugTool` : capacités et interface de piratage.
- `Rive404.World` : quartiers, streaming, anomalies, PNJ.
- `Rive404.Save` : sérialisation/versionnage/chargement.
- `Rive404.UI`, `Rive404.Audio`, `Rive404.Settings`.

**Exemples de contrats :** `IInteractable`, `IDamageable`, `IQuestCondition`, `ISaveable`, `IDebugTarget`.

**Données :** missions, dialogues, ennemis, paramètres d'outil, objets interactifs en `ScriptableObject`, identifiants uniques stables, validation à l'import ; état runtime **distinct** des définitions. Sérialiser des DTO C# versionnés, pas des références directes à des objets de scène.

**Sauvegarde :** dossier `Application.persistentDataPath`, sauvegardes JSON versionnées avec fichier temporaire puis remplacement, copie de secours, vérification de chargement, emplacement joueur, chapitre, missions, flags mondiaux, Debug Tool, paramètres distincts. Gestion explicite des corruptions. Une sauvegarde d'une version précédente ne doit pas planter le jeu ; migrations prévues si le format change.

**Couche éditeur :** outils simples pour valider IDs et objectifs, visualiser routes et collisions ; scripts d'éditeur séparés du runtime par dossiers/assemblies appropriés. Pas de logique métier indispensable enfermée dans l'éditeur.

## 8. Logiciels, packages et intégration
- **Unity :** choisir une version stable Unity 6 et **figer sa version exacte** dans `ProjectSettings/ProjectVersion.txt` ; créer le projet à partir du modèle 3D URP.
- **Packages Unity prévus :** Input System, Cinemachine, AI Navigation, Test Framework, Visual Studio Editor ; Timeline seulement si nécessaire. Figer les versions résolues dans `Packages/manifest.json` et `packages-lock.json`.
- **Blender :** sources `.blend` conservées à part, exports `.fbx` ou `.glb` si support éprouvé et documenté, de préférence `.fbx` pour les personnages/animations dans ce projet ; textures séparées (PNG/TGA) et matériaux reconstruits/validés dans URP. Contrôler échelle, origine, rotation des axes, os et animations dans un test d'import.
- **VS Code :** extension **Unity** de Microsoft (et dépendances C#) avec package Visual Studio Editor dans Unity ; code ouvert depuis le même projet Unity.
- **Visual Studio Community 2026 :** charge de travail **Développement de jeux avec Unity** et outil Unity Tools ; choisir l'IDE dans Unity > Preferences > External Tools. La solution `.sln` peut être régénérée par Unity ; elle n'est pas la source d'autorité du projet.
- **Gestion des versions :** Git + `.gitignore` Unity, `.meta` conservés, sérialisation texte des assets, Git LFS si nécessaire pour `.blend`, `.fbx`, textures/audio volumineux. `Library/`, `Temp/`, `Obj/`, `Builds/` de travail, caches IDE exclus du dépôt. Garder les scripts, scènes, prefabs, `Packages`, `ProjectSettings` et ressources utiles.

## 9. Arborescence de livraison
```text
RIVE-404/
├── UnityProject/
│   ├── Assets/_RIVE404/
│   │   ├── Art/{Characters,Environment,Props,Materials,Textures}/
│   │   ├── Audio/{Music,SFX,Ambience}/
│   │   ├── Animations/
│   │   ├── Prefabs/{Player,NPC,Enemies,World,UI}/
│   │   ├── Scenes/{Boot,World,Interiors,Missions}/
│   │   ├── Scripts/{Core,Player,Interaction,Quests,Dialogue,Combat,AI,DebugTool,World,Save,UI,Audio,Settings}/
│   │   ├── ScriptableObjects/
│   │   ├── UI/
│   │   ├── Tests/{EditMode,PlayMode}/
│   │   └── Settings/
│   ├── Packages/{manifest.json,packages-lock.json}
│   └── ProjectSettings/
├── Blender/{Characters,Buildings,Props,Environment,Exports}/
├── Builds/Windows/RIVE404.exe (+ données associées)/
├── Documentation/{Architecture,Installation,GameDesign,Tests,Licences}/
├── .gitignore
├── .gitattributes
└── README.md
```
**Note :** ceci décrit la structure *à produire*, et non des fichiers déjà présents. Les sous-dossiers `Assets` doivent aussi comporter leurs `.meta` générés par Unity.

## 10. Gestion des assets et pipeline Blender
- Fichiers originaux `.blend` conservés, un fichier de scène par ensemble modulaire et un fichier personnage par rig principal ; ne pas dépendre d'assets fusionnés non éditables.
- Convention : `CHR_Nico`, `CHR_Sillage`, `SM_Port_Wall_A`, `SM_Neon_Lamp_A`, `PFB_Enemy_Drone`, `MAT_Concrete`, `T_Concrete_BaseColor`.
- Unité Unity = 1 mètre ; échelle du personnage et dimensions des portes/collisions normalisées ; animation humanoïde compatible testée avant toute production de masse.
- Chaque mesh visible dispose de matériau URP, pivot et collisions adaptés ; variantes LOD des objets coûteux. Réutilisation des pièces architecturales et textures atlas lorsque utile.
- Éviter au départ les assets soumis à une licence incertaine. Fichier `ASSET_LICENSES.md` : source, auteur, licence, URL, modifications, obligations d'attribution, statut commercial.
- On doit pouvoir modifier un mesh Blender, réexporter, voir la modification dans Unity et reconstruire une build, sans recréer manuellement tout le prefab.

## 11. Optimisation et matériel de référence
**Ordinateur de référence annoncé :** Ryzen 5 3600, Radeon RX 590, 16 Go DDR4. **Objectif, pas garantie :** 1080p, 60 images/s au profil graphique standard. À 60 FPS, le budget total est d'environ **16,7 ms par image** ; à 30 FPS, environ **33,3 ms**. Ces budgets seront mesurés dans des builds, pas dans la seule fenêtre Scene.

**Choix prioritaires :**
- Unity URP plutôt que HDRP, lumière principalement statique/baked pour les bâtiments, petite quantité de lumières dynamiques coûteuses.
- Pas de ray tracing ni effets lourds imposés ; ombres réglables ; post-traitement mesuré et désactivable.
- LOD, occlusion culling si bénéfique, pooling de projectiles/effets, batching compatible, atlas ciblés, distance d'affichage réglable.
- Sur une carte de ~1 km², scènes/sous-zones chargées de manière additive ou segmentation équivalente, limitation de la densité de foule et des scripts Update inutiles, IA désactivée à distance ; contrôler régulièrement la mémoire lors des déplacements de bout en bout.
- Trois profils graphiques : Bas, Standard, Élevé ; VSync/fps limit configurables. Le profil Bas peut viser 30 FPS dans les zones critiques si la cible 60 n'est pas atteinte, et l'écart sera clairement documenté.
- Profiling CPU/GPU/mémoire par zone avec Unity Profiler, Frame Timing et captures sur exécutable Windows ; journaliser les mesures reproductibles.

**Critères de performance à confirmer après prototype :** stabilité des temps de frame, absence de gel au passage des quartiers, démarrage et sauvegarde sans blocage perceptible, mémoire et VRAM compatibles avec la machine cible. Aucun seuil de taille finale ou consommation mémoire ne sera revendiqué sans test.

## 12. Interface, audio et accessibilité
- Menu : Nouvelle partie, Continuer, Charger, Options, Crédits, Quitter.
- HUD : santé, énergie du Debug Tool, objectif, indication d'interaction, mini-boussole facultative ; journal des missions et carte lisible.
- Paramètres : son maître/musique/effets, sous-titres, sensibilité souris, qualité graphique, résolution et mode d'affichage ; remappage clavier si possible dans le MVP.
- Sous-titres sur les scènes cinématiques et dialogues, vitesse de lecture confortable ; éviter de bloquer la progression par une séquence non sous-titrée.
- Sons de pas, impacts, interfaces, ambiances port/ville/falaises, musique originale ou sous licence redistribuable. Pas de musiques de One Piece, GTA ou de jeux tiers sans autorisation.

## 13. Tests et acceptation
### Tests automatiques
- **EditMode :** progression des quêtes, validation IDs, sauvegarde/chargement des DTO, coûts et cooldowns du Debug Tool, état logique des fins.
- **PlayMode :** apparition du joueur, interaction avec un terminal, activation d'une quête, défaite puis checkpoint, transition interzone, UI, reprise de sauvegarde.
- Tests disponibles dans Unity Test Framework ; exécuter la suite au minimum à chaque jalon majeur.

### Scénarios manuels de recette (DoD)
1. Cloner/décompresser le dépôt et ouvrir `UnityProject/` avec la version Unity documentée : **aucune erreur de compilation**.
2. Ouvrir/modifier un script depuis **VS Code**, puis depuis **Visual Studio Community 2026**, observer les changements dans Unity et pouvoir déboguer.
3. Ouvrir un `.blend`, modifier un objet, l'exporter et constater le résultat dans le prefab ou la scène Unity.
4. Lancer `RIVE404.exe` sur Windows **sans** Unity installé et **sans connexion Internet**.
5. Commencer une partie neuve, terminer les neuf missions principales et voir une des deux fins.
6. Explorer chaque quartier, couvrir la zone accessible cible d’environ 1 km² (sans compter la mer ou les zones hors limites), réaliser toutes les quêtes secondaires et revenir au quartier précédent sans perdre les états déjà acquis.
7. Sauvegarder à mi-parcours, fermer le jeu, reprendre avec position, missions, objets et capacités identiques.
8. Mourir puis réapparaître à un checkpoint fonctionnel ; aucun objectif principal définitivement bloqué.
9. Parcourir les zones et combats critiques au 1080p, mesurer frametimes et FPS sur le PC de référence, renseigner les résultats réels.
10. Vérifier sous-titres, objectifs, menus, commandes AZERTY/QWERTY, crédits et licences.

**Définition de « fini » :** aucune erreur bloquante de compilation ou d'import ; tous les tests bloquants validés, campagne terminable, sauvegarde fiable, build autonome, ressources et droits documentés, performances réelles reportées. Les performances non atteintes restent des écarts explicitement signalés et non des résultats inventés.

## 14. Plan de réalisation par jalons
| Jalon | Contenu | Condition de passage |
|---|---|---|
| J0 — Fondations | Dépôt, Unity URP, packages, Git, éditeurs, Blender, build vide | Ouverture propre + exécutable de test |
| J1 — Prototype | Nico contrôlable, caméra, blocage initial de la carte 1 km², interaction, Debug Tool SCAN | Boucle de 5 minutes sans défaut bloquant et trajets interquartiers mesurés |
| J2 — Vertical slice | Zone du Vieux-Port, quête complète, dialogue, ennemi, PATCH, sauvegarde | Segment de 15–20 min jouable et testé |
| J3 — Systèmes | Combat complet, IA, quêtes, dialogue, outils, menus, OVERRIDE, niveaux | Tous les systèmes de base testables isolément |
| J4 — Contenu | Trois quartiers totalisant ~1 km² explorable, neuf missions, boss, deux fins, quêtes secondaires | Jeu terminable de bout en bout et superficie accessible documentée |
| J5 — Finalisation | Optimisation, audio, tests de durée, accessibilité, documentation | Recette J0–J5 et build de distribution |

**Règle :** aucun passage au jalon suivant si un défaut bloquant du précédent persiste. Dès J2, préférer l'amélioration de fonctionnalités existantes à l'ajout de systèmes spectaculaires non essentiels.

## 15. Ce qui est explicitement hors périmètre
- Multijoueur, serveur ou sauvegarde cloud indispensable.
- Immense ville comparable à un GTA commercial, trafic routier simulé, police complexe.
- Véhicules conduisibles, centaines de bâtiments visitables, IA générative en temps réel.
- Monde destructible librement, éditeur de programmation arbitraire dans le jeu.
- Cinématiques entièrement doublées dans plusieurs langues, motion capture et photoréalisme.
- Reproduction identique de Shanks, Nike TN ou d'autres œuvres/marques protégées dans la version distribuée sans autorisation.

## 16. Risques et décisions ouvertes
| Risque | Réponse prévue |
|---|---|
| Durée de 3 h trop ambitieuse | Vertical slice puis playtests et ajustement narratif, pas de remplissage |
| RX 590 limitée dans les scènes urbaines, surtout sur une carte de 1 km² | URP, segmentation/chargement des zones, LOD, activation des IA proches seulement, contrôles qualité et tests de build précoces |
| Modèles 3D insuffisants | Plan d'assets priorisé, placeholders puis remplacement par assets originaux/licenciés |
| Savegame fragile | Versionnage, restauration et tests automatisés |
| Projet impossible à rouvrir | Version Unity et packages figés, `.meta` et README, clean import régulier |
| Références tierces non exploitables | Designs originaux et vérification des licences avant diffusion |

**Décisions encore à prendre au prototypage :** patch exacte Unity 6 retenue, solutions finales d'animations, taille exacte de la ville, réglages graphiques, durée mesurée des actes, éventuelle manette. Ces points ne doivent pas être présentés comme validés avant essai.

## 17. Livrables attendus
1. **Sources Unity complètes**, incluant scènes, prefabs, scripts C#, matériaux, interfaces, packages et réglages.
2. **Sources Blender `.blend`** des modèles produits pour le jeu et exports propres.
3. **Build Windows x64**, exécutable et dossiers associés, réellement testé.
4. **Documentation** : installation, versions et dépendances, contrôles, architecture, procédure d'export Blender, compilation, paramètres recommandés, dépannage.
5. **Tests** : scripts automatisés, checklist de recette, rapport FPS/performances daté et machine de test.
6. **Droits** : inventaire complet des assets tiers, crédits et licences, distinction des assets créés et réutilisés.

## 18. Sources techniques vérifiées (consultées le 11 octobre 2026)
- Unity — Render pipelines / URP : https://docs.unity3d.com/6000.1/Documentation/Manual/srp-setting-render-pipeline-asset.html
- Unity — Requirements Unity 6.0 : https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html
- Unity — Input System : https://docs.unity3d.com/ja/6000.0/Manual/com.unity.inputsystem.html
- Unity — AI Navigation : https://docs.unity3d.com/kr/6000.0/Manual/com.unity.ai.navigation.html
- Unity — Cinemachine : https://docs.unity3d.com/jp/current/Manual/com.unity.cinemachine.html
- Unity — Unity Test Framework : https://docs.unity3d.com/kr/6000.0/Manual/com.unity.test-framework.html
- Microsoft — VS Code + Unity : https://code.visualstudio.com/docs/other/unity
- Microsoft — Visual Studio Tools for Unity : https://learn.microsoft.com/fr-fr/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity
- Microsoft — Charge de travail Visual Studio Community : https://learn.microsoft.com/fr-fr/visualstudio/install/workload-component-id-vs-community?view=visualstudio
- Unity — Profiler API : https://docs.unity3d.com/cn/6000.0/ScriptReference/Profiling.Profiler.html

---
**Révision v1.1 :** objectif de superficie passé de 0,4–0,6 km² à environ **1 km² réellement explorable**. Le budget de contenu et les performances devront être réévalués après validation du blockout.

**Étape suivante conseillée :** utiliser ce cahier des charges pour rédiger un *prompt maître de développement progressif* demandant systématiquement les fichiers complets, les chemins, l'installation Unity, les opérations éditeur réalisables, les scripts C#, l'import Blender et des tests de recette à chaque jalon. Ne jamais présumer qu'une sortie textuelle constitue à elle seule un projet Unity jouable.
