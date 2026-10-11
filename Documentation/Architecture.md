# Architecture de l'implémentation

## Responsabilités

- `Rive404.Domain` : état sérialisable, campagne, progression, graphes de dialogues, énergie, migration et prérequis narratifs. Assembly sans références Unity, compilée et testée avec .NET 8.
- `Rive404.Runtime` : amorçage, session, interaction, Debug Tool, fichiers de sauvegarde, conversion des matériaux et audio.
- `Rive404.Player` : CharacterController unique, caméra avec collision, attaques, esquive, IA, NavMesh et animations FBX par Playables.
- `Rive404.World` : construction déterministe du terrain et du contenu, activation des décors par distance, checkpoints et déplacement de Sillage.
- `Rive404.UI` : interface française, carte, journal, dialogues, menus et préférences persistantes.
- `Rive404.Editor` : préparation URP, catalogues, import des modèles et build Windows.

Runtime, Player, World et UI partagent l'assembly `Rive404.Game` ; cela évite les références circulaires d'assemblies. Domain et Editor restent séparés. Les règles métier ne dépendent pas des composants de scène.

## Déroulement d'une session

Bootstrap contient le composant GameRuntime. Il installe caméra, éclairage, audio et UI. Nouvelle partie ou Continuer initialise GameState et QuestEngine, construit le monde puis le joueur. Chaque interaction enregistre un événement stable ; le moteur avance uniquement les objectifs compatibles et conserve les événements persistants pour éviter les blocages après une collecte ou un combat anticipé.

Les conversations exigent une action fraîche et ne sont pas consommées pour de futures missions. Les fragments, l'ancre de Sillage, le noyau et les fins sont protégés par StoryRules. Les choix de fin sont accessibles après la campagne et la victoire requises.

## Données et édition

Le catalogue C# contient neuf missions principales et cinq secondaires. Le menu Préparer génère `Resources/Campaign.asset` et `DebugToolSettings.asset`, que l'éditeur peut modifier. Il conserve ces modifications lors des préparations suivantes. Les dialogues sont actuellement des données C# dans DialogueCatalog ; leur texte et leurs branches restent éditables.

Les sources Blender sont distinctes des exports FBX. Les quatre modèles riggés utilisent des animations Generic Idle/Walk, sans dépendre d'un AnimatorController absent. Le contrôleur dispose aussi d'un mouvement procédural visuel de secours. Les matériaux importés sont convertis en URP, avec émission pour les matériaux de signal.

## Monde et persistance

Le terrain de base couvre 1000 × 1000 m bruts. Les bâtiments et obstacles réduisent la surface réellement accessible ; celle-ci reste à relever. Les décors sont regroupés en cellules et activés par distance avec marge et hystérésis. Les objets de quête restent disponibles et les ennemis morts sont restaurés depuis les flags.

Cette première implémentation utilise Bootstrap et des objets générés à l'exécution, avec activation par cellules. Le chargement de scènes additif et les scènes de quartier éditables du cahier des charges restent un travail d'évolution à valider ; les cellules ne prouvent pas une absence de gel de transition. Les six intérieurs actuels sont des espaces ouverts traversables.

Les effets PATCH/OVERRIDE ont une durée réelle. Les flags historiques gardent la preuve d'un objectif, mais le bouclier du boss dépend de l'effet OVERRIDE encore actif. Les passages temporaires disposent d'une route de secours.

## Sauvegarde

SaveStore est indépendant de Unity : le runtime injecte JsonUtility, les tests injectent le sérialiseur .NET. Le fichier JSON contient une enveloppe, une charge utile versionnée et son SHA-256. L'écriture passe par un fichier temporaire vidé sur disque, puis un remplacement avec copie de secours. Une sauvegarde corrompue ne remplace pas le dernier secours valide. Les versions futures sont refusées ; la migration du prototype v0 est testée.

La position, le checkpoint, la santé, l'énergie, les améliorations, le temps de jeu, les flags, les missions et la fin sont conservés. Les effets temporaires et les processus ne sont pas sauvegardés. L'autosauvegarde intervient aux événements importants et toutes les soixante secondes de jeu ; F5 et les bornes permettent une sauvegarde explicite.

## Validation

Les tests .NET exécutent les vraies règles et les fichiers de sauvegarde. Roslyn vérifie la syntaxe de tous les C#. Les suites Unity testent l'import/compilation, JsonUtility et des interactions de session/boss ; elles ne sont pas exécutables ici sans l'éditeur. Aucun test de règles ne remplace les parcours de jeu, le débogage dans les IDE, la build Windows et le profiling final.
