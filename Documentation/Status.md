# État des exigences — 11 octobre 2026

Les sources livrent une première implémentation intégrée. Le jeu complet ne peut pas être déclaré accepté tant que les validations Unity/Windows et le contenu final n'ont pas été terminés.

| Exigence | Produit dans les sources | Preuve / travail restant |
|---|---|---|
| PRD-01 — Projet Unity ouvrable | Version, manifest, Bootstrap, scripts et métadonnées | Import et résolution des packages non exécutés ; lock à produire |
| PRD-02 — Gameplay C#/Unity | Systèmes intégrés et campagne | Domaine compilé ; compilation Unity non exécutée |
| PRD-03 — Sources Blender | 16 `.blend`, 16 FBX, rigs et clips | Réouverture et aller-retour Blender testés ; import/réimport Unity non exécutés |
| PRD-04 — IDE/débogage | Intégration VS Editor, code éditable | Points d'arrêt VS Code/Visual Studio à vérifier sur le poste |
| PRD-05 — Windows hors connexion | Commande/menu de build | Aucun `.exe` produit ; module, licence et recette Windows requis |
| PRD-06 — Monde ~1 km² | Sol brut 1000×1000, trois quartiers, six intérieurs, cellules activées par distance | Surface accessible et transitions non mesurées ; scènes additives non implémentées |
| PRD-07 — Campagne ~180 min | Neuf missions, cinq secondaires, 85 pages de dialogues, boss et deux fins | Parcours et durée non mesurés ; contenu/rythme à enrichir après playtest |
| PRD-08 — Sauvegarde/reprise | JSON/checksum/temporaire/secours, migrations et checkpoints | Stockage et règles testés ; fermeture/reprise de l'application Unity à vérifier |
| PRD-09 — Français | UI, objectifs, dialogues, narratif des terminaux | Relecture visuelle exhaustive en jeu à terminer |
| PRD-10 — 1080p/60FPS | Profils Bas/Standard/Élevé et activation des décors | Aucun profiling PC cible ; ni FPS ni absence de gel certifiés |
| PRD-11 — Audio/droits | 14 WAV originaux, inventaire assets, crédits | Mixage en jeu et audit de la distribution finale à terminer |

## Éléments de production à poursuivre

- Compiler/importer avec l'éditeur exact, exécuter les tests Unity et corriger tout résultat observé.
- Générer la build Windows, vérifier le débogage et le vrai pipeline Blender/Unity.
- Faire un parcours complet et chronométré, ajuster les missions, les distances, les combats et la densité du monde.
- Enrichir les animations de combat et les séquences narratives, développer les quartiers et les intérieurs au-delà des modules initiaux.
- Valider le chargement de zones, les optimisations et les performances sur le PC de référence.
- Clore la recette et les notices de distribution avant de qualifier le jeu de terminé.

Les sources sont conservées localement et dans l'archive de livraison. Pour les récupérer depuis [le dépôt GitHub](https://github.com/Nico91170/testVideoGame), sélectionner `main`, puis **Code > Download ZIP**, après confirmation de leur envoi. Aucun exécutable Windows n'est livré et aucune publication de jeu ou de l'environnement n'a été effectuée.
