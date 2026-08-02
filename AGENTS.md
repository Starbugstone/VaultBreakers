# Vaultbreakers project rules

- The documents in `Docs/` are the main source of truth for product intent, architecture, implementation order, acceptance criteria, and asset conventions.
- Read the relevant documents before changing the project. If implementation and documentation disagree, follow the documentation unless the user explicitly changes the requirement; update stale documentation in the same change when appropriate.
- Follow `Docs/COMBAT_POC_PLAN.md` phase order and exit gates. Do not implement later-phase or post-POC systems speculatively.
- Follow the modular character contract in `Docs/MODULAR_AVATAR_PIPELINE.md` for skeletons, sockets, modules, imports, and validation.
- Preserve user-owned and unrelated working-tree changes. Never discard them to make a task clean.
- Do not add packages without explicit approval. Keep gameplay translation out of input readers, deterministic rules covered by tests, and generated Unity assets reproducible through the project setup tooling.
- After every coherent change, compile, run the relevant automated checks, inspect Unity logs, and report any validation that still requires a physical controller, graphics device, development build, or human playtest.

