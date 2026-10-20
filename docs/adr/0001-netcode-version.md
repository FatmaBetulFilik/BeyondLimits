# ADR 0001: Netcode for GameObjects version

- **Status:** Accepted
- **Date:** 2026-10-17

## Context

The game is a co-op survival game built with Unity 6.3 LTS (6000.3.26f1). We need a networking library for a client-server (host) topology with Unity Relay.

Netcode for GameObjects (NGO) is Unity's official networking library for GameObject-based projects. Two major versions were available when we made this decision:

- **NGO 2.13.x:** the current 2.x line, still maintained.
- **NGO 3.x:** released in September 2026. It adds Netcode for Entities, Entities, Burst and Collections as hard dependencies and renames several namespaces.

## Options

1. **NGO 2.13.3**
   - The Unity 6000.3.26f1 editor lists 2.13.3 as both the minimum and the recommended version (`Unity.app/Contents/Resources/PackageManager/Editor/manifest.json`).
   - Most tutorials, samples and forum answers target the 2.x line.
   - Package Manager shows a "not officially supported" notice for this editor, which conflicts with the editor's own recommendation.
2. **NGO 3.x**
   - Newer, but released for later editor versions, not for 6.3 LTS.
   - Brings the Entities stack into the project, which we do not use.
   - Very few learning resources yet.

## Decision

We use **Netcode for GameObjects 2.13.3**, the version the editor recommends for Unity 6000.3.26f1.

Installed together with it:

- `com.unity.transport` 2.7.4 (dependency of NGO)
- `com.unity.multiplayer.playmode` 2.0.2 (local multiplayer testing)

## Consequences

- Package versions are pinned in `Packages/manifest.json` and `Packages/packages-lock.json`. Teammates get the same versions after pulling.
- Package updates are made only through a pull request.
- We will not upgrade to NGO 3.x unless we also upgrade the Unity editor. That would be a separate decision with its own ADR.
- If we see unexplained networking errors, we will first check whether they relate to the "not officially supported" notice above.
