# Networking Guide

This guide is for everyone working on BeyondLimits. Rabia owns the networking layer; you use it. Writing your own gameplay systems (building, inventory, characters, AI) in a network-ready way is your job, and this guide covers what you need for that.

A Turkish version of this guide is shared in the team docs. Both have the same content.

## Who does what

| Task | Owner |
| --- | --- |
| Netcode packages and versions | Rabia |
| NetworkManager and DebugUI prefabs | Rabia |
| Network skeleton of the Player prefab (NetworkObject, NetworkTransform, movement) | Rabia |
| Connection: Relay, sessions, join codes, main menu | Rabia |
| Shared components: Health, IDamageable, Character | Rabia |
| Review of every PR that touches networking | Rabia |
| Writing your own system in a network-ready way | System owner |
| Registering your own networked prefabs | Prefab owner |
| Testing your own feature in multiplayer | Feature owner |
| Gameplay on the Player prefab (animation, attacks) | Character owner |

## 1. Setup

You never install networking packages yourself. They come from the repo through `Packages/manifest.json`.

### One-time setup (skip if done in Phase 0)

- [ ] Unity **6000.3.26f1** is installed
- [ ] `git lfs install` has been run once
- [ ] Smart Merge (UnityYAMLMerge) is configured in `~/.gitconfig`

### After every networking update

1. Close Unity.
2. Run `git switch main` and `git pull`.
3. If you work on your own branch: `git switch <your-branch>` and `git merge main`.
4. Open Unity. New packages install automatically; the first launch can take a few minutes.
5. Check that the Console has no red errors.

Package Manager shows a "not officially supported" notice next to Netcode for GameObjects 2.13.3. This is expected: 2.13.3 is the version our editor recommends. See `docs/adr/0001-netcode-version.md`. **Do not press Update.**

## 2. Testing multiplayer in your own scene

Every feature is tested in two windows: one host and one client. A feature that works for one player may not work over the network.

### Prepare the scene (once per scene)

1. Open your own test scene (`Assets/_Project/Scenes/TestScenes/<your-name>-...`).
2. Drag the `Assets/_Project/Prefabs/Network/NetworkManager` prefab into the Hierarchy.
3. Drag the `DebugUI` prefab from the same folder.
4. Add your scene to **File > Build Profiles > Scene List** (drag it into the list). Netcode only syncs scenes that are in this list.
5. If the scene already contains a player, delete it. NetworkManager spawns a player for everyone who connects.
6. Save the scene.

Do not change NetworkManager settings in your scene. If you need a different setting, ask Rabia.

### Run the test

1. Open **Window > Multiplayer > Multiplayer Play Mode**.
2. Tick **Player 2** and wait until it becomes active. This must happen before you press Play.
3. Press Play. Player 2 opens in a separate window.
4. Click **Host** in the main editor and **Client** in the Player 2 window.
5. Try your feature **in both windows**.

### Checklist for every feature

- [ ] Works on the host
- [ ] Works on the client
- [ ] When one player does something, the other sees the result
- [ ] A late-joining client sees the world correctly (wait a while after Play, then click Client)
- [ ] Works with simulated latency (below)
- [ ] No red errors in the Console

### Latency test

Latency on your own machine is zero; 50-150 ms is normal on the real internet. Set the latency to 100-150 ms in the **Network Simulator** component on the NetworkManager in your scene and repeat the test. Do not commit this change.

## 3. Writing network-ready code

One rule: **the server makes every decision that affects more than one player.** The goal is consistency, not cheat prevention: everyone sees the same world, and two requests arriving at the same time never conflict. The four concepts below are how you apply this rule.

### Concept 1: Where does this code run? (`IsServer`, `IsOwner`)

Every player's machine runs its own copy of the game. Networked scripts inherit from `NetworkBehaviour` instead of `MonoBehaviour` and get access to:

| Property | `true` when | Use it for |
| --- | --- | --- |
| `IsServer` | The code runs on the server (the host) | Code that changes game state |
| `IsOwner` | This object belongs to this machine's player | Reading input, camera |
| `IsClient` | The code runs on a player's machine | Visuals, sound, effects |

Do not use `IsHost`; always write `IsServer` for server logic. Let method names say where they run: `ServerApplyDamage`, `ClientPlayHitEffect`.

### Concept 2: Request -> validate -> apply

1. The **client** sends a request: "I want to chop this tree".
2. The **server** validates it: Is the player close enough? Is anything left?
3. The **server** applies it: it changes the state.
4. **Everyone** sees the result: the NetworkVariable syncs the new value.

```csharp
public class ResourceNode : NetworkBehaviour
{
    // Everyone can read it, only the server can change it.
    private readonly NetworkVariable<int> remaining = new(10);

    // Called on the client when the player presses the gather key.
    public void TryGather() => RequestGatherRpc();

    // Runs on the server. RpcParams tells us who sent it.
    [Rpc(SendTo.Server)]
    private void RequestGatherRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (remaining.Value <= 0) return;           // validate
        if (!IsCloseEnough(sender)) return;         // validate
        remaining.Value--;                          // apply
        // Give the item to the sender's inventory (server-side).
    }
}
```

A client never says "I added one log to my inventory". It only asks; the server decides what it gets.

### Concept 3: NetworkVariable or RPC?

Rule of thumb: **"Does a player who joins later need to know this?"** Yes: NetworkVariable. No: RPC.

| | NetworkVariable | RPC |
| --- | --- | --- |
| Used for | State: health, remaining resources, is the door open | Events: "I attacked", play an effect |
| Seen by late joiners | Yes, they get the current value | No, the event is in the past |
| Who changes it | Only the server, by default | Requests: client -> server; events: server -> everyone |

### Concept 4: Only the server spawns and despawns

Only the server creates and destroys objects that everyone should see (dropped items, built walls, enemies):

```csharp
// Server only.
GameObject wall = Instantiate(wallPrefab, position, rotation);
wall.GetComponent<NetworkObject>().Spawn();   // everyone sees it

// Later, server only.
wall.GetComponent<NetworkObject>().Despawn();  // gone for everyone
```

An object created with `Instantiate` on a client exists only on that client. An object removed with `Destroy` on a client disappears only on that client.

### What can stay on the client

Things that concern a single player do not go through the server: your own player's movement, the camera, menus and UI, the building preview (ghost), sound and particle effects.

## 4. Adding a networked prefab

Every prefab the server spawns during play (walls, enemies, dropped items, animals) must be registered. The prefab's owner does this in their own PR.

1. Open the prefab in Prefab Mode (double-click it in the Project window).
2. Add `NetworkObject` to the **root object**. A prefab has exactly one, never on child objects.
3. If the object moves during play, add `NetworkTransform`. Sync only the axes that change and enable **Use Unreliable Deltas** (it handles packet loss much better; see ADR 0002). If the object does not move (walls, rocks), leave `NetworkTransform` out; it would only send unnecessary data.
4. Click `Assets/DefaultNetworkPrefabs` in the Project window. Drag your prefab into its **List** field in the Inspector.
5. Spawn it from the server in your test scene and check it in both windows.

Why the list? When the server says "spawn this prefab", the client looks it up in this list. A prefab that is not in the list cannot be created on the client and causes an error.

**Do not move or rename** `DefaultNetworkPrefabs`. Netcode looks for it in the `Assets/` root; if it is missing, Netcode creates a new one and NetworkManager loses its reference. It is the only file that does not follow our folder structure.

## 5. Do's and don'ts

### Don't touch (ask Rabia if you need a change)

- Networking packages and their versions (installing, updating or removing them in Package Manager)
- Settings of the `NetworkManager` prefab
- Location and name of `Assets/DefaultNetworkPrefabs` (adding your own prefabs to it is fine)
- The `Assets/_Project/Scripts/Networking` folder
- The `NetworkObject`, `NetworkTransform` and `PlayerMovement` components on the Player prefab

### Do

- Guard code that changes game state with `IsServer`.
- Validate every client request on the server.
- Spawn and despawn networked objects only on the server.
- Add new networked prefabs to `DefaultNetworkPrefabs`.
- If you are unsure, open the PR anyway and add Rabia as a reviewer.

### PR checklist

Copy this list into the description of any PR that touches networking and tick it:

```markdown
- [ ] Tested as host
- [ ] Tested as client
- [ ] Tested with a late-joining client
- [ ] Tested with 100-150 ms simulated latency
- [ ] New networked prefabs added to DefaultNetworkPrefabs
- [ ] No Console errors
```

Add Rabia under **Reviewers**.

## 6. Troubleshooting

| Symptom | Likely cause and fix |
| --- | --- |
| An object shows up in only one window | It was created with `Instantiate` on a client. Create it on the server and call `Spawn()`. |
| Error saying the prefab is not registered when spawning | The prefab is missing from `DefaultNetworkPrefabs`. See section 4. |
| Error about a missing NetworkObject | Add `NetworkObject` to the prefab's root object. |
| A value changes on the host but not on the client | The value is stored in a normal field. Use a `NetworkVariable`. |
| Error when a client tries to change a NetworkVariable | Only the server can write it. The client should ask through an RPC and let the server change it. |
| A late-joining player sees the world wrong | State is sent with an RPC. Keep state in a `NetworkVariable`. |
| Pressing a key moves every player | The input code is missing an `IsOwner` check. |
| The client cannot connect | Client was clicked before the host started. Click Host first, then Client. |
| The Player 2 window does not open | Player 2 must be enabled in the Multiplayer Play Mode window before you press Play. |
| Netcode compile errors after pulling | Restart Unity and wait for packages to install. If it persists, contact Rabia. |

If you find a new problem and its fix, add it to this table.
