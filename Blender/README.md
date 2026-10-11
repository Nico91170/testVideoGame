# Pipeline original RIVE404

Les 16 modèles ont été créés pour RIVE404. Chaque `.blend` conserve ses pièces séparées et ses matériaux éditables. Le FBX correspondant réunit ces pièces en **un seul mesh**, avec plusieurs matériaux : le nombre de renderers reste ainsi réduit dans Unity. Aucune texture externe n'est requise.

## Régénérer et contrôler

Depuis la racine du dépôt, avec **Blender 4.3.2** et Python 3 :

```bash
blender -b --python Blender/Tools/generate_assets.py
python3 Blender/Tools/generate_audio.py
blender -b --python Blender/Tools/validate_assets.py -- --render
blender -b --python Blender/Tools/validate_editable_sources.py
```

La première commande remplace les assets générés : sauvegarder les modifications manuelles avant régénération. Les générateurs sont la source reproductible initiale, les `.blend` sont les sources d'édition artistique. Le rapport `validation_report.json` contrôle le réimport de chaque FBX dans Blender, les unités, poids de skinning, os et formats WAV. `editable_sources_report.json` vérifie la réouverture des 16 sources et la conservation d'une modification de mesh lors d'un export/réimport jetable. Le rendu `Preview/Kit_Overview.png` est un aperçu studio Blender, pas une capture du jeu Unity.

## Modifier un modèle sans régénérer le kit

1. Ouvrir son fichier dans `Characters`, `Buildings`, `Environment` ou `Props`.
2. Modifier les pièces, conserver le pivot à l'origine et enregistrer le `.blend`.
3. Sélectionner uniquement meshes et armature. Pour un personnage, conserver les poids des groupes de sommets.
4. Pour retrouver l'export optimisé sans régénérer les autres modèles, exécuter dans la console Blender en adaptant le chemin et le nom :

```python
import runpy
kit = runpy.run_path('/CHEMIN/testVideoGame/Blender/Tools/generate_assets.py', run_name='export_library')
kit['save']('Nico', 'Characters')
```

Alternative : export manuel FBX, Selected Objects, Mesh + Armature, Forward `-Z`, Up `Y`, Apply Unit Scale, Scale `1`, Add Leaf Bones désactivé ; Bake Animation avec All Actions pour les personnages. Réexporter vers `UnityProject/Assets/_RIVE404/Resources/Models/<Nom>.fbx` en conservant le `.meta`. Unity réimporte automatiquement. Un export manuel non fusionné contient davantage de renderers ; joindre les copies des meshes avant export si nécessaire, puis annuler la fusion dans le `.blend` d'édition.

## Conventions

- Un mètre Blender correspond à une unité Unity. Les modèles reposent sur Z=0 dans Blender ; le sol devient Y=0 dans Unity.
- Les faces avant regardent Blender `-Y`, soit Unity `+Z` après l'export `-Z/Y`.
- `Nico`, `Sillage`, `NPC` et `Corrupted` : rigs à 14 os, poids rigides par pièce, clips `Nom_Idle` et `Nom_Walk`. Le mode d'import **Legacy** permet l'utilisation de `Animation` dans le runtime. Les clips ne contiennent pas de root motion : le contrôleur déplace le personnage.
- Il s'agit de rigs simples, pas d'une certification Humanoid/Mecanim. Les quatre rigs utilisent les mêmes os. Les robots et drones sont des meshes statiques dont le runtime déplace les objets.
- Les matériaux `MAT_*` sont opaques, à couleurs unies ; les lunettes ne nécessitent pas de transparence. Le runtime reconstruit les matériaux URP à partir des couleurs importées. Glow/Warning possèdent aussi une émission dans Blender.
- Les sources architecturales restent modulaires et les entrées géométriques de House, Warehouse et Fort sont ouvertes. Les collisions Unity et la caméra se règlent dans le monde et ne sont pas incorporées aux exports.
- Tous les exports sont de petite complexité (moins de 1 000 sommets par modèle). Le kit n'utilise pas de LOD : la segmentation/désactivation du monde reste nécessaire pour maîtriser les appels de rendu et les IA.

## Audio

`generate_audio.py` compose et synthétise une boucle musicale de 64 secondes, trois ambiances de 20 secondes et dix effets. Aucun enregistrement ou échantillon tiers. Format mono PCM16 22 050 Hz, volume modéré et petites enveloppes pour éviter les clics. Les fichiers sont chargés avec `Resources.Load<AudioClip>("Audio/Theme")` et les autres noms de fichier sans extension. Ces ambiances sont une interprétation synthétique stylisée, pas des prises de son réalistes.

## Limites de validation

Les sources Blender, exports FBX et fichiers WAV ont été générés et contrôlés. L'import Unity, le retargeting Humanoid éventuel, les animations en situation de combat, le mixage en jeu et les performances Windows exigent des validations dans Unity et dans une build. Le rapport n'affirme pas ces résultats.
