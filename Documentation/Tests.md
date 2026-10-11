# Validation de RIVE 404

État du 11 octobre 2026. Les contrôles de domaine compilent du vrai C# et exécutent les règles ainsi que la sauvegarde sur disque. Ils ne remplacent pas l'import Unity, les tests du gameplay ou la mesure d'une build Windows.

Le projet cible désormais **6000.0.84f1** à la suite de l'alerte Unity Hub sur l'ancien pin 6000.0.40f1. Le contrôle structurel a été relancé après cette modification ; l'import et la compilation Unity restent non exécutés. Le journal `Validation/UnityAvailability.txt` décrit la tentative historique effectuée avec l'ancien pin et n'a pas été réécrit.

## Vérifications automatisées

| Contrôle | Commande | État |
|---|---|---|
| Installation .NET officielle avec SHA-512 avant extraction | `Tools/install_dotnet.sh` | SDK 8.0.425 installé et vérifié dans `/workspace/tools/dotnet` |
| Domaine et stockage réels, sans dépendance NuGet | `Tools/run_domain_checks.sh` | 26 tests réussis, zéro échec ; compilation C# et exécution réelles, code de sortie 0 |
| Analyse syntaxique C# 9 avec Roslyn officiel | `Tools/run_syntax_checks.sh` | 29 scripts analysés, zéro erreur ; contrôle final réussi (code de sortie 0), sans compilation Unity |
| GUID, scène, assemblages et versions épinglées | `python3 Tools/check_project.py` | 81 .meta, 5 assemblages, zéro erreur ; contrôle final réussi (code de sortie 0) |
| FBX et audio PCM, réimport réel Blender | `blender -b --python Blender/Tools/validate_assets.py` | 16 FBX et 14 WAV validés avec Blender 4.3.2 ; import Unity non exécuté |
| Sources Blender éditables et modification conservée après export | `blender -b --python Blender/Tools/validate_editable_sources.py` | 16 sources réouvertes ; modification temporaire de Nico puis export/réimport FBX réussis |
| Tests Unity EditMode (campagne et JsonUtility/disque) | `Tools/unity_checks.sh editmode` | Non exécutés : éditeur Unity indisponible |
| Tests Unity PlayMode (Bootstrap, monde, reprise, boss) | `Tools/unity_checks.sh playmode` | Non exécutés : éditeur Unity indisponible |
| Compilation et build autonome Windows x64 | `Tools/unity_checks.sh windows` | Non exécutées : éditeur, licence et module Windows requis |

Le SDK provient de `https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.425/dotnet-sdk-8.0.425-linux-x64.tar.gz`. Son SHA-512 est celui publié dans les métadonnées Microsoft .NET 8 ; TLS et vérification d'intégrité restent activés. Les sources NuGet sont volontairement vides pour les contrôles sans dépendances. Les sorties binaires des outils et les résultats de build sont exclus des sources selon le `.gitignore` du projet. Les comptes rendus finaux sont conservés dans [Validation/Domain.txt](Validation/Domain.txt), [Validation/Syntax.txt](Validation/Syntax.txt) et [Validation/Structure.txt](Validation/Structure.txt). Les rapports Blender détaillés sont [validation_report.json](../Blender/validation_report.json) et [editable_sources_report.json](../Blender/editable_sources_report.json).

Les 26 contrôles de domaine couvrent le catalogue 9+5, l'achèvement de la campagne, les événements sans rapport, les combats et collectes anticipés, les conversations successives, les récompenses idempotentes, la reprise des objectifs, la migration et le checkpoint sous le sol, les graphes de dialogues des 12 PNJ et leurs variantes, les données corrompues, l'énergie, les prérequis des fins, les gardes du sacrifice et de la collecte des fragments, et les définitions invalides. Les tests du stockage exécutent une sauvegarde réelle, un chargement réel, la conservation UTF-8, le secours après corruption, le rejet d'un checksum incorrect et la préservation de l'état précédent après tentative de sauvegarde incompatible.

## Préparation sur une machine Unity

1. Installer **Unity 6000.0.84f1** via Unity Hub, avec **Windows Build Support (Mono)** et une licence Unity activée.
2. Ouvrir `UnityProject`. Autoriser le téléchargement officiel des packages épinglés. Attendre la fin de l'import sans erreurs de console.
3. Exécuter **RIVE 404 > 1. Préparer le projet**. Le menu produit les vrais assets URP et renderer via les types du package, initialise les ressources et Global Settings URP, prépare les catalogues `ScriptableObject`, inclut les shaders nécessaires aux objets créés à l'exécution et inscrit Bootstrap dans les scènes de build.
4. Confirmer **Project Settings > Player > Active Input Handling : Input System Package (New)**. La valeur est déjà enregistrée dans `ProjectSettings.asset`.
5. Ouvrir `Assets/_RIVE404/Scenes/Bootstrap.unity`, lancer Play, puis une nouvelle partie.
6. Exécuter EditMode et PlayMode dans **Window > General > Test Runner**. Conserver les XML de résultats et le journal Unity.
7. Exécuter **RIVE 404 > 3. Construire Windows x64**. Livrer tout le dossier `Builds/Windows`, avec les données de l'exécutable.

Pour le mode batch Linux, définir `UNITY_EDITOR` vers l'exécutable officiel Unity. Les commandes `Tools/unity_checks.sh prepare|editmode|playmode|windows` conservent les journaux et résultats sous `Builds/TestResults`. Les suites EditMode et PlayMode appellent d'abord la préparation afin de tester le projet URP configuré.

L'accès à `packages.unity.com` renvoie actuellement **403 CONNECT** dans cette machine cloud. Aucune résolution complète des packages, compilation Unity ou activation de licence n'a été démontrée. `Packages/manifest.json` épingle les versions demandées ; **`packages-lock.json` doit être généré par le vrai Package Manager lors du premier import réussi puis conservé dans Git**. Un fichier de résolution n'est pas fabriqué sans métadonnées officielles. Les scripts batch échouent explicitement si l'éditeur manque.

## Recette Windows à terminer

| Exigence | Manipulation | Résultat attendu |
|---|---|---|
| Compilation/import | Projet ouvert sur Unity exact, console et Test Runner | Zéro référence manquante ; tests réussis |
| Hors connexion | Couper le réseau après distribution, lancer l'exécutable | Menu et campagne accessibles sans Unity installé |
| Commandes | AZERTY puis QWERTY, caméra, saut, sprint, accroupissement, esquive Alt, combat léger/fort | Contrôle cohérent, collision et invite lisible ; instructions de touche actualisées |
| Debug Tool | SCAN, PATCH de passerelle, OVERRIDE barrière/robot | Portée et coûts appliqués ; expiration observée ; réutilisation possible |
| Monde | Parcourir les trois quartiers et les six intérieurs ; relever la surface réellement accessible | Pas de retour au menu ; vérifier la cible 1 km² en excluant mer et obstacles |
| Campagne | Terminer les neuf missions par actions réelles et relever chaque durée | Aucune progression par attente seule ; aucun blocage après combat/collecte anticipés |
| Secondaires | Activer et terminer les cinq missions, y compris avec collecte/combat antérieurs | Retour PNJ et récompenses corrects, aucune duplication |
| Défaite | Subir une défaite après avoir franchi un checkpoint | Reprise au checkpoint ; quêtes conservées ; courte invulnérabilité |
| Sauvegarde | F5, fermer l'application, relancer Continuer | Position, chapitre, flags et progression conservés |
| Corruption | Copier le dossier de sauvegarde, corrompre seulement le fichier principal | Message explicite ; copie de secours utilisable |
| Boss/fins | Affronter le boss avant/après OVERRIDE du noyau, choisir chacune des fins dans deux parties | Bouclier respecté, phases visibles ; fins accessibles avec trois fragments et victoire |
| Pipeline Blender | Modifier une source `.blend`, réexporter FBX, réimporter puis reconstruire | Dimensions, matériaux URP et animation vérifiés dans Unity |
| Français/menus | Journal, carte, dialogues, paramètres, crédits et fins | Textes lisibles, aucun objectif manquant ; quartiers non visités masqués, objectif principal repérable |
| Accessibilité | Volume nul, profils bas/standard/élevé, VSync, résolution et sensibilité | Signaux importants restent compréhensibles ; paramètres conservés |

La durée **180 minutes** et les **60 FPS en 1080p sur Ryzen 5 3600/RX 590/16 Go** sont des cibles non mesurées. Relever en build les temps CPU/GPU, mémoire, minimum et médiane FPS, notamment aux transitions, en combat et au boss. Un playtest chronométré doit distinguer temps de jeu utile, attente, trajet et blocage. Rééquilibrer après ces mesures ; aucune durée ou performance n'est certifiée par les seuls tests de domaine.

## Source des contrôles API éditeur

Une revue des sources publiques officielles a confirmé les signatures de `GraphicsSettings.GetGraphicsSettings()`, `PlayerSettings.SetScriptingBackend(...)` et du `ModelImporter` dans [UnityCsReference 6000.0](https://github.com/Unity-Technologies/UnityCsReference/tree/6000.0). Les factories du renderer, `UniversalRenderPipelineAsset.Create(...)`, `ResourceReloader` et `UniversalRenderPipelineGlobalSettings.Ensure(...)` ont été inspectées dans [Graphics 6000.0/staging](https://github.com/Unity-Technologies/Graphics/tree/6000.0/staging). Cette branche affiche actuellement URP 17.0.4 ; elle aide à vérifier les API de la famille 17.0, sans prouver l'import et la compilation du pin 17.0.3. Le préparateur vérifie explicitement les signatures internes avant invocation et échoue avec un message si le package ne correspond pas.
