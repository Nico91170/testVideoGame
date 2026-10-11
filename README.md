# RIVE 404 — Le Dernier Commit

Jeu solo 3D pour Windows x64, en Unity 6/C#. Nico découvre Santa Riva, une ville figée dans une simulation de la fin des années 1990. Avec le capitaine Sillage, il rassemble trois fragments et choisit l'avenir du monde.

Pour télécharger les sources, ouvrir [Nico91170/testVideoGame](https://github.com/Nico91170/testVideoGame), sélectionner la branche `main`, puis **Code > Download ZIP**. Extraire l'archive et utiliser le dossier `UnityProject/` dans Unity Hub.

## État des sources

Implémentation source créée le 11 octobre 2026 : monde généré de trois quartiers, neuf missions principales, cinq secondaires, douze PNJ, dialogues français, combat, boss en trois phases, deux fins, Debug Tool, sauvegarde, carte et menus. Les sources Blender et les sons originaux sont inclus.

**Le projet n'a pas encore été importé ni compilé dans Unity. Aucun exécutable Windows n'est livré.** L'accès cloud aux services Unity est bloqué et aucun éditeur activé n'est disponible dans cette machine. Les tests du domaine C# et du stockage sont réels ; ils ne prouvent pas le fonctionnement du gameplay Unity. Voir [le rapport de validation](Documentation/Tests.md) et [l'état des exigences](Documentation/Status.md).

Les objectifs d'une campagne de trois heures, d'environ 1 km² réellement explorable et de 60 FPS en 1080p sur Ryzen 5 3600/RX 590/16 Go restent à mesurer et à ajuster.

## Ouvrir et lancer

1. Installer **Unity 6000.0.40f1** avec Unity Hub et le module **Windows Build Support (Mono)**. Activer votre licence dans Unity Hub.
2. Ajouter le dossier **`UnityProject/`** à Unity Hub. Ouvrir avec cette version et attendre le téléchargement des packages et l'import des FBX.
3. Dans Unity, choisir **RIVE 404 → 1. Préparer le projet**. Ce menu génère les vrais assets URP, les trois profils graphiques et les catalogues éditables.
4. Ouvrir **`Assets/_RIVE404/Scenes/Bootstrap.unity`**, lancer **Play**, puis **Nouvelle partie**.
5. Parler à **Brune**, près du point de départ. Le journal et la carte indiquent le prochain objectif.

Si l'import ou la compilation échoue, conserver le message de console et le journal : ces résultats sont indispensables pour terminer la validation de cette première implémentation.

## Construire Windows

Exécuter les tests EditMode et PlayMode dans **Window → General → Test Runner**, puis utiliser **RIVE 404 → 3. Construire Windows x64**. Le résultat attendu est `Builds/Windows/RIVE404.exe` et ses fichiers associés. Distribuer le dossier complet, puis le tester sur Windows sans Unity et sans réseau.

Le fichier `Packages/packages-lock.json` sera produit par le vrai Package Manager après une résolution réussie ; le conserver ensuite dans Git. Il n'a pas été inventé pendant le blocage réseau.

## Commandes

| Action | Commande |
|---|---|
| Déplacement | ZQSD / WASD, positions physiques identiques ; flèches en secours |
| Caméra / distance | Souris / molette |
| Sprint / saut / accroupissement | Maj / Espace / C |
| Interaction | E, à proximité de la cible visée |
| Attaque légère / forte | Clic gauche / clic droit |
| Esquive | Alt gauche |
| SCAN / PATCH / OVERRIDE | Tab / 1 / 2 |
| Journal / carte / aide | J / M / H |
| Pause / sauvegarde | Échap / F5 |

Les bornes vertes restaurent santé et énergie, enregistrent le checkpoint et sauvegardent. Le Debug Tool applique des corrections temporaires ; les découvertes et la progression sont conservées. L'Archiviste est vulnérable pendant l'OVERRIDE actif du noyau.

## Sources et vérifications

| Dossier | Contenu |
|---|---|
| `UnityProject/` | Projet, sources C#, scène Bootstrap, FBX et WAV |
| `Blender/` | 16 modèles `.blend`, générateurs, aperçu et rapports de réimport |
| `Documentation/` | Installation, architecture, campagne, licences, tests et état des exigences |
| `Tools/` | Installation .NET vérifiée, tests du domaine, syntaxe, intégrité et commandes Unity |

Dans le cloud préparé :

```bash
cd /workspace/testVideoGame
Tools/run_domain_checks.sh
Tools/run_syntax_checks.sh
python3 Tools/check_project.py
```

Les sorties conservées figurent dans `Documentation/Validation/`. Pour recréer l'archive des sources avec manifest SHA-256 et vérification CRC : `python3 Tools/package_sources.py`. Le résultat est placé à côté du dépôt, avec un fichier de checksum séparé.

Pour une autre machine, installer d'abord le SDK via `Tools/install_dotnet.sh` sous Linux x64, ou utiliser votre SDK .NET 8 et définir `RIVE_DOTNET` vers son exécutable. Les vérifications syntaxiques ne constituent pas une compilation contre Unity.

Voir [Installation](Documentation/Installation.md), [Architecture](Documentation/Architecture.md), [Campagne](Documentation/Campaign.md), [Pipeline Blender](Blender/README.md) et [Licences des assets](Documentation/ASSET_LICENSES.md).
