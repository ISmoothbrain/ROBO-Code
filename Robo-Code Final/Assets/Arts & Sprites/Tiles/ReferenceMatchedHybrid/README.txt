REFERENCE-MATCHED HYBRID DIRT + SCI-FI INDUSTRIAL TILESET
=====================================================
Artwork: Original hybrid remixes of user-supplied ground and sci-fi reference textures.
Reference A is the actual 32 x 32 source terrain: organic rounded rock, dark brown soil, green grass.
Reference B provides steel blue/navy panels, green trims, vents, bolts, and machinery inspiration.

CONTENTS
- 288 pixel-aligned 32x32 terrain tiles (5 stages x 32 tiles + 4 transition pairs x 32 tiles)
- 32 separate modular props/hazard fixtures
- RGBA atlas: 512 x 576 (16 columns x 18 rows, cell 32 x 32), transparent outside artwork
- Props RGBA atlas: 256 x 128 (8 columns x 4 rows, cell 32 x 32)
- 320 individually exported 32x32 transparent PNGs, JSON manifest, optional Unity Editor importer
- Labeled PREVIEW is informational, not the usable atlas

UNITY QUICK START
1. Extract the package and copy Assets/ReferenceMatchedHybrid into your project's Assets folder.
2. Wait for compilation.
3. Choose Tools > Reference Matched Hybrid > Build Unity Tiles.
4. GeneratedTiles contains Unity Tile assets. On a Grid component with cell size 1x1, paint using Tilemaps.
5. Use a terrain Tilemap with TilemapCollider2D; place decorations on a separate Tilemap. Configure hazards separately.
6. All individual sprites: Point filter, 32 Pixels Per Unit, Uncompressed, Mip Maps Off.
7. For slicing the atlas instead, Sprite Editor: Multiple > Grid by Cell Size > 32x32 > Offset 0,0 > Padding 0,0. (Unity bottom-left origin row ordering differs from manifest top-left.)
8. Tiles 000-031 pure ground, 032-063 light dirt, 064-095 hybrid, 096-127 heavy, 128-159 full metal. Tiles 160-287 are special transitions in groups of 32.
9. Cardinal exposed side mask (tile name edges_XX) bits: top=1, right=2, bottom=4, left=8. For example 00=interior, 01=top, 09=top-left corner, 15=isolated square.

IMPORTANT
Grid connectivity is encoded by neighboring exposed-side rules. For clean joins, choose edge masks from actual adjacent occupancy. Horizontal and vertical dirt/metal transitions have their own cells and should be placed between stages. Material transitions are deliberately rugged and pixel-hard, not antialiased. Unity importer not executed in this environment. There is no built-in Unity RuleTile dependency.
