# Source-control checkpoint validation — 2026-09-08

This checkpoint preserves the previously uncommitted Phase 8 combat implementation,
tests, generated Unity assets, tuning, and documentation, and adds proprietary licensing
and restore instructions. No gameplay code was changed during the checkpoint task.

## Automated results

Unity version: **6000.4.4f1**, Windows editor, `-batchmode -nographics`.

| Check | Result | Local evidence |
|---|---|---|
| EditMode tests | 157 passed, 0 failed, 0 skipped; process exit 0 | `Logs/Sync-20260908/EditMode.xml` and `EditMode.log` |
| PlayMode tests | 50 passed, 0 failed, 0 skipped; process exit 0 | `Logs/Sync-20260908/PlayMode.xml` and `PlayMode.log` |
| Windows development build | Succeeded; process exit 0 | `Logs/Sync-20260908/Build.log` |
| Compiler diagnostics | No C# compiler errors or warnings found in the three logs | Same logs |
| Git LFS integrity | Five current binary assets pass `git lfs fsck` | Blender source, FBX, three preview PNGs |
| Unity asset metadata | Every source asset has its corresponding tracked or pending `.meta` | Git source inventory |
| Package manifests | Both JSON files parse successfully | `Packages/manifest.json`, `Packages/packages-lock.json` |

Build output: `Builds/Vaultbreakers_Sync_20260908/Vaultbreakers.exe`.
The executable was built but not launched for a human playtest.

Raw logs, test XML, build output, and local caches remain on the workstation and are
ignored by Git. This summary preserves the results in source control. The setup tool
was not rerun because its regeneration would rewrite the existing scenes and prefab;
the tests exercised the assets being preserved.

Git's whitespace check passes for code and documentation. Unity-generated YAML contains
its usual empty serialized values with trailing spaces, and the existing `.slnx` has
CRLF lines. Those generated files were preserved rather than reformatted.

## Remaining validation

The deferred Phase 3, 5, 6, 7, and 8 human feel gates remain open. Physical controller
input/reconnection, visual and audio readability, performance on a graphics device,
and development-build playtesting were not exercised by this batch run. Distinguishing
the tactical value of dodge from shielding still needs the Phase 9 enemies.

## Licensing and recovery

The root `LICENSE` reserves rights to original project materials under the repository's
existing author identity, StarbugStone. Third-party exceptions and provenance are
recorded in `LICENSES/THIRD_PARTY_ASSETS.md`; the upstream `.gitattributes` MIT notice is
retained separately. Repository visibility remains public.

The README documents cloning with Git LFS and opening the pinned Unity version. Source,
Unity metadata, package manifests, project settings, Blender source, and documentation
are included in this checkpoint; generated caches and diagnostic files are not required
to recover the project.
