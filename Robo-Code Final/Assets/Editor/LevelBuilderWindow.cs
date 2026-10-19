using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Editor tool for painting the Level 2 terrain, so the blocks land on exact
// coordinates instead of being dragged by hand.
// Open it from the menu bar: Tools > Level Builder
public class LevelBuilderWindow : EditorWindow
{
    private Tilemap ground;
    private TileBase surfaceTile; // the top row you walk on
    private TileBase fillTile;    // everything underneath it

    // each row is one solid block: left, right, top, bottom
    // these match the layout plan
    private static readonly int[,] blocks =
    {
        {  0,  18,  -6, -12 },   // start, flat ground
        { 22,  28,  -6, -12 },   // first jump
        { 32,  38,  -5, -12 },   // second jump
        { 42,  56,  -4, -12 },   // landing before the pit
        { 62,  79,   3, -12 },   // high ledge, reached by the elevator
        { 80, 125,  -4, -12 }    // enemy run into the shop
    };

    [MenuItem("Tools/Level Builder")]
    private static void Open()
    {
        GetWindow<LevelBuilderWindow>("Level Builder");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Paints the Level 2 terrain", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        ground = (Tilemap)EditorGUILayout.ObjectField("Ground Tilemap", ground, typeof(Tilemap), true);
        surfaceTile = (TileBase)EditorGUILayout.ObjectField("Surface Tile", surfaceTile, typeof(TileBase), false);
        fillTile = (TileBase)EditorGUILayout.ObjectField("Fill Tile", fillTile, typeof(TileBase), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Find the tilemap for me"))
        {
            AutoFind();
        }

        if (GUILayout.Button("2. Clear all tiles"))
        {
            Clear();
        }

        if (GUILayout.Button("3. Build the level"))
        {
            Build();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Drag a tile into each slot from the Project panel.\n" +
            "Surface Tile is the one with the coloured top edge.\n" +
            "Fill Tile is the plain stone one.\n" +
            "Leave Surface empty to use the fill tile everywhere.",
            MessageType.Info);
    }

    // saves hunting through the project for the tile asset by grabbing one
    // that is already sitting on the tilemap
    private void AutoFind()
    {
        if (ground == null)
        {
            foreach (Tilemap found in FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
            {
                if (found.name == "Ground")
                {
                    ground = found;
                    break;
                }
            }
        }

        if (ground == null)
        {
            Debug.LogWarning("Level Builder: no tilemap called Ground in this scene.");
            return;
        }

    }

    private void Clear()
    {
        if (ground == null)
        {
            Debug.LogWarning("Level Builder: set the Ground Tilemap first.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(ground, "Clear level");
        ground.ClearAllTiles();
        MarkDirty();
    }

    private void Build()
    {
        if (ground == null || fillTile == null)
        {
            Debug.LogWarning("Level Builder: set the Ground Tilemap and the Fill Tile first.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(ground, "Build level");

        for (int i = 0; i < blocks.GetLength(0); i++)
        {
            int left = blocks[i, 0];
            int right = blocks[i, 1];
            int top = blocks[i, 2];
            int bottom = blocks[i, 3];

            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y <= top; y++)
                {
                    // the top row of each block is the bit the player walks on,
                    // so it gets the surface tile and the rest gets plain fill
                    bool isTopRow = (y == top);
                    TileBase use = (isTopRow && surfaceTile != null) ? surfaceTile : fillTile;
                    ground.SetTile(new Vector3Int(x, y, 0), use);
                }
            }
        }

        MarkDirty();
        Debug.Log("Level Builder: done. Move the Player to (3, -4) and press Play.");
    }

    // tells Unity the scene changed so the edit actually gets saved
    private void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
    }
}