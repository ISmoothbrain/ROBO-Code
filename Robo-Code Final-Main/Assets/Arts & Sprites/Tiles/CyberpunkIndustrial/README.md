# Cyberpunk Industrial — Unity 2D modular pixel-art set

This is an ORIGINAL 2D tileset designed from **three** supplied environment video references (two foreground environments, one city background). It does **not** use the excluded NPC video. It does not contain characters, NPCs, or textures reconstructed from the ZIP's missing sprite source files.

## Contents

- `Assets/CyberpunkIndustrial/Atlases/terrain_atlas.png` — **61** distinct 16×16 terrain cells, 12 columns on a transparent sheet. Core top, bottom, sides, 4 outer corners, 4 inner corners, various exposed-edge combinations, repeatable infill, wall panels, thin platforms, caps, and brackets.
- `Atlases/props_atlas.png` — **47** independent 16×16 industrial props and fittings, including a pipe network, lamps, vents, fans, ladders, cable sections and hazard warning strips.
- `Atlases/hazards_atlas.png` — **8** independent hazard sprites. Static sprites, not animation sheets.
- `Sprites/Terrain`, `Sprites/Props`, `Sprites/Hazards` — **116 individual transparent PNGs** matching every atlas cell byte-for-byte.
- `Backgrounds/01_sky.png`, `02_clouds.png`, `03_distant_city.png`, `04_near_industry.png` — **four separate, horizontally repeating 512×256 pixel layers**, independently usable.
- `Previews` — labelled tile guides and one 512×288 assembled sample + 2× nearest-neighbour enlarged preview. Labels are NOT part of sprite atlases.
- `tile_manifest.json` — names, atlas row/column, collision designation and pixel settings.
- `Scenes/sample_terrain_layout.json` — tile placements for the 32×18 top-left-origin sample, for reconstruction elsewhere.
- `Editor/CyberpunkIndustrialImporter.cs` — Unity Editor menu to import sprites and create actual Tile `.asset` files and a demo `.unity` scene.
- `Scripts/CyberpunkParallaxLayer.cs` — runtime parallax movement with horizontal wrap.

## Import into Unity

1. Extract the archive in the **root of your Unity project**, so the package folder is located at `Assets/CyberpunkIndustrial`.
2. After Unity finishes compiling, open **Tools → Cyberpunk Industrial → Build Tile Assets and Sample Scene**.
3. Wait for the importer to set **Sprite (2D and UI)**, **16 Pixels Per Unit**, **Point (no filter)**, **Uncompressed**, and **no mipmaps** on every individual sprite and background.
4. The Editor script creates `.asset` Tiles under `Assets/CyberpunkIndustrial/GeneratedTiles/` with **Grid collision for terrain only**, and saves `Assets/CyberpunkIndustrial/Scenes/IndustrialDemo.unity`.
5. Open the demo scene. Move the camera horizontally at runtime to see the four layers parallax-scroll and wrap. **No player/controller is included.** The fire/saw sprites have no damage or physics; add damage colliders/scripts to the separate Hazards tilemap or attach hazard prefab objects according to your game logic.
6. Create/edit your own Unity Tile Palette and drag imported Tiles from `GeneratedTiles` into the palette. The old `Tileset Palette` ZIP is only a structural reference; this tool does **not** overwrite those assets.

### Optional: use atlases directly

Select an atlas PNG, then set:

- Texture Type: **Sprite (2D and UI)**
- Sprite Mode: **Multiple**
- Pixels Per Unit: **16** (so 16 pixels = 1 world unit)
- Filter Mode: **Point (no filter)**; Compression: **None / Uncompressed**; Mipmaps: **off**
- Sprite Editor → Slice → **Grid by Cell Size** → Pixel Size **16×16**, Offset **0,0**, Padding **0,0**. Apply.

All foreground atlas dimensions are integer multiples of 16. Backgrounds must remain independent from the terrain and props. When building larger scenes, place solid ground on a collidable tilemap, props on another, and hazards on their own gameplay trigger/object layer.

## Design notes and scope

- The terrain is intentionally orthographic, with a small controlled steel/green/cyan palette. Foreground sprites have hard pixel edges, no filtered blur, gradients, or stamped-in text.
- **Inner corners are supplied as explicit inset-notch sprites**; select these manually where appropriate. Auto-tiling the demo uses cardinal exposed edge combinations, not diagonally-aware RuleTiles.
- Repeating edge tiles use a fixed 3-pixel green rim and separate dark border. Repeating top strips and side strips are reusable.
- Parallax backgrounds are 32 world units wide at 16 PPU. Their first and last pixel columns match exactly to support seamless looping.
- The demo scene proves art composition and tile placement. It is **not** a finished playable level or a replacement for your existing Unity level.
- The included PNGs and C# file contents have been validated structurally (RGBA grid dimensions and exact tile-to-atlas pixel comparisons); Unity execution/compilation was **not run here**.
