# Third-Party Assets

No third-party assets are stored in this repository.
Each team member downloads them locally into `Assets/ThirdParty/` (ignored by Git).

## How to add a new asset
1. Import it into `Assets/ThirdParty/<AssetName>/`.
   If the asset comes from the Asset Store, move it there **inside the Unity Editor** (not in Finder).
2. If the asset is **not** from the Asset Store (e.g. a zip from a website), export it once as a
   `.unitypackage` (right-click the folder -> Export Package), upload it to the team Drive and put that
   link in the table. This keeps asset GUIDs identical for everyone, so scene references don't break.
3. Add a row to the table below in the same commit and tell the team.

## Rules
- Do not edit files inside `Assets/ThirdParty/`. Create Prefab/Material Variants under `Assets/_Project/` instead.
- Everyone must use the exact version listed here.

## Assets
| Asset | Source | Version | License | Download | Folder |
|---|---|---|---|---|---|
