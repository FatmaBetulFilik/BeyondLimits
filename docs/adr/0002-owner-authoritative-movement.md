# ADR 0002: Owner-authoritative player movement

- **Status:** Accepted
- **Date:** 2026-10-20

## Context

BeyondLimits is a co-op game: all players are on the same team and enemies are controlled by AI. There is no PvP. Player movement must feel responsive for every player, including clients that connect through Unity Relay with 30-100 ms of latency.

Netcode for GameObjects offers two ways to sync a player's position with `NetworkTransform`:

1. **Owner-authoritative:** each client moves its own player and the position is synced to everyone else.
2. **Server-authoritative:** clients send input to the server, which moves the player. Without client-side prediction this adds a full round trip of delay to every key press; with prediction it is much harder to build.

## Decision

Player movement is **owner-authoritative**:

- `NetworkTransform` on the Player prefab uses **Authority Mode: Owner**.
- `PlayerMovement` reads input and moves the player only when `IsOwner` is true.
- Only position (X, Y, Z) and rotation around Y are synced; scale and X/Z rotation are not.
- **Interpolate** is enabled so remote players move smoothly between updates.
- **Use Unreliable Deltas** is enabled. With the "Mobile 4G" Network Simulator preset (100 ms delay, 4% packet loss), reliable delivery caused visible stutter because a lost packet blocked the updates behind it. Unreliable deltas removed the stutter.

Everything else that affects more than one player (damage, health, inventory, gathering, building, AI) stays server-authoritative.

## Consequences

- Each player's own movement has no input delay.
- Remote players are shown slightly behind their real position. This is expected and smoothed by interpolation.
- A client could report a wrong position. In a co-op game this does not harm other players, so we only log suspicious movement on the server and do not correct it.
- Teleports (such as respawning) must use `NetworkTransform.Teleport()` so remote copies do not slide between the two positions.
