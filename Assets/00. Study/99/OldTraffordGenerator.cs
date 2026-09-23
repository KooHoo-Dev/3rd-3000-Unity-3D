#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class OldTraffordGenerator : MonoBehaviour
{
    // Context Menu: Right-click in Hierarchy -> 3D Object -> Old Trafford
    [MenuItem("GameObject/3D Object/Old Trafford (Theatre of Dreams)", false, 10)]
    public static void GenerateOldTrafford(MenuCommand menuCommand)
    {
        // Root Object
        GameObject stadiumRoot = new GameObject("Old_Trafford_Stadium");
        GameObjectUtility.SetParentAndAlign(stadiumRoot, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(stadiumRoot, "Create Old Trafford");

        // Common Materials
        Material matGrass = CreateEngineMaterial("Mat_PitchGrass", new Color(0.18f, 0.48f, 0.18f), 0.1f);
        Material matUnitedRed = CreateEngineMaterial("Mat_SeatsRed", new Color(0.78f, 0.06f, 0.18f), 0.3f);
        Material matRoofWhite = CreateEngineMaterial("Mat_RoofWhite", new Color(0.92f, 0.92f, 0.94f), 0.2f);
        Material matSteelTruss = CreateEngineMaterial("Mat_SteelTruss", new Color(0.85f, 0.85f, 0.85f), 0.6f);
        Material matGlass = CreateEngineMaterial("Mat_FacadeGlass", new Color(0.2f, 0.35f, 0.45f, 0.7f), 0.9f, true);
        Material matPitchLine = CreateEngineMaterial("Mat_PitchLines", Color.white, 0.1f);
        Material matConcrete = CreateEngineMaterial("Mat_Concrete", new Color(0.5f, 0.5f, 0.52f), 0.1f);

        // 1. PITCH & FIELD AREA (Real dimensions: 105m x 68m)
        GameObject pitchArea = new GameObject("01_Pitch_Area");
        pitchArea.transform.SetParent(stadiumRoot.transform, false);

        // Pitch Base Plane
        CreateBox("Pitch_Turf", pitchArea.transform, new Vector3(0, 0, 0), new Vector3(105, 0.2f, 68), matGrass);
        // Concrete Run-off apron
        CreateBox("Apron_Runoff", pitchArea.transform, new Vector3(0, -0.05f, 0), new Vector3(120, 0.15f, 82), matConcrete);

        // Markings & Goals
        CreatePitchMarkings(pitchArea.transform, matPitchLine);
        CreateGoal(pitchArea.transform, new Vector3(-52.5f, 0, 0), 1, matPitchLine);
        CreateGoal(pitchArea.transform, new Vector3(52.5f, 0, 0), -1, matPitchLine);

        // 2. STANDS
        GameObject standsGroup = new GameObject("02_Stands");
        standsGroup.transform.SetParent(stadiumRoot.transform, false);

        // NORTH STAND (Sir Alex Ferguson Stand - 3 Tiers, Tallest, ~42m high)
        BuildNorthStand(standsGroup.transform, matUnitedRed, matRoofWhite, matSteelTruss, matConcrete);

        // SOUTH STAND (Sir Bobby Charlton Stand - Single/Double lower tier, Player Tunnel & Dugouts)
        BuildSouthStand(standsGroup.transform, matUnitedRed, matRoofWhite, matConcrete);

        // WEST STAND (Stretford End - 2 Large Tiers)
        BuildWestStand(standsGroup.transform, matUnitedRed, matRoofWhite, matConcrete);

        // EAST STAND (Glass Facade, 2 Tiers)
        BuildEastStand(standsGroup.transform, matUnitedRed, matRoofWhite, matGlass, matConcrete);

        // 3. NORTH CORNER QUADRANTS (NW & NE upper fills)
        BuildCornerQuadrants(standsGroup.transform, matUnitedRed, matRoofWhite);

        Selection.activeObject = stadiumRoot;
    }

    // --- NORTH STAND (SIR ALEX FERGUSON STAND) ---
    private static void BuildNorthStand(Transform parent, Material matSeats, Material matRoof, Material matTruss, Material matWall)
    {
        GameObject stand = new GameObject("Sir_Alex_Ferguson_Stand_North");
        stand.transform.SetParent(parent, false);

        // Tier 1 (Lower)
        CreateBox("Tier_1", stand.transform, new Vector3(0, 3f, 43f), new Vector3(105, 6, 10), matSeats);
        // Tier 2 (Middle)
        CreateBox("Tier_2", stand.transform, new Vector3(0, 11f, 52f), new Vector3(108, 10, 10), matSeats);
        // Tier 3 (Upper - Massive tier)
        CreateBox("Tier_3", stand.transform, new Vector3(0, 23f, 63f), new Vector3(112, 14, 14), matSeats);

        // Back Wall
        CreateBox("Back_Wall", stand.transform, new Vector3(0, 17f, 70.5f), new Vector3(115, 36, 2), matWall);

        // Cantilever Roof
        CreateBox("Cantilever_Roof", stand.transform, new Vector3(0, 36f, 48f), new Vector3(116, 1.2f, 46), matRoof);

        // Iconic Cantilever White External Steel Trusses
        for (float x = -48; x <= 48; x += 24)
        {
            GameObject truss = CreateBox($"Roof_Truss_Girder_{x}", stand.transform, new Vector3(x, 40f, 54f), new Vector3(1.5f, 7f, 36f), matTruss);
            truss.transform.rotation = Quaternion.Euler(-12, 0, 0);
        }
    }

    // --- SOUTH STAND (SIR BOBBY CHARLTON STAND) ---
    private static void BuildSouthStand(Transform parent, Material matSeats, Material matRoof, Material matWall)
    {
        GameObject stand = new GameObject("Sir_Bobby_Charlton_Stand_South");
        stand.transform.SetParent(parent, false);

        // Tier 1 & 2 (Lower height due to railway behind it)
        CreateBox("Tier_1", stand.transform, new Vector3(0, 2.5f, -42.5f), new Vector3(100, 5, 9), matSeats);
        CreateBox("Tier_2", stand.transform, new Vector3(0, 9f, -50f), new Vector3(102, 8, 8), matSeats);

        // Back Wall
        CreateBox("Back_Wall", stand.transform, new Vector3(0, 8f, -54.5f), new Vector3(104, 18, 2), matWall);

        // Roof
        CreateBox("Cantilever_Roof", stand.transform, new Vector3(0, 18f, -44f), new Vector3(106, 0.8f, 22), matRoof);

        // Dugouts & Player Tunnel
        CreateBox("Dugout_Home", stand.transform, new Vector3(-8, 0.8f, -36f), new Vector3(7, 1.6f, 2.5f), matWall);
        CreateBox("Dugout_Away", stand.transform, new Vector3(8, 0.8f, -36f), new Vector3(7, 1.6f, 2.5f), matWall);
        CreateBox("Player_Tunnel", stand.transform, new Vector3(0, 0.6f, -38f), new Vector3(4, 1.4f, 7f), matWall);
    }

    // --- WEST STAND (STRETFORD END) ---
    private static void BuildWestStand(Transform parent, Material matSeats, Material matRoof, Material matWall)
    {
        GameObject stand = new GameObject("Stretford_End_West");
        stand.transform.SetParent(parent, false);

        CreateBox("Tier_1", stand.transform, new Vector3(-58f, 4f, 0), new Vector3(9, 8, 68), matSeats);
        CreateBox("Tier_2", stand.transform, new Vector3(-66f, 13f, 0), new Vector3(9, 12, 70), matSeats);
        CreateBox("Back_Wall", stand.transform, new Vector3(-71f, 13f, 0), new Vector3(2, 28, 72), matWall);

        // Sloped Roof
        CreateBox("Roof", stand.transform, new Vector3(-58f, 28f, 0), new Vector3(26, 0.8f, 74), matRoof);
    }

    // --- EAST STAND ---
    private static void BuildEastStand(Transform parent, Material matSeats, Material matRoof, Material matGlass, Material matWall)
    {
        GameObject stand = new GameObject("East_Stand");
        stand.transform.SetParent(parent, false);

        CreateBox("Tier_1", stand.transform, new Vector3(58f, 4f, 0), new Vector3(9, 8, 68), matSeats);
        CreateBox("Tier_2", stand.transform, new Vector3(66f, 13f, 0), new Vector3(9, 12, 70), matSeats);
        
        // Iconic Glass Exterior Facade (East forecourt)
        CreateBox("East_Glass_Facade", stand.transform, new Vector3(71f, 13f, 0), new Vector3(1.5f, 27, 72), matGlass);

        // Roof
        CreateBox("Roof", stand.transform, new Vector3(58f, 28f, 0), new Vector3(26, 0.8f, 74), matRoof);
    }

    // --- CORNER QUADRANTS (NW and NE upper sections) ---
    private static void BuildCornerQuadrants(Transform parent, Material matSeats, Material matRoof)
    {
        GameObject corners = new GameObject("Quadrants_North_Corners");
        corners.transform.SetParent(parent, false);

        // North-West Quadrant
        GameObject nw = CreateBox("Quadrant_NW", corners.transform, new Vector3(-54f, 20f, 48f), new Vector3(16, 16, 16), matSeats);
        nw.transform.rotation = Quaternion.Euler(0, 45, 0);

        // North-East Quadrant
        GameObject ne = CreateBox("Quadrant_NE", corners.transform, new Vector3(54f, 20f, 48f), new Vector3(16, 16, 16), matSeats);
        ne.transform.rotation = Quaternion.Euler(0, -45, 0);
    }

    // --- PITCH MARKINGS & GOALS ---
    private static void CreatePitchMarkings(Transform parent, Material matLine)
    {
        GameObject lines = new GameObject("Pitch_Markings");
        lines.transform.SetParent(parent, false);

        // Center line & Center Circle
        CreateBox("Center_Line", lines.transform, new Vector3(0, 0.12f, 0), new Vector3(0.3f, 0.05f, 68), matLine);
        GameObject centerCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        centerCircle.name = "Center_Spot";
        centerCircle.transform.SetParent(lines.transform, false);
        centerCircle.transform.position = new Vector3(0, 0.13f, 0);
        centerCircle.transform.localScale = new Vector3(18.3f, 0.01f, 18.3f);
        centerCircle.GetComponent<Renderer>().sharedMaterial = matLine;

        // Penalty boxes
        CreateBox("PenaltyBox_West", lines.transform, new Vector3(-44.25f, 0.12f, 0), new Vector3(16.5f, 0.02f, 40.3f), matLine);
        CreateBox("PenaltyBox_East", lines.transform, new Vector3(44.25f, 0.12f, 0), new Vector3(16.5f, 0.02f, 40.3f), matLine);
    }

    private static void CreateGoal(Transform parent, Vector3 pos, float dir, Material matLine)
    {
        GameObject goal = new GameObject(dir > 0 ? "Goal_West" : "Goal_East");
        goal.transform.SetParent(parent, false);
        goal.transform.position = pos;

        // Post L, Post R, Crossbar
        CreateBox("Post_Left", goal.transform, new Vector3(0, 1.22f, -3.66f), new Vector3(0.2f, 2.44f, 0.2f), matLine);
        CreateBox("Post_Right", goal.transform, new Vector3(0, 1.22f, 3.66f), new Vector3(0.2f, 2.44f, 0.2f), matLine);
        CreateBox("Crossbar", goal.transform, new Vector3(0, 2.44f, 0), new Vector3(0.2f, 0.2f, 7.52f), matLine);
    }

    // --- HELPER CREATION METHODS ---
    private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = size;
        if (mat != null) cube.GetComponent<Renderer>().sharedMaterial = mat;
        return cube;
    }

    private static Material CreateEngineMaterial(string name, Color color, float smoothness, bool transparent = false)
    {
        // Try Universal Render Pipeline/Lit first; fallback to Standard (Built-in)
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        if (transparent)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // URP transparent
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        return mat;
    }
}
#endif