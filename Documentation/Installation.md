# Installation et dépannage

## Poste Windows

Préparer Unity Hub, Unity 6000.0.84f1 avec Windows Build Support (Mono), Blender 4.3.2 ou version compatible, Git et VS Code avec l'extension Unity de Microsoft. Le package Unity Visual Studio Editor est épinglé dans le projet. Le second IDE demandé par le cahier des charges doit être installé avec les outils de développement de jeux Unity et vérifié sur le poste Windows.

Dans Unity Hub, rechercher **6000.0.84f1** dans l'archive des versions. L'ancien pin 6000.0.40f1 présente une alerte de sécurité : ne pas utiliser « Accepter et ignorer » pour poursuivre avec cette version. Installer le correctif demandé puis ouvrir le projet avec cet éditeur. La compatibilité réelle des scripts et packages doit encore être confirmée à l'import. Les sources et références de la mise à jour figurent dans [UnityVersion.md](UnityVersion.md).

Ouvrir `UnityProject`, attendre la résolution des packages, exécuter **RIVE 404 > 1. Préparer le projet**, puis ouvrir Bootstrap. La préparation conserve les catalogues déjà édités. Elle configure URP et les shaders nécessaires aux modèles créés à l'exécution.

Les pins demandés sont URP 17.0.3, Input System 1.11.2, Cinemachine 3.1.3, AI Navigation 2.0.6, Test Framework 1.4.5 et Visual Studio Editor 2.0.22. La résolution et la compatibilité de cette combinaison restent à confirmer par le premier import Unity réussi. Cinemachine est disponible comme dépendance ; la caméra actuelle utilise un contrôleur C# avec collision.

## Blocage cloud observé

Les requêtes HTTPS à `services.api.unity.com` et `packages.unity.com` ont renvoyé **403 CONNECT**. Les destinations `services.api.unity.com`, `packages.unity.com`, `download.unity3d.com` et `docs.unity3d.com` ont été ajoutées au brouillon de configuration, en conservant les presets existants. Cet enregistrement ne change pas le réseau courant et ne publie pas l'environnement.

Lors de la correction de version, une requête à l'API officielle des releases `services.api.unity.com` a ensuite réussi. Elle a permis de vérifier 6000.0.84f1 ; ce résultat ne prouve ni la résolution du registry des packages ni la disponibilité d'un éditeur activé. La page de sécurité `unity.com` et les notes hébergées sur `storage.googleapis.com` sont restées inaccessibles depuis cette machine.

Pour reprendre la préparation Unity dans le cloud, revoir et enregistrer les paramètres, puis publier l'environnement lorsque l'interface le demande. Vérifier ensuite le téléchargement officiel et l'activation par un mécanisme Unity approprié. Les domaines supplémentaires révélés par un téléchargement réel doivent être ajoutés au besoin. Ne mettre aucune licence ni valeur de secret dans les sources, les journaux ou le chat.

La machine actuelle est Debian x64. La compatibilité du système, des bibliothèques natives et de l'activation Unity devra être vérifiée avant de considérer le mode batch disponible. Le poste Windows reste nécessaire pour la recette Windows et les mesures sur la configuration cible.

Le brouillon contient aussi le script d'installation .NET et les instructions de démarrage réutilisables. Le script utilise le checkout existant `/workspace/testVideoGame` et lance les contrôles du domaine, de syntaxe et de structure. Son enregistrement a été confirmé ; sa publication et son exécution dans une nouvelle tâche ne l'ont pas été.

Au début de la réalisation, le dépôt distant était vide et ne présentait aucune ref `main` ; le checkout initial était sur une branche `work` sans commit. L'utilisateur a ensuite autorisé l'envoi des sources sur `main`. Après confirmation de cet envoi, télécharger le projet depuis [Nico91170/testVideoGame](https://github.com/Nico91170/testVideoGame) avec **Code > Download ZIP**, puis extraire l'archive. La configuration cloud ne constitue pas une preuve de restauration des sources dans une nouvelle tâche.

## Diagnostic

| Symptôme | Vérification et correction |
|---|---|
| Packages non téléchargés | Lire l'erreur Package Manager ; vérifier les destinations officielles et l'authentification nécessaire |
| Scène vide | Ouvrir Bootstrap, exécuter Préparer puis Nouvelle partie ; le monde est généré au démarrage de la session |
| Matériaux roses | Vérifier l'import URP et relancer Préparer ; les matériaux FBX sont convertis en URP au chargement |
| Animation absente | Vérifier les clips Idle/Walk et l'import Generic ; le contrôleur utilise Playables |
| Mouvement absent | Activer Input System Package (New), regarder les erreurs et quitter menu/dialogue/pause |
| Erreur du préparateur URP | Conserver le message ; vérifier la version exacte et les signatures du package avant correction |
| Module Windows manquant | Ajouter Windows Build Support (Mono) pour le même éditeur dans Unity Hub |
| Sauvegarde illisible | Conserver principal et `.bak` ; le jeu vérifie le checksum et tente le secours |

Sous Windows, la progression est stockée sous `Application.persistentDataPath`, habituellement `%USERPROFILE%/AppData/LocalLow/RIVE404/RIVE 404 - Le Dernier Commit/`. Le fichier est `rive404-save.json`, accompagné d'une copie `.bak` après remplacement d'une sauvegarde valide. Les préférences utilisent PlayerPrefs, séparément de la progression.

Lancer les scripts batch avec `UNITY_EDITOR` défini vers l'exécutable exact ; `Tools/unity_checks.sh` écrit les journaux et XML dans `Builds/TestResults`. La syntaxe shell convient à Linux ou Git Bash ; les menus Unity offrent les mêmes opérations pour l'utilisation interactive Windows.
