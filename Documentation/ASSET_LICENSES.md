# Inventaire et droits des assets — RIVE404

Date : 11 octobre 2026. Les modèles, leurs matériaux et les sons ci-dessous ont été produits spécialement pour ce projet, par génération procédurale originale assistée par Codex. Aucun mesh, texture, photographie, enregistrement, composition musicale, logo, costume ou personnage provenant d'un jeu tiers n'a été incorporé.

**Licence des assets créés : CC0 1.0 Universal.** Les meshes, matériaux, sources `.blend`, exports `.fbx`, fichiers audio `.wav` et l'aperçu PNG du kit sont mis à disposition sans obligation d'attribution. Utilisation, modification et redistribution commerciales autorisées dans les limites légales applicables. Texte et description officielle : <https://creativecommons.org/publicdomain/zero/1.0/>. Les scripts de génération sont du code du projet et suivent sa licence logicielle lorsqu'elle est définie ; ils n'incorporent pas de code copié d'un projet tiers.

| Ensemble | Fichiers / source | Auteur | Licence | Modifications / obligations |
|---|---|---|---|---|
| Nico | `Blender/Characters/Nico.blend`, `Resources/Models/Nico.fbx` | Création originale pour Nico91170 / RIVE404, assistée par Codex | CC0-1.0 | Roux, lunettes, barbe, survêtement avec accents rouge/vert ; aucun logo de marque. Aucune attribution obligatoire. |
| Capitaine Sillage | `Blender/Characters/Sillage.blend`, `Resources/Models/Sillage.fbx` | Même provenance | CC0-1.0 | Officier original à cape écarlate, épée, costume bleu/crème et boucle géométrique. Aucun emblème ou personnage tiers reproduit. |
| Habitant et agent corrompu | `Blender/Characters/{NPC,Corrupted}.blend`, FBX correspondants | Même provenance | CC0-1.0 | Docker au gilet et à la casquette ; variante corrompue violette/turquoise. |
| Ennemis mécaniques | `Blender/Characters/{Drone,Sentinel,Boss}.blend`, FBX correspondants | Même provenance | CC0-1.0 | Géométrie originale assemblée ; boss couronné de fragments d'archive. |
| Architecture | `Blender/Buildings/{House,Warehouse,Tower,Fort}.blend`, FBX correspondants | Même provenance | CC0-1.0 | Maison du port, dépôt, tour néon et fort à cour ouverte ; pièces sources séparées éditables. |
| Environnement et accessoires | `Blender/Environment/Tree.blend`, `Blender/Props/{Lamp,Terminal,Crate,Boat}.blend`, FBX correspondants | Même provenance | CC0-1.0 | Géométrie et palette originales, aucune texture tierce. |
| Musique | `Resources/Audio/Theme.wav` | Composition et synthèse originales pour RIVE404, assistées par Codex | CC0-1.0 | Boucle de 64 s, harmonie, mélodie et arpège définis dans `generate_audio.py`. Aucun sample ou enregistrement tiers. |
| Ambiances | `Resources/Audio/{PortAmbience,NeonAmbience,CliffsAmbience}.wav` | Même provenance | CC0-1.0 | Trois boucles synthétiques de 20 s ; bruit pseudo-aléatoire déterministe et oscillateurs. |
| Effets | `Resources/Audio/{Footstep,Swing,Impact,Scan,Patch,Override,UI,Quest,Death,Energy}.wav` | Même provenance | CC0-1.0 | Dix effets synthétiques, aucune banque de sons. |
| Aperçu | `Blender/Preview/Kit_Overview.png` | Rendu des modèles du projet par Blender | CC0-1.0 | Planche de contrôle, pas une capture Unity ou une preuve de gameplay. |

Tous les fichiers `Resources/...` de ce tableau se trouvent sous `UnityProject/Assets/_RIVE404/`. Les sources procédurales sont `Blender/Tools/generate_assets.py` et `Blender/Tools/generate_audio.py` ; il n'existe pas d'URL de téléchargement externe des assets, car ils ont été créés localement.

## Logiciels et packages distincts des assets

Blender 4.3.2 a servi à créer/exporter les modèles et à rendre la planche de contrôle. Son utilisation n'impose pas la licence GPL de Blender aux meshes et aux rendus créés. Informations : <https://www.blender.org/about/license/>. Python et sa bibliothèque standard servent à générer les WAV ; aucune bibliothèque audio n'est redistribuée avec eux.

Unity et les packages URP, Input System, AI Navigation, Test Framework et Visual Studio Editor relèvent de leurs licences et conditions respectives ; leur présence dans le projet n'est pas une revendication de propriété sur ces logiciels. Conserver les notices des packages et examiner les conditions Unity applicables à la distribution de l'exécutable. Aucun logiciel Blender/Python n'est nécessaire dans la build distribuée.

## Portée de l'audit

L'inventaire couvre le kit effectivement présent, pas de futurs téléchargements. Toute nouvelle ressource tierce doit ajouter ici sa source, son auteur, sa licence, son URL, ses changements et ses obligations avant distribution. Les crédits en jeu peuvent remercier « Création originale RIVE404 — développement et génération d'assets assistés par Codex », sans présenter un artiste tiers fictif. La distribution commerciale de futurs contenus non inventoriés reste à examiner séparément.
