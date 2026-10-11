# Correction de la version Unity — 11 octobre 2026

Unity Hub signale que les applications créées avec l'ancien éditeur **6000.0.40f1** présentent un problème de sécurité connu. Le projet et les instructions ciblent désormais **6000.0.84f1**, révision **78ab6fc243d5**, dans la même branche Unity 6.0 LTS.

## Preuves de la sélection

- La fiche canonique [CVE-2025-59489](https://github.com/CVEProject/cvelistV5/blob/main/cves/2025/59xxx/CVE-2025-59489.json) indique que les versions 6000.0 LTS antérieures à **6000.0.58f2** sont affectées. Elle référence [l'avis Unity](https://unity.com/security/sept-2025-01). Ce seuil concerne cette faille précise et ne certifie pas l'absence de toute autre vulnérabilité.
- L'[API officielle Unity — branche 6000.0](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.0&limit=5), consultée le 11 octobre 2026, retourne **6000.0.84f1** en premier : flux LTS, publication le 16 septembre 2026, révision 78ab6fc243d5. Cette version est postérieure au correctif indiqué par la fiche CVE.
- L'[API Unity — correctif 6000.0.58f2](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.0.58f2&limit=1) confirme son existence et sa publication le 3 octobre 2025, révision 92dee566b325.

L'API et la fiche CVE ont été récupérées avec succès. La page d'avis Unity et les notes sur le stockage Google ont renvoyé un refus réseau depuis ce cloud ; leur contenu n'est pas présenté comme ayant été lu.

## Installation et limites

Dans Unity Hub, rechercher **6000.0.84f1**, installer Windows Build Support (Mono) et activer la licence. Ne pas ignorer les alertes de sécurité. Télécharger les sources mises à jour et sélectionner cet éditeur pour `UnityProject/`.

Le changement conserve la branche 6000.0 et les packages actuellement épinglés. Leur résolution, l'import, la compilation Unity et le gameplay n'ont pas encore été validés dans l'éditeur. Le contrôle structurel des sources est relancé après le changement de pin ; les résultats historiques ne constituent pas une recette de cette version.

La fiche CVE précise qu'une application affectée doit être **reconstruite et redistribuée** avec le runtime corrigé. Aucune build Windows n'avait été produite ni publiée pour ce projet.
