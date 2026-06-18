# Contributing to Visual State Machine V3

Thanks for taking the time to contribute! VSM3 is a Unity package (UPM), and the conventions below keep the codebase consistent and the editor/runtime boundary clean.

## Getting set up

1. Fork the repository and clone your fork.
2. Add the package to a Unity project (2022.3 LTS or newer; Unity 6 recommended) — e.g. via **Package Manager -> Add package from disk...** pointing at the cloned `package.json`, or as a local `file:` dependency in your project's `Packages/manifest.json`.
3. Branch off `main`:
   - `feat/<short-name>` for features
   - `fix/<short-name>` for bug fixes

## Making changes

- Keep runtime code in `Runtime/` and editor-only code in `Editor/`. Editor assemblies may reference Runtime; **never the reverse**.
- Add or update tests under `Tests/EditMode` (editor/utility logic) or `Tests/PlayMode` (runtime behaviour). Both are exposed via the package's `testables`.
- Update `README.md` and `CHANGELOG.md` when you change behaviour, public API, or the editor UI.

## Coding standards

Full standard: <https://github.com/PaulNonatomic/CodingStandards>

The points that matter most in this codebase:

- **One `State` subclass per file**, named after the class. Unity's `MonoScript.GetClass()` returns only the first class in a file, which breaks rename-safe GUID type resolution — bundled state files won't be accepted.
- `PascalCase` for types/methods/properties; `I`-prefixed interfaces; `_camelCase` private fields; `camelCase` locals and parameters. Spell names out.
- Tabs (4-wide), Allman braces, explicit access modifiers, `var` when the type is apparent.
- No `#region` directives.
- Prefer guard clauses / early returns; follow SOLID, especially Single Responsibility.
- Prefer `async`/`await` over coroutines; use `[field: SerializeField]` to expose auto-property backing fields.

## Opening a pull request

1. Push your branch and open a PR against `main`.
2. Fill in the PR template.
3. PRs are reviewed before merging and are **squash-merged**, so each PR becomes a single commit on `main` — keep it focused. Resolve review threads before merge.

## Commit messages

Short, imperative subject lines (e.g. "Fix portal node orphan detection"). Add a body only when the change needs explaining.

## Licence

VSM3 is released under the MIT licence. By contributing, you agree that your contributions are licensed under the same terms.
