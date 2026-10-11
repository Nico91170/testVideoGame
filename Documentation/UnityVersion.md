# Migration vers Unity 6.6 — 11 octobre 2026

Le projet cible **Unity 6.6 (6000.6.5f1)**, révision **3ff58d469c8a**, à la demande de l'utilisateur. Le fichier `ProjectVersion.txt`, le manifest, les outils et les instructions utilisent cette cible.

## Éditeur et packages vérifiés

L'[API officielle Unity](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.6.5f1&limit=1) confirme **6000.6.5f1**, publiée le **8 octobre 2026**, révision **3ff58d469c8a**. Le flux est `SUPPORTED` et la version est marquée recommandée dans la réponse consultée.

| Package | Version | Source officielle pour Unity 6.6 |
|---|---|---|
| Universal Render Pipeline | 17.6.0 | [Manuel 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.render-pipelines.universal.html), [package.json Graphics](https://raw.githubusercontent.com/Unity-Technologies/Graphics/master/Packages/com.unity.render-pipelines.universal/package.json) |
| SRP Core, dépendance URP | 17.6.0 | [Documentation Core 17.6](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.6/manual/index.html) |
| Input System | 1.20.1 | [Manuel 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.inputsystem.html) |
| AI Navigation | 2.0.16 | [Manuel 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.ai.navigation.html) |
| Cinemachine | 6.6.0 | [Manuel 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.cinemachine.html), [documentation 6.6](https://docs.unity3d.com/Packages/com.unity.cinemachine@6.6/manual/index.html) |
| Test Framework | 1.8.0 | [Manuel 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.test-framework.html), [documentation 1.8](https://docs.unity3d.com/Packages/com.unity.test-framework@1.8/manual/index.html) |
| Visual Studio Editor | 2.0.28 | [Manuel 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.ide.visualstudio.html) |

URP, Cinemachine et Test Framework sont indiqués comme packages cœur liés à la version de l'éditeur. Les versions `latest` du registre public ne décrivent pas correctement ces packages pour 6000.6 : les pages propres à Unity 6.6 et leurs métadonnées ont été utilisées. Le Package Manager de cet éditeur doit effectuer la résolution complète et produire le fichier lock ; il n'a pas été fabriqué manuellement.

## Adaptations des API

- `GameRuntime.ApplyAudioSettings` utilise désormais `FindObjectsByType<AudioSource>()`. L'ancienne surcharge avec `FindObjectsSortMode.None` et son enum sont obsolètes dans [UnityCsReference 6000.6](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.6/Runtime/Export/Scripting/UnityEngineObject.bindings.cs). La recherche reste limitée aux objets actifs, sans tri.
- La préparation URP vise **17.6.0**. Les signatures de `CreateRendererAsset(string, RendererType, bool, string)`, `UniversalRenderPipelineAsset.Create(ScriptableRendererData)`, `UniversalRenderPipelineGlobalSettings.Ensure(bool)` et `ResourceReloader.ReloadAllNullIn(object, string)` restent présentes dans les [sources officielles Graphics](https://github.com/Unity-Technologies/Graphics). Les appels existants sont conservés avec leurs contrôles de signatures.
- Les API utilisées pour la physique, CharacterController, NavMeshAgent, Playables, l'affichage et la qualité ont été inspectées dans la branche officielle [UnityCsReference 6000.6](https://github.com/Unity-Technologies/UnityCsReference/tree/6000.6). Les références d'assemblages existantes restent cohérentes avec les packages documentés.

Ces revues établissent les versions et signatures consultées ; elles ne remplacent pas une compilation dans l'éditeur exact.

## Ouvrir la livraison migrée

1. Installer **6000.6.5f1** via Unity Hub, avec **Windows Build Support (Mono)** et une licence activée.
2. Télécharger les sources mises à jour. Si une ancienne livraison a déjà été modifiée, conserver ces modifications et utiliser une nouvelle extraction pour le premier import 6.6.
3. Ouvrir `UnityProject/` avec **6000.6.5f1** et attendre la résolution et l'import sans erreurs.
4. Exécuter **RIVE 404 > 1. Préparer le projet**, ouvrir Bootstrap puis lancer les tests EditMode et PlayMode avant Play et la build Windows.

Les catalogues C# et les sources Blender restent éditables. Les anciens assets URP générés ne doivent pas être recopiés dans la nouvelle extraction ; réintégrer les données et modèles personnalisés après validation de leur import.

## Validations et historique

Les contrôles du domaine, de syntaxe et de structure sont disponibles dans `Tools/`. Aucun éditeur Unity activé n'est disponible dans ce cloud : **l'import, la compilation Unity, les tests Unity et la build Windows restent à exécuter sur 6000.6.5f1**. Les métadonnées du registry et les pages de documentation sont accessibles, mais les tarballs de `download.packages.unity.com` ont été refusés ; aucune résolution complète des dépendances n'est certifiée.

La cible initiale 6000.0.40f1 était affectée par [CVE-2025-59489](https://github.com/CVEProject/cvelistV5/blob/main/cves/2025/59xxx/CVE-2025-59489.json) et avait été remplacée par 6000.0.84f1 avant cette migration 6.6. Ne pas ignorer les alertes de Unity Hub. Aucune build Windows issue de l'ancien éditeur n'avait été produite ni publiée.
