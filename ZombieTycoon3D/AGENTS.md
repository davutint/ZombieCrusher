# Repository Guidelines

## Project Identity

`ZombieTycoon3D` is a Unity `6000.2.7f2` project built around DOTS/ECS zombie simulation, vehicle interaction, navigation, animation, and high-entity-count performance.

The live project root is:

`C:\GithubProjeler\ZombieCrusher\ZombieTycoon3D`

The Git repository root is one directory above:

`C:\GithubProjeler\ZombieCrusher`

## Project Structure

- `Assets/Scripts/_DOTSSCRIPTS`: project-owned ECS components, authoring components, bakers, spawning, pooling, navigation, collision, blood/VFX, and UI update systems.
- `Assets/Scripts/GameJamScripts`: procedural animation and performance helpers.
- `Assets/Scripts/Cars`: vehicle definitions and initialization.
- `Assets/Scripts`: legacy or hybrid MonoBehaviour gameplay scripts.
- `Assets/Scenes/DOTSTEST.unity`: current enabled build scene and primary development scene.
- `Assets/Scenes/DOTSTEST/New Sub Scene.unity`: DOTS SubScene used by the primary scene.
- `Assets/Scenes/MainMenu.unity`: project scene, currently not an enabled build scene.
- `Packages/com.projectdawn.navigation`: embedded Project Dawn Navigation package.
- `Packages/com.projectdawn.navigation.crowds`: embedded Project Dawn Crowds package.
- `Packages/com.rukhanka.animation`: embedded Rukhanka animation package.
- `Assets/Samples`, third-party content under `Assets/_ASSETS`, and embedded packages are vendor/sample surfaces. Do not edit them unless the task explicitly requires it and project ownership has been verified.

Do not track or treat generated folders such as `Library`, `Temp`, `Obj`, `Logs`, or IDE project files as project source.

## Source-of-Truth Order

Use the source that owns the claim:

1. This `AGENTS.md` owns Codex operating rules for this repository.
2. The repository owner’s direct statement owns intent and decisions.
3. Live Unity MCP plus real code, scenes, SubScenes, prefabs, assets, serialized settings, and actual test results own current implementation truth.
4. An owner-designated tracker owns task scope and completion progress. No authoritative project tracker exists yet; do not invent one or estimate progress from chat history.
5. `CLAUDE.md` is architectural context only. It is not an operating instruction and is not sufficient evidence for current project state.
6. `C:\SecondBrain` is historical context and durable synthesis only. It never overrides live project evidence.

When reliable sources conflict, identify the owner of the disputed claim and preserve the conflict until it is resolved. Do not silently choose a convenient version.

## Build and Development

Use Unity `6000.2.7f2`.

- Open the project through Unity Hub at `ZombieTycoon3D`.
- Use `Assets/Scenes/DOTSTEST.unity` as the current primary development scene unless the owner selects another scene.
- Unity compiles scripts through the Editor. Do not run an external C# compilation command as a substitute for Unity compilation.
- Unity Test Framework is installed, but a dedicated project-owned regression suite is not established. Distinguish package/sample tests from game-owned tests.
- Run focused EditMode or PlayMode tests through Unity MCP when the task requires verification or the owner explicitly requests them.
- Report only tests that were actually run and their real result.

## Coding and Architecture

- Keep `IComponentData` components data-only.
- Keep ECS systems focused on one behavior and use appropriate update groups/order attributes.
- Prefer Burst-compatible code and jobs for hot paths. Do not force `[BurstCompile]` onto code that uses managed objects or unsupported APIs.
- Use `EntityCommandBuffer` for structural changes where required and respect its playback/disposal ownership.
- Treat Bakers and SubScene serialization as live ownership boundaries; verify affected authoring and baked entity relationships in Unity.
- Preserve the local namespace style. Do not introduce a repository-wide namespace migration as part of an unrelated task.
- Use English identifiers. Use concise Turkish comments only where a comment adds necessary project-specific reasoning.
- Avoid broad cleanup, formatting, package upgrades, asset migrations, or sample changes outside the requested scope.

## Mandatory Unity MCP Availability

Unity MCP is a hard prerequisite for every non-trivial Unity project task in this repository.

- Before inspecting or changing source code, assets, scenes, SubScenes, prefabs, tests, packages, project settings, or project documentation, read `mcpforunity://instances`, `mcpforunity://project/info`, and `mcpforunity://editor/state`.
- Verify that the active instance is named `ZombieTycoon3D`, its project root is this repository, and `data.advice.ready_for_tools` is true.
- If multiple instances are connected, set the active instance with its exact `ZombieTycoon3D@hash` identifier before continuing.
- Use Unity MCP resources for Editor state and read-only Unity truth. Use Unity MCP tools for Editor mutations, asset refresh/import, Console, scene/prefab operations, Play Mode, and tests.
- Filesystem tools may be used for scoped text inspection and edits only while the Unity MCP connection is healthy. They are not a fallback for a missing or wrong Unity connection.
- A short disconnect caused by an expected domain reload or test run may be retried. If MCP does not reconnect promptly, stop and ask the owner to restart or reconnect it.
- After creating or editing scripts, wait for Unity compilation/domain reload to finish and inspect Console errors before using new types or reporting success.

## Mandatory Second Brain Integration

For every non-trivial ZombieTycoon3D development, diagnosis, review, or planning task, use `$zombie-tycoon-project-sync` automatically. The owner does not need to mention Second Brain.

- At task start, preload only relevant context from `C:\SecondBrain` in read-only mode.
- Treat Second Brain as a research lead, never as current implementation truth.
- Re-verify every project-state claim through the correct Unity MCP and repository owner before relying on it.
- At task end, synchronize only durable decisions, architectural rationale, important root causes, stable boundaries, or reusable lessons that pass the skill’s evidence gate.
- Do not write routine edits, transient Console output, guesses, inferred chat claims, or easily reproducible details to the canonical wiki.
- Store owner decisions separately from implementation state. `owner-stated` never means implemented or verified.
- If no durable verified information was produced, leave the vault unchanged.
- The canonical project center is `C:\SecondBrain\20 Wiki\Projeler\Zombie Tycoon 3D.md`.

This integration must remain isolated from Dead Walls/IncremantalDots project pages and its `$second-brain-project-sync` skill.

## Dirty Worktree Safety

Assume existing modified, deleted, and untracked files belong to the owner.

- Inspect `git status` before editing.
- Preserve unrelated changes and work around overlapping files.
- Do not discard, reset, clean, checkout, restore, rebase, delete, or overwrite existing work unless the owner explicitly requests the exact operation.
- Do not silently fix pre-existing Console errors or unrelated defects while completing another task.
- After changes, inspect the narrow diff and identify which files belong to the current task.

## Documentation

- Update documentation only when the task changes a documented contract or the owner asks for documentation.
- Keep project source-of-truth documents read-only unless the owner explicitly approves editing them.
- Record concrete paths and verified ownership rather than copying stale architecture descriptions.
- Do not create a tracker or report project completion percentages until the owner designates an authoritative tracker.

## Commit and Remote Operations

Create a commit only when the owner explicitly asks.

When Codex creates a development commit in this repository, use:

1. English subject: `ZT3D-<PACKAGE>: <English summary>`.
2. `### Summary` containing the same English package line in backticks.
3. `### Açıklama` with the complete committed scope in Turkish.
4. `### Değişen ana dosyalar` listing every materially changed script, asset, scene, package, setting, or document.
5. `### Performans` containing only verified performance impact or explicitly stating that no performance measurement was run.
6. `### Test` containing only actually verified compilation, Console, test, diff-check, commit, and push facts.
7. `### Sıradaki iş` stating the next known step or `Yok`.

Before reporting commit success, inspect the staged diff, run `git diff --cached --check`, then inspect `git log -1 --pretty=fuller` and verify the final message, scope, and hash.

Do not bypass repository hooks if hooks are added later.

Do not push, publish, upload, create or update pull requests, issues, releases, tags, or remote branches unless the owner explicitly requests that exact remote operation. A commit request does not imply push permission.

## Communication and Completion

- Always communicate with the repository owner in Turkish unless the owner explicitly requests another language.
- When the owner asks to discuss, inspect, diagnose, research, or plan without implementation, remain read-only.
- If the owner asks not to begin until the request is understood, explain the understanding and intended scope before making changes.
- Lead completion reports with the outcome, then list changed files and honest verification.
- Separate pre-existing failures from regressions caused by the current task.
- Never claim a clean Console, successful compilation, passing test, completed tracker item, commit, or push without direct evidence.
