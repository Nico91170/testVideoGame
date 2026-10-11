# RIVE 404 — Plan de réalisation v1

Date : 11 octobre 2026. Référence : `RIVE404_Cahier_des_charges_technique_v1_1.md`.

Ce plan organise le travail à réaliser. Il ne constitue ni un projet Unity, ni une build, ni une validation des objectifs du jeu.

## 1. Point de départ et objectif

L'inspection cloud du 11 octobre 2026 a montré que le dépôt `Nico91170/testVideoGame` est vide : aucun commit, aucun projet Unity et aucune branche distante `main` lors de la vérification GitHub. Blender 4.3.2 est disponible dans la machine cloud ; l'enregistrement et la réouverture d'un `.blend`, puis l'export et le réimport FBX d'un objet simple ont été vérifiés. Cela ne valide pas encore l'import dans Unity ni un personnage animé. Unity et les outils C# n'ont pas été trouvés dans le PATH et les emplacements d'installation Unity examinés.

La cible du cahier des charges reste un jeu solo Windows x64, Unity 6/C#, URP, avec sources Blender, neuf missions principales, cinq quêtes secondaires, deux fins et sauvegarde. Les trois heures de campagne, le kilomètre carré explorable et les 60 FPS à 1080p sont des objectifs à mesurer.

Organisation confirmée : réalisation en solo, en apprenant Unity/C#. Prévoir un apprentissage pratique intégré au projet et un recours possible à des assets réutilisables dont les licences sont documentées. Le rythme hebdomadaire reste à préciser ; les estimations ci-dessous prennent **10 heures par semaine** comme hypothèse de calcul.

Le premier objectif personnel est de produire et comprendre une petite build jouable. La progression mène ensuite à une tranche représentative, puis à la campagne prévue dans le cahier des charges.

## 2. Jalons et conditions de passage

| Jalon | Livrable principal | Condition de passage |
|---|---|---|
| A — Apprentissage pratique | Petits exercices Unity/C# et une scène de test | Savoir créer un objet, écrire un composant, utiliser un prefab et expliquer une interaction simple |
| J0 — Fondations | Projet URP versionné, chaîne C#/Blender, build Windows de test | Projet rouvrable, compilation propre, débogage C#, asset réimportable et exécutable lancé sur Windows |
| J1 — Prototype | Boucle de cinq minutes et blocage géométrique des trois quartiers | Déplacement, caméra, interaction, SCAN et premier combat utilisables ; parcours de la carte et première transition de zone mesurés |
| J2 — Tranche jouable | Une quête représentative du Vieux-Port, de 15–20 minutes visées | Dialogue, ennemi, PATCH, checkpoint, transition, sauvegarde et reprise après fermeture fonctionnent ensemble |
| J3 — Systèmes | Gameplay et outils de production stabilisés | Systèmes requis disponibles, tests pertinents exécutés, contenu validable sans nouveaux systèmes majeurs |
| J4 — Campagne | Parcours complet, puis contenu et habillage des trois quartiers | Neuf missions, boss et deux fins terminables ; quêtes secondaires et intérieurs intégrés ; surface et durée mesurées |
| J5 — Distribution | Sources, build Windows, documentation et rapport de recette | Recette bloquante passée, droits documentés, résultats de performance réels consignés |

Un défaut bloquant suspend le passage au jalon suivant. Les tests, les sauvegardes, l'optimisation et le suivi des licences accompagnent chaque jalon.

### A — Apprendre avec des exercices courts

Installer d'abord l'éditeur Unity retenu et les outils C# nécessaires aux exercices. La configuration reproductible et les preuves de J0 consolideront cette première installation.

1. Découvrir l'éditeur : scènes, GameObjects, composants, Transform, prefabs, Play Mode et console.
2. Apprendre les bases C# utilisées immédiatement : variables, conditions, boucles, méthodes, classes, listes et références. Comprendre le cycle de vie d'un composant Unity.
3. Réaliser trois exercices dans une scène séparée : déplacer un personnage, déclencher une interaction, collecter un objet qui met à jour un objectif.
4. Apprendre Git avec de petits commits, puis le débogueur en suivant une variable et un point d'arrêt.
5. Faire les premiers exercices Blender : déplacer et modifier un mesh, appliquer l'échelle, régler le pivot, attribuer un matériau et exporter en FBX.

Utiliser les ressources officielles de [Unity Learn](https://learn.unity.com/) et la [documentation C# Microsoft](https://learn.microsoft.com/fr-fr/dotnet/csharp/). Pour chaque notion, produire un exemple très court, le modifier et vérifier le comportement obtenu.

**Condition de passage :** être capable de modifier les exercices et de diagnostiquer une erreur simple de la console. L'objectif est de disposer des bases pour J0/J1 ; les interfaces, événements, ScriptableObjects et outils de test seront approfondis lorsqu'un système concret en aura besoin.

### J0 — Établir une chaîne de production reproductible

1. Choisir une version stable exacte de Unity 6 et vérifier sa compatibilité avec les packages et la machine utilisée. La figer dans `ProjectSettings/ProjectVersion.txt`.
2. Créer `UnityProject/` avec le modèle 3D URP. Ajouter Input System, Cinemachine, AI Navigation, Test Framework et Visual Studio Editor avec versions résolues figées. Ajouter Timeline lorsque son usage est justifié.
3. Préparer Git : branche de référence `main`, `.gitignore` Unity, conservation des `.meta`, sérialisation texte et règles LFS si les fichiers binaires le nécessitent.
4. Préparer les dossiers Unity, Blender et Documentation prévus dans le cahier des charges. Documenter les versions, la procédure d'ouverture et la compilation.
5. Vérifier l'édition et le débogage d'un script avec VS Code. Vérifier ensuite le second IDE demandé par le cahier des charges sur une machine compatible.
6. Tester le pipeline complet avec un objet puis un personnage provisoire riggé : Blender → FBX → Unity. Contrôler unités, axes, pivot, matériaux, os, animations et réimport après modification.
7. Produire une build Windows x64 simple et la lancer sur une machine Windows, sans Unity ni connexion Internet.

**À clarifier avant exécution :** version Unity retenue, accès à une machine Windows de test, activation/licence Unity utilisable, organisation des outils cloud et locaux. L'automatisation cloud nécessitera aussi de vérifier les modules et destinations réseau utiles. Les identifiants éventuels seront configurés par les mécanismes sécurisés appropriés.

**Preuves attendues :** README d'installation, versions figées, journal d'import/compilation, preuve de lancement Windows et procédure de réimport de l'asset.

### J1 — Tester les choix qui conditionnent tout le jeu

1. Créer un personnage provisoire : déplacement, sprint, saut, caméra avec collision et interaction contextuelle.
2. Définir les profils AZERTY/QWERTY. Résoudre le conflit `Q` : déplacement à gauche en AZERTY et esquive dans le cahier des charges. Prévoir une touche distincte ou un remappage ; définir aussi les commandes d'attaque.
3. Ajouter un terminal interactif et SCAN sur des objets prédéfinis.
4. Tester une attaque, une esquive et un ennemi provisoire pour valider distances, feedback et besoins d'animation.
5. Bloquer géométriquement les trois quartiers et leurs liaisons, avec relief, frontières et repères. Mesurer l'espace réellement accessible et les principaux trajets.
6. Tester le chargement additif et la conservation du joueur et des systèmes lors d'un passage entre zones.

**Validation :** une boucle de cinq minutes exécutée dans une build Windows ; commandes compréhensibles, caméra utilisable, interaction fiable et traversée documentée. La surface accessible exclut mer, fonds de décor et zones bloquées.

**Décision de sortie :** stabiliser contrôleur, caméra, commandes, topographie générale et mode de transition avant de produire les décors détaillés. Reporter la manette et l'accroupissement facultatif.

### J2 — Produire une tranche jouable représentative

Construire une quête du Vieux-Port avec un objectif clair, un dialogue, un ennemi, une utilisation de PATCH et une récompense. Inclure un passage vers une autre zone, même provisoire.

Ajouter la santé, la défaite et le checkpoint ; sauvegarder position, quête, états du monde et capacités. Vérifier la reprise après fermeture complète, le chargement d'une copie de secours et le traitement explicite d'un fichier corrompu.

Habiller un petit secteur avec le pipeline de production prévu : kit architectural modulaire, matériaux URP, personnage animé et audio de base. Conserver le reste en géométrie simple.

**Validation :** faire tester le segment par des personnes qui ne connaissent pas la solution. Relever durée, incompréhensions et blocages ; exécuter les tests de quête et de sauvegarde pertinents. Profiler une build Windows sur le PC cible dès que celui-ci est disponible.

**Décision de sortie :** réestimer la campagne à partir du temps réel nécessaire pour produire une quête, un asset et une minute de contenu. Si la boucle ou le rythme de production ne sont pas viables, corriger puis retester ; toute réduction des exigences fait l'objet d'une décision explicite.

### J3 — Stabiliser les systèmes et l'outillage

Ordre recommandé :

1. Identifiants stables, état runtime distinct des définitions et format de sauvegarde versionné.
2. Progression des quêtes, conditions, récompenses idempotentes, dialogues, journal et flags du monde.
3. Combat, esquive, dégâts, perception et navigation des trois familles d'ennemis.
4. OVERRIDE, énergie, portée, coûts et améliorations du Debug Tool.
5. Streaming des zones, persistance des objets de quête et restauration des états.
6. Menus, options, paramètres audio/vidéo, sous-titres et carte des quartiers visités. Prévoir les profils graphiques Bas, Standard et Élevé, avec ombres, distance d'affichage, limite FPS et VSync configurables.
7. Outils de validation : IDs dupliqués, références manquantes, objectifs impossibles, données incohérentes.

Introduire les modules C# décrits dans le cahier des charges au rythme des besoins. Chaque système doit permettre de produire du contenu concret.

**Validation :** tests EditMode des règles et DTO ; tests PlayMode des interactions, transitions, checkpoint et reprise ; build Windows intégrée. À chaque évolution du format de sauvegarde, tester la migration d'une sauvegarde précédente et le traitement explicite, sans plantage, d'une version non prise en charge. Enregistrer séparément tests passés, échoués, ignorés et non exécutés.

### J4 — Construire la campagne puis détailler le monde

1. Rendre les neuf missions principales et les deux fins terminables avec géométrie et assets provisoires. Répartition : prologue 1 mission, acte I 2, acte II 3, acte III 2, final 1.
2. Intégrer le boss et vérifier les vulnérabilités liées au Debug Tool, les checkpoints et les conséquences du choix final.
3. Produire le contenu par quartiers : Vieux-Port, Quartier Néon, Falaises Rouges. Réutiliser un kit architectural, des matériaux et des animations lorsque cela convient.
4. Ajouter progressivement les cinq quêtes secondaires, environ douze PNJ narratifs et trois à six intérieurs fonctionnels, conformément au périmètre prévu.
5. Ajouter les assets aboutis, LOD, collisions, audio et textes français. Documenter source et licence dès chaque import.
6. Après chaque lot, rejouer sa mission depuis une nouvelle partie et une sauvegarde, puis tester le passage vers les autres quartiers.

**Validation :** campagne terminable de bout en bout, aucun objectif principal définitivement bloqué, états conservés entre quartiers et mesures réelles de surface et de durée.

Les durées par acte du cahier des charges servent de repères de conception. Elles ne justifient pas de ralentir artificiellement les déplacements ou d'ajouter des tâches répétitives.

### J5 — Recette, optimisation et livraison

1. Effectuer plusieurs premières parties chronométrées et documenter les blocages, abandons, temps perdu et durée médiane.
2. Profiler les combats, zones denses et transitions dans une build Windows à 1080p, profil Standard, sur Ryzen 5 3600, RX 590 et 16 Go de RAM. Relever CPU/GPU, mémoire, VRAM et pics de frametime ; corriger puis retester les zones concernées. Tester aussi Bas et Élevé en consignant leurs résultats séparément.
3. Valider sauvegarde, reprise, corruption, checkpoint, options, commandes et accessibilité sur l'ensemble du parcours.
4. Vérifier le lancement hors connexion sur Windows sans Unity installé, puis rouvrir les sources depuis une copie propre avec la version Unity documentée.
5. Terminer crédits, inventaire de licences, documentation d'installation, commandes, architecture, export Blender, compilation et dépannage.
6. Livrer sources Unity, sources Blender, build Windows et rapport de tests daté indiquant machine, version et résultats.

**Acceptation :** appliquer les dix scénarios manuels du cahier des charges. Tout objectif non atteint est consigné avec sa mesure et sa décision de traitement. Une mesure cloud ne remplace pas la validation sur le PC Windows de référence.

## 3. Mesurer les objectifs majeurs

| Objectif | Méthode | Première vérification | Confirmation finale |
|---|---|---|---|
| Environ 1 km² explorable | Mesure des surfaces effectivement accessibles et parcours des liaisons ; exclusions documentées | J1 | J4/J5 |
| Campagne d'environ trois heures | Premières parties chronométrées, médiane, dispersion, blocages et temps perdu | Segment J2, campagne J4 | J5 |
| 60 FPS à 1080p, profil Standard | Profiling de builds Windows sur le PC cible ; budget de 16,7 ms/image, observation des pics et transitions | J2 | J5 |
| Sauvegarde fiable | Fermeture/reprise, corruption, copie de secours, états de missions et format versionné | J2 | Chaque jalon suivant |
| Sources modifiables | Réouverture propre, édition C#, modification Blender, réimport et reconstruction | J0 | J5 |

## 4. Organisation et estimation

En solo, limiter le travail en cours à une fonctionnalité ou une mission. Découper chaque tâche pour obtenir un résultat visible en une à trois séances. Séparer les séances de code, de création d'assets et d'intégration ; terminer chaque lot par une build jouable. Tenir un backlog simple : à faire, en cours, à valider, terminé.

Exemple de semaine de dix heures : deux heures de formation ciblée, quatre heures de réalisation Unity/C#, deux heures de Blender/contenu, une heure de tests et une heure de bilan/documentation. Adapter cette répartition au jalon ; une difficulté technique peut occuper plusieurs séances.

**Fourchettes initiales de planification, pour un débutant seul à dix heures par semaine :**

| Étape | Effort indicatif | Calendrier indicatif |
|---|---|---|
| A — Bases Unity/C#/Blender | 40–80 h | 4–8 semaines |
| J0 — Chaîne de production | 30–60 h | 3–6 semaines |
| J1 — Prototype | 60–120 h | 6–12 semaines |
| J2 — Tranche jouable | 100–180 h | 10–18 semaines |

Ces budgets sont des hypothèses de travail, pas des mesures ni des engagements. Les essais d'apprentissage réutilisés dans J0/J1 peuvent réduire certains postes. Recalculer à la fin de A et J0 selon le temps réellement consommé. Ajouter une réserve d'environ 30 % pour les difficultés, reprises et corrections : l'ordre de grandeur jusqu'à J2 est alors **300–570 heures**, soit **environ sept à quatorze mois à dix heures par semaine**, hors interruptions. Une disponibilité différente change le calendrier ; l'apprentissage ne se réduit pas toujours proportionnellement à davantage d'heures.

La version complète représente un effort important de contenu après la mise en place des systèmes. À ce rythme, un projet de plusieurs années est possible. La date de sortie sera estimée après J2, lorsque le coût réel des missions et des assets sera connu. Les objectifs de neuf missions, trois quartiers et trois heures restent ceux du jeu complet.

Après J2, établir une estimation par lot à partir des temps mesurés : missions restantes, environnements, personnages, animations, audio et recette. Distinguer le travail réutilisable du travail propre à chaque mission. Ajouter une réserve visible pour intégration, corrections et retests ; convertir l'effort en calendrier selon la disponibilité réelle. Recalculer cette prévision après chaque quartier produit.

## 5. Premières actions concrètes

1. Fixer un rythme hebdomadaire tenable et vérifier l'accès à une machine Windows de test.
2. Choisir la version exacte de Unity 6 et préparer les outils C# sur le poste de développement retenu. Le PC Windows permet le travail interactif et les tests de l'exécutable ; les usages cloud seront configurés et vérifiés séparément.
3. Réaliser et valider les exercices de l'étape A.
4. Créer et versionner le projet URP, figer les packages, produire une build Windows vide et vérifier édition/débogage C#.
5. Valider l'aller-retour Blender/FBX/Unity, puis un personnage provisoire animé.
6. Démarrer J1 avec contrôleur, caméra, commandes et petit parcours ; étendre ensuite le blocage aux trois quartiers.

Le prochain livrable personnel est la scène d'exercices A. Le prochain livrable de production est J0 validé. Le premier point de décision sur la faisabilité du jeu complet est la tranche jouable J2.
