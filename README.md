# Vaultbreakers in the Shatterbelt

A controller-first, fixed-camera science-fiction arcade brawler built with
Unity **6000.4.4f1** and URP. The current combat POC implements movement, aiming,
melee, ranged fire, directional shielding, and dodging against practice dummies.
Enemies and the wave/retry loop are the next implementation phases.

## Open the project

Install Git LFS before cloning so the Blender source, FBX model, and image assets
are restored as real files:

```sh
git lfs install
git clone https://github.com/Starbugstone/VaultBreakers.git
cd VaultBreakers
git lfs pull
git lfs fsck
```

Open the project with the pinned Unity version, allow Package Manager to restore
`Packages/manifest.json` and `Packages/packages-lock.json`, then open
`Assets/Vaultbreakers/Scenes/Prototype/Prototype_Arena.unity` and press Play.
The modular equipment showcase is in
`Assets/Vaultbreakers/Scenes/Test/Avatar_Showcase.unity`.

## Project documentation

- [Project intent and architecture](Docs/PROJECT.md)
- [Combat POC phases and exit gates](Docs/COMBAT_POC_PLAN.md)
- [Implementation status and validation limits](Docs/PROJECT_STATUS.md)
- [Modular avatar contract](Docs/MODULAR_AVATAR_PIPELINE.md)
- [Implementation decisions](Docs/IMPLEMENTATION_NOTES.md)

Run EditMode and PlayMode tests using Unity's Test Runner. Generated project
assets can be rebuilt through **Vaultbreakers > Setup > Build 3D Foundation and
Modular Avatar**; this rewrites the generated scenes and prefab, so preserve any
manual edits before running it. The Blender generation script is
`Tools/Blender/generate_modular_vaultbreaker.py`.

Git tracks the source, project settings, package manifests, Unity assets and
metadata, Blender source, and documentation. Local caches, raw logs, generated
IDE files, and build output are excluded by `.gitignore`; Unity can regenerate
them. Validation summaries belong in `Docs/`.

## License

Copyright (c) 2026 StarbugStone. All rights reserved.

The original project materials are proprietary. See [LICENSE](LICENSE) for the
terms and [the asset and dependency register](LICENSES/THIRD_PARTY_ASSETS.md) for
third-party exceptions. Public repository access does not grant an open-source
license. Contact the repository owner for written permission to reuse materials.
