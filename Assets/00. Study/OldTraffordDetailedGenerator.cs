#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Production-ready Unity Editor Tool to procedurally generate a high-fidelity 
/// 3D architectural model of Manchester United's Old Trafford stadium ("The Theatre of Dreams").
/// Fully supports Universal Render Pipeline (URP) with automatic Built-in Standard shader fallback.
/// </summary>
public static class OldTraffordDetailedGenerator
{
    private const string RootName = "Old_Trafford_Stadium_HD";

    // Pitch Official FIFA Dimensions
    private const float PitchLength = 105f;
    private const float PitchWidth = 68f;
    private const float HalfPitchLength = PitchLength * 0.5f; // 52.5m
    private const float HalfPitchWidth = PitchWidth * 0.5f;   // 34.0m

    // Shared Materials
    private static Material s_GrassMat;
    private static Material s_RedSeatMat;
    private static Material s_WhiteRoofMat;
    private static Material s_SteelTrussMat;
    private static Material s_DarkSteelMat;
    private static Material s_ConcreteMat;
    private static Material s_LightConcreteMat;
    private static Material s_GlassFacadeMat;
    private static Material s_PitchLineMat;
    private static Material s_GoalNetMat;
    private static Material s_FloodlightGlowMat;

    [MenuItem("GameObject/3D Object/Old Trafford (High Fidelity)", false, 10)]
    public static void GenerateOldTrafford()
    {
        // Remove existing instance if present
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        InitializeMaterials();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Old Trafford HD");

        Transform rootTransform = root.transform;
        rootTransform.position = Vector3.zero;
        rootTransform.rotation = Quaternion.identity;

        try
        {
            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Building Pitch and Field...", 0.1f);
            BuildPitchAndField(rootTransform);

            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Building Sir Alex Ferguson Stand (North)...", 0.25f);
            BuildSirAlexFergusonStand(rootTransform);

            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Building Stretford End (West)...", 0.45f);
            BuildStretfordEnd(rootTransform);

            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Building East Stand and Glass Facade...", 0.60f);
            BuildEastStand(rootTransform);

            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Building Sir Bobby Charlton Stand (South)...", 0.75f);
            BuildSirBobbyCharltonStand(rootTransform);

            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Constructing Quadrants (Corner Tiers)...", 0.85f);
            BuildQuadrants(rootTransform);

            EditorUtility.DisplayProgressBar("Generating Old Trafford", "Assembling Roof Cantilevers & Floodlights...", 0.95f);
            BuildRoofAndTrussSystem(rootTransform);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("<color=#C70101><b>[Old Trafford Generator]</b></color> Stadium generated successfully with full architectural hierarchy!");
    }

    [MenuItem("GameObject/3D Object/Old Trafford - Remove All Instances", false, 11)]
    public static void RemoveOldTrafford()
    {
#if UNITY_2023_1_OR_NEWER
        GameObject[] instances = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
        GameObject[] instances = UnityEngine.Object.FindObjectsOfType<GameObject>();
#endif
        int count = 0;
        foreach (GameObject go in instances)
        {
            if (go != null && go.name.StartsWith("Old_Trafford_Stadium"))
            {
                Undo.DestroyObjectImmediate(go);
                count++;
            }
        }
        Debug.Log($"[Old Trafford Generator] Removed {count} stadium instance(s).");
    }

    #region Material Management

    private static void InitializeMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        bool isURP = litShader != null;
        if (!isURP)
        {
            litShader = Shader.Find("Standard");
        }

        s_GrassMat = CreateMaterial("M_Pitch_Grass", new Color(0.20f, 0.45f, 0.15f), 0.15f, 0.0f, litShader, isURP);
        s_GrassMat.mainTexture = GenerateGrassStripeTexture();

        s_RedSeatMat = CreateMaterial("M_ManUtd_RedSeats", new Color(0.78f, 0.005f, 0.005f), 0.55f, 0.1f, litShader, isURP);
        s_WhiteRoofMat = CreateMaterial("M_Roof_Panels", new Color(0.92f, 0.93f, 0.95f), 0.35f, 0.05f, litShader, isURP);
        s_SteelTrussMat = CreateMaterial("M_Steel_WhiteTruss", new Color(0.95f, 0.95f, 0.95f), 0.65f, 0.4f, litShader, isURP);
        s_DarkSteelMat = CreateMaterial("M_Steel_Framework", new Color(0.22f, 0.24f, 0.26f), 0.5f, 0.6f, litShader, isURP);
        s_ConcreteMat = CreateMaterial("M_Stadium_Concrete", new Color(0.48f, 0.50f, 0.52f), 0.15f, 0.05f, litShader, isURP);
        s_LightConcreteMat = CreateMaterial("M_Light_Concrete", new Color(0.68f, 0.70f, 0.72f), 0.2f, 0.05f, litShader, isURP);
        s_PitchLineMat = CreateMaterial("M_Pitch_Lines", Color.white, 0.2f, 0.0f, litShader, isURP);

        // Tinted Glass
        s_GlassFacadeMat = CreateMaterial("M_Tinted_Glass", new Color(0.12f, 0.22f, 0.30f, 0.65f), 0.95f, 0.85f, litShader, isURP, transparent: true);

        // Goal Net
        s_GoalNetMat = CreateMaterial("M_Goal_Net", new Color(0.9f, 0.9f, 0.9f, 0.45f), 0.1f, 0.0f, litShader, isURP, transparent: true);

        // Floodlight Emissive
        s_FloodlightGlowMat = CreateMaterial("M_Floodlight_Glow", new Color(1.0f, 0.98f, 0.90f), 0.9f, 0.1f, litShader, isURP);
        if (isURP)
        {
            s_FloodlightGlowMat.EnableKeyword("_EMISSION");
            s_FloodlightGlowMat.SetColor("_EmissionColor", new Color(2.5f, 2.4f, 2.0f));
        }
        else
        {
            s_FloodlightGlowMat.EnableKeyword("_EMISSION");
            s_FloodlightGlowMat.SetColor("_EmissionColor", new Color(2.0f, 1.9f, 1.6f));
        }
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic, Shader shader, bool isURP, bool transparent = false)
    {
        Material mat = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.DontSave
        };

        if (isURP)
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
        }
        else
        {
            mat.color = color;
            mat.SetFloat("_Glossiness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (transparent)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
        }

        return mat;
    }

    private static Texture2D GenerateGrassStripeTexture()
    {
        int width = 128;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color darkGrass = new Color(0.18f, 0.42f, 0.14f);
        Color lightGrass = new Color(0.24f, 0.52f, 0.18f);

        int stripeHeight = 32;
        for (int y = 0; y < height; y++)
        {
            bool isDark = ((y / stripeHeight) % 2) == 0;
            Color baseCol = isDark ? darkGrass : lightGrass;

            for (int x = 0; x < width; x++)
            {
                float noise = (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 0.05f;
                Color finalCol = new Color(
                    Mathf.Clamp01(baseCol.r + noise),
                    Mathf.Clamp01(baseCol.g + noise),
                    Mathf.Clamp01(baseCol.b + noise),
                    1f
                );
                tex.SetPixel(x, y, finalCol);
            }
        }
        tex.Apply();
        return tex;
    }

    #endregion

    #region 01 Pitch and Field

    private static void BuildPitchAndField(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "01_Pitch_and_Field");

        // 1. Raised Main Playing Turf
        ProceduralMeshBuilder pitchBuilder = new ProceduralMeshBuilder();
        pitchBuilder.AddBox(
            new Vector3(0f, 0.25f, 0f),
            new Vector3(PitchLength, 0.5f, PitchWidth),
            Quaternion.identity,
            new Vector2(2f, 16f)
        );
        CreateMeshObject("Pitch_PlayingSurface", pitchBuilder.ToMesh("PitchMesh"), s_GrassMat, group.transform);

        // 2. Old Trafford's iconic beveled perimeter slope
        ProceduralMeshBuilder apronBuilder = new ProceduralMeshBuilder();
        float apronW = PitchLength + 10f;
        float apronD = PitchWidth + 10f;

        // North slope
        apronBuilder.AddQuad(
            new Vector3(-HalfPitchLength - 5f, -0.4f, HalfPitchWidth + 5f),
            new Vector3(-HalfPitchLength, 0.25f, HalfPitchWidth),
            new Vector3(HalfPitchLength, 0.25f, HalfPitchWidth),
            new Vector3(HalfPitchLength + 5f, -0.4f, HalfPitchWidth + 5f),
            Vector2.zero, Vector2.one
        );
        // South slope
        apronBuilder.AddQuad(
            new Vector3(HalfPitchLength + 5f, -0.4f, -HalfPitchWidth - 5f),
            new Vector3(HalfPitchLength, 0.25f, -HalfPitchWidth),
            new Vector3(-HalfPitchLength, 0.25f, -HalfPitchWidth),
            new Vector3(-HalfPitchLength - 5f, -0.4f, -HalfPitchWidth - 5f),
            Vector2.zero, Vector2.one
        );
        // West slope
        apronBuilder.AddQuad(
            new Vector3(-HalfPitchLength - 5f, -0.4f, -HalfPitchWidth - 5f),
            new Vector3(-HalfPitchLength, 0.25f, -HalfPitchWidth),
            new Vector3(-HalfPitchLength, 0.25f, HalfPitchWidth),
            new Vector3(-HalfPitchLength - 5f, -0.4f, HalfPitchWidth + 5f),
            Vector2.zero, Vector2.one
        );
        // East slope
        apronBuilder.AddQuad(
            new Vector3(HalfPitchLength + 5f, -0.4f, HalfPitchWidth + 5f),
            new Vector3(HalfPitchLength, 0.25f, HalfPitchWidth),
            new Vector3(HalfPitchLength, 0.25f, -HalfPitchWidth),
            new Vector3(HalfPitchLength + 5f, -0.4f, -HalfPitchWidth - 5f),
            Vector2.zero, Vector2.one
        );

        // Asphalt Track / Moat perimeter
        apronBuilder.AddBox(new Vector3(0f, -0.45f, 0f), new Vector3(apronW + 6f, 0.1f, apronD + 6f), Quaternion.identity, Vector2.one * 5f);
        CreateMeshObject("Pitch_Apron_And_Slopes", apronBuilder.ToMesh("ApronMesh"), s_ConcreteMat, group.transform);

        // 3. Crisp Pitch Line Geometry (Extruded flat ribbons at Y = 0.515f to prevent Z-fighting)
        ProceduralMeshBuilder lineBuilder = new ProceduralMeshBuilder();
        float yL = 0.515f;
        float lw = 0.12f;

        // Perimeter Lines
        lineBuilder.AddRibbon(new Vector3(-HalfPitchLength, yL, -HalfPitchWidth), new Vector3(HalfPitchLength, yL, -HalfPitchWidth), lw);
        lineBuilder.AddRibbon(new Vector3(-HalfPitchLength, yL, HalfPitchWidth), new Vector3(HalfPitchLength, yL, HalfPitchWidth), lw);
        lineBuilder.AddRibbon(new Vector3(-HalfPitchLength, yL, -HalfPitchWidth), new Vector3(-HalfPitchLength, yL, HalfPitchWidth), lw);
        lineBuilder.AddRibbon(new Vector3(HalfPitchLength, yL, -HalfPitchWidth), new Vector3(HalfPitchLength, yL, HalfPitchWidth), lw);

        // Halfway Line & Center Spot
        lineBuilder.AddRibbon(new Vector3(0f, yL, -HalfPitchWidth), new Vector3(0f, yL, HalfPitchWidth), lw);
        lineBuilder.AddDisk(new Vector3(0f, yL, 0f), 0.35f, 16);
        lineBuilder.AddRing(new Vector3(0f, yL, 0f), 9.15f, lw, 48);

        // Penalty Boxes (16.5m depth x 40.32m width)
        BuildPenaltyAreaLines(lineBuilder, -HalfPitchLength, 1f, yL, lw);
        BuildPenaltyAreaLines(lineBuilder, HalfPitchLength, -1f, yL, lw);

        // Corner Arcs (1m radius)
        lineBuilder.AddArc(new Vector3(-HalfPitchLength, yL, -HalfPitchWidth), 1.0f, 0f, 90f, lw, 12);
        lineBuilder.AddArc(new Vector3(-HalfPitchLength, yL, HalfPitchWidth), 1.0f, 270f, 360f, lw, 12);
        lineBuilder.AddArc(new Vector3(HalfPitchLength, yL, -HalfPitchWidth), 1.0f, 90f, 180f, lw, 12);
        lineBuilder.AddArc(new Vector3(HalfPitchLength, yL, HalfPitchWidth), 1.0f, 180f, 270f, lw, 12);

        CreateMeshObject("Pitch_Lines", lineBuilder.ToMesh("LinesMesh"), s_PitchLineMat, group.transform);

        // 4. Goals and Net Meshes
        BuildGoal(group.transform, -HalfPitchLength, 1f);
        BuildGoal(group.transform, HalfPitchLength, -1f);

        // 5. Technical Area Dugouts (South pitchside)
        BuildDugouts(group.transform);
    }

    private static void BuildPenaltyAreaLines(ProceduralMeshBuilder mb, float goalX, float dirX, float y, float lw)
    {
        float pBoxDepth = 16.5f;
        float pBoxHalfWidth = 20.16f;

        float gBoxDepth = 5.5f;
        float gBoxHalfWidth = 9.16f;

        // Big Box
        float pxFront = goalX + (dirX * pBoxDepth);
        mb.AddRibbon(new Vector3(goalX, y, -pBoxHalfWidth), new Vector3(pxFront, y, -pBoxHalfWidth), lw);
        mb.AddRibbon(new Vector3(goalX, y, pBoxHalfWidth), new Vector3(pxFront, y, pBoxHalfWidth), lw);
        mb.AddRibbon(new Vector3(pxFront, y, -pBoxHalfWidth), new Vector3(pxFront, y, pBoxHalfWidth), lw);

        // Goal Box (6 yard)
        float gxFront = goalX + (dirX * gBoxDepth);
        mb.AddRibbon(new Vector3(goalX, y, -gBoxHalfWidth), new Vector3(gxFront, y, -gBoxHalfWidth), lw);
        mb.AddRibbon(new Vector3(goalX, y, gBoxHalfWidth), new Vector3(gxFront, y, gBoxHalfWidth), lw);
        mb.AddRibbon(new Vector3(gxFront, y, -gBoxHalfWidth), new Vector3(gxFront, y, gBoxHalfWidth), lw);

        // Penalty Spot (11m out)
        Vector3 spotPos = new Vector3(goalX + (dirX * 11.0f), y, 0f);
        mb.AddDisk(spotPos, 0.25f, 12);

        // D-Arc (9.15m from spot, outside box)
        float startAngle = (dirX > 0f) ? -53f : 127f;
        float endAngle = (dirX > 0f) ? 53f : 233f;
        mb.AddArc(spotPos, 9.15f, startAngle, endAngle, lw, 16);
    }

    private static void BuildGoal(Transform parent, float xPos, float inwardDir)
    {
        GameObject goalObj = new GameObject(inwardDir > 0 ? "Goal_West_Stretford" : "Goal_East");
        goalObj.transform.SetParent(parent, false);
        goalObj.transform.position = new Vector3(xPos, 0.5f, 0f);

        ProceduralMeshBuilder postBuilder = new ProceduralMeshBuilder();
        float gw = 7.32f;
        float gh = 2.44f;
        float postRadius = 0.08f;
        float halfW = gw * 0.5f;

        // Uprights & Crossbar
        postBuilder.AddBox(new Vector3(0f, gh * 0.5f, -halfW), new Vector3(postRadius * 2, gh, postRadius * 2), Quaternion.identity, Vector2.one);
        postBuilder.AddBox(new Vector3(0f, gh * 0.5f, halfW), new Vector3(postRadius * 2, gh, postRadius * 2), Quaternion.identity, Vector2.one);
        postBuilder.AddBox(new Vector3(0f, gh, 0f), new Vector3(postRadius * 2, postRadius * 2, gw), Quaternion.identity, Vector2.one);

        // Back Stanchions
        float netDepth = 2.2f;
        Vector3 backOffset = new Vector3(-inwardDir * netDepth, 0f, 0f);
        postBuilder.AddExtrudedLine(new Vector3(0f, gh, -halfW), backOffset + new Vector3(0f, gh * 0.8f, -halfW), 0.05f);
        postBuilder.AddExtrudedLine(new Vector3(0f, gh, halfW), backOffset + new Vector3(0f, gh * 0.8f, halfW), 0.05f);

        CreateMeshObject("Goal_Posts", postBuilder.ToMesh("PostMesh"), s_PitchLineMat, goalObj.transform);

        // Net Mesh
        ProceduralMeshBuilder netBuilder = new ProceduralMeshBuilder();
        Vector3 tl = new Vector3(0f, gh, -halfW);
        Vector3 tr = new Vector3(0f, gh, halfW);
        Vector3 bl = new Vector3(0f, 0f, -halfW);
        Vector3 br = new Vector3(0f, 0f, halfW);

        Vector3 ntl = tl + backOffset;
        Vector3 ntr = tr + backOffset;
        Vector3 nbl = bl + backOffset;
        Vector3 nbr = br + backOffset;

        // Top, Back, and Sides
        netBuilder.AddQuad(tl, tr, ntr, ntl, Vector2.zero, new Vector2(10f, 3f));
        netBuilder.AddQuad(ntl, ntr, nbr, nbl, Vector2.zero, new Vector2(10f, 4f));
        netBuilder.AddQuad(bl, tl, ntl, nbl, Vector2.zero, new Vector2(3f, 4f));
        netBuilder.AddQuad(tr, br, nbr, ntr, Vector2.zero, new Vector2(3f, 4f));

        CreateMeshObject("Goal_NetMesh", netBuilder.ToMesh("NetMesh"), s_GoalNetMat, goalObj.transform);
    }

    private static void BuildDugouts(Transform parent)
    {
        GameObject dugoutGroup = new GameObject("Dugouts_and_Technical_Area");
        dugoutGroup.transform.SetParent(parent, false);

        float dugoutZ = -HalfPitchWidth - 2.8f;
        BuildSingleDugout(dugoutGroup.transform, new Vector3(-9.0f, 0.1f, dugoutZ), "Home_Dugout");
        BuildSingleDugout(dugoutGroup.transform, new Vector3(9.0f, 0.1f, dugoutZ), "Away_Dugout");
    }

    private static void BuildSingleDugout(Transform parent, Vector3 pos, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        ProceduralMeshBuilder frame = new ProceduralMeshBuilder();
        frame.AddBox(new Vector3(0f, 0.9f, -0.9f), new Vector3(10f, 1.8f, 0.15f), Quaternion.identity, Vector2.one);
        frame.AddBox(new Vector3(-5f, 0.9f, 0f), new Vector3(0.15f, 1.8f, 1.8f), Quaternion.identity, Vector2.one);
        frame.AddBox(new Vector3(5f, 0.9f, 0f), new Vector3(0.15f, 1.8f, 1.8f), Quaternion.identity, Vector2.one);
        frame.AddBox(new Vector3(0f, 1.8f, 0f), new Vector3(10.2f, 0.15f, 2.0f), Quaternion.Euler(-10f, 0f, 0f), Vector2.one);
        CreateMeshObject("Structure", frame.ToMesh("DugoutFrame"), s_DarkSteelMat, go.transform);

        // Bucket Seats in Dugout
        ProceduralMeshBuilder seats = new ProceduralMeshBuilder();
        for (int i = 0; i < 14; i++)
        {
            float seatX = -4.2f + (i * 0.65f);
            seats.AddBox(new Vector3(seatX, 0.45f, -0.4f), new Vector3(0.48f, 0.5f, 0.48f), Quaternion.identity, Vector2.one);
            seats.AddBox(new Vector3(seatX, 0.85f, -0.62f), new Vector3(0.48f, 0.55f, 0.12f), Quaternion.identity, Vector2.one);
        }
        CreateMeshObject("Executive_Leather_Seats", seats.ToMesh("SeatsMesh"), s_RedSeatMat, go.transform);
    }

    #endregion

    #region 02 Sir Alex Ferguson Stand (North Stand)

    private static void BuildSirAlexFergusonStand(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "02_Sir_Alex_Ferguson_Stand");

        float standLength = 100f; // Width along X
        float halfL = standLength * 0.5f;

        // Tier 1 (Lower Tier) - 20 rows, rake ~24 deg
        ProceduralMeshBuilder t1Builder = new ProceduralMeshBuilder();
        Vector3 t1Start = new Vector3(-halfL, 0.5f, HalfPitchWidth + 5.5f);
        t1Builder.AddSteppedStand(
            startPos: t1Start,
            standWidthDir: Vector3.right,
            standLength: standLength,
            forwardStepDir: Vector3.forward,
            upDir: Vector3.up,
            stepCount: 20,
            treadDepth: 0.72f,
            riserHeight: 0.28f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_1_Lower", t1Builder.ToMesh("SAF_T1"), s_RedSeatMat, group.transform);

        // Tier 2 (Middle Executive/Box Tier)
        ProceduralMeshBuilder t2Builder = new ProceduralMeshBuilder();
        Vector3 t2Start = new Vector3(-halfL, 7.5f, HalfPitchWidth + 21.0f);
        t2Builder.AddSteppedStand(
            startPos: t2Start,
            standWidthDir: Vector3.right,
            standLength: standLength,
            forwardStepDir: Vector3.forward,
            upDir: Vector3.up,
            stepCount: 14,
            treadDepth: 0.75f,
            riserHeight: 0.38f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_2_Executive", t2Builder.ToMesh("SAF_T2"), s_RedSeatMat, group.transform);

        // Tier 3 (Massive Upper Tier - 38 steep rows reaching 36m height)
        ProceduralMeshBuilder t3Builder = new ProceduralMeshBuilder();
        Vector3 t3Start = new Vector3(-halfL, 14.5f, HalfPitchWidth + 33.0f);
        t3Builder.AddSteppedStand(
            startPos: t3Start,
            standWidthDir: Vector3.right,
            standLength: standLength,
            forwardStepDir: Vector3.forward,
            upDir: Vector3.up,
            stepCount: 38,
            treadDepth: 0.75f,
            riserHeight: 0.56f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_3_Upper_Massive", t3Builder.ToMesh("SAF_T3"), s_RedSeatMat, group.transform);

        // Back Wall Concourse & Structural Core (~42m high)
        ProceduralMeshBuilder concourseBuilder = new ProceduralMeshBuilder();
        float backZ = HalfPitchWidth + 33.0f + (38 * 0.75f); // ~95.5m
        concourseBuilder.AddBox(
            new Vector3(0f, 21.0f, backZ + 2.0f),
            new Vector3(standLength + 6.0f, 42.0f, 4.0f),
            Quaternion.identity,
            new Vector2(10f, 4f)
        );

        // Concrete Support Columns Under Tiers
        for (int i = -3; i <= 3; i++)
        {
            float colX = i * 14f;
            concourseBuilder.AddBox(new Vector3(colX, 8f, HalfPitchWidth + 20f), new Vector3(2.5f, 16f, 2.5f), Quaternion.identity, Vector2.one);
            concourseBuilder.AddBox(new Vector3(colX, 15f, HalfPitchWidth + 32f), new Vector3(3.0f, 30f, 3.0f), Quaternion.identity, Vector2.one);
        }
        CreateMeshObject("Concrete_UnderStructure_And_RearWall", concourseBuilder.ToMesh("SAF_Concrete"), s_ConcreteMat, group.transform);

        // SAF Giant Cantilever Roof Deck
        ProceduralMeshBuilder roofBuilder = new ProceduralMeshBuilder();
        float roofY_Rear = 42.0f;
        float roofY_Front = 37.5f;
        float roofFrontZ = HalfPitchWidth + 4.0f;

        roofBuilder.AddQuad(
            new Vector3(-halfL - 2f, roofY_Front, roofFrontZ),
            new Vector3(halfL + 2f, roofY_Front, roofFrontZ),
            new Vector3(halfL + 2f, roofY_Rear, backZ + 3.0f),
            new Vector3(-halfL - 2f, roofY_Rear, backZ + 3.0f),
            Vector2.zero, new Vector2(12f, 8f)
        );
        CreateMeshObject("Roof_Canopy_Deck", roofBuilder.ToMesh("SAF_RoofDeck"), s_WhiteRoofMat, group.transform);

        // Iconic External Cantilever White Steel Super-Trusses
        BuildSirAlexSuperTrusses(group.transform, halfL, backZ, roofFrontZ);
    }

    private static void BuildSirAlexSuperTrusses(Transform parent, float halfL, float backZ, float roofFrontZ)
    {
        GameObject trussGroup = new GameObject("External_Super_Trusses");
        trussGroup.transform.SetParent(parent, false);

        ProceduralMeshBuilder mb = new ProceduralMeshBuilder();

        // 4 Grand Cantilever Towers spanning above the roof
        float[] trussXPositions = new float[] { -36f, -12f, 12f, 36f };

        foreach (float tx in trussXPositions)
        {
            // Giant rear vertical mast tower rising to 55m
            Vector3 mastTop = new Vector3(tx, 55f, backZ + 2f);
            mb.AddBox(new Vector3(tx, 27.5f, backZ + 2f), new Vector3(2.5f, 55f, 2.5f), Quaternion.identity, Vector2.one);

            // Cantilever rafter extending out forward to pitch inner rim
            Vector3 rafterTip = new Vector3(tx, 39.5f, roofFrontZ);
            Vector3 rafterRear = new Vector3(tx, 44.0f, backZ + 2f);
            mb.AddExtrudedLine(rafterRear, rafterTip, 2.0f);

            // Diagonal tension stays from mast top (55m) down to rafter mid and tip
            Vector3 rafterMid = Vector3.Lerp(rafterRear, rafterTip, 0.5f);
            mb.AddExtrudedLine(mastTop, rafterTip, 0.6f);
            mb.AddExtrudedLine(mastTop, rafterMid, 0.6f);

            // Rear tie-backs anchored into ground
            Vector3 groundAnchor = new Vector3(tx, 0f, backZ + 12f);
            mb.AddExtrudedLine(mastTop, groundAnchor, 0.8f);
        }

        // Cross-bracing lattice between the main towers
        for (int i = 0; i < trussXPositions.Length - 1; i++)
        {
            float xA = trussXPositions[i];
            float xB = trussXPositions[i + 1];
            mb.AddExtrudedLine(new Vector3(xA, 48f, backZ + 2f), new Vector3(xB, 44f, backZ + 2f), 0.5f);
            mb.AddExtrudedLine(new Vector3(xA, 44f, backZ + 2f), new Vector3(xB, 48f, backZ + 2f), 0.5f);
        }

        CreateMeshObject("White_Cantilever_Steelwork", mb.ToMesh("SAF_Trusses"), s_SteelTrussMat, trussGroup.transform);
    }

    #endregion

    #region 03 Stretford End (West Stand)

    private static void BuildStretfordEnd(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "03_Stretford_End");

        float standWidth = 66f; // Span along Z
        float halfW = standWidth * 0.5f;

        // Tier 1 (Lower)
        ProceduralMeshBuilder t1 = new ProceduralMeshBuilder();
        Vector3 t1Start = new Vector3(-HalfPitchLength - 5.5f, 0.5f, -halfW);
        t1.AddSteppedStand(
            startPos: t1Start,
            standWidthDir: Vector3.forward,
            standLength: standWidth,
            forwardStepDir: Vector3.left,
            upDir: Vector3.up,
            stepCount: 22,
            treadDepth: 0.72f,
            riserHeight: 0.28f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_1_Lower", t1.ToMesh("Stretford_T1"), s_RedSeatMat, group.transform);

        // Tier 2 (Upper Stretford End)
        ProceduralMeshBuilder t2 = new ProceduralMeshBuilder();
        float t2StartX = -HalfPitchLength - 5.5f - (22 * 0.72f) - 2.5f; // ~ -74m
        Vector3 t2Start = new Vector3(t2StartX, 8.5f, -halfW);
        t2.AddSteppedStand(
            startPos: t2Start,
            standWidthDir: Vector3.forward,
            standLength: standWidth,
            forwardStepDir: Vector3.left,
            upDir: Vector3.up,
            stepCount: 26,
            treadDepth: 0.75f,
            riserHeight: 0.46f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_2_Upper", t2.ToMesh("Stretford_T2"), s_RedSeatMat, group.transform);

        // Rear Wall & Concrete Shell
        float backX = t2StartX - (26 * 0.75f); // ~ -93.5m
        ProceduralMeshBuilder conc = new ProceduralMeshBuilder();
        conc.AddBox(new Vector3(backX - 1.5f, 13f, 0f), new Vector3(3f, 26f, standWidth + 4f), Quaternion.identity, new Vector2(6f, 3f));
        CreateMeshObject("Concrete_Support_Shell", conc.ToMesh("Stretford_Shell"), s_ConcreteMat, group.transform);

        // Stretford Sloped Roof Canopy
        ProceduralMeshBuilder roof = new ProceduralMeshBuilder();
        float frontX = -HalfPitchLength - 3.5f;
        roof.AddQuad(
            new Vector3(frontX, 22.0f, -halfW - 2f),
            new Vector3(frontX, 22.0f, halfW + 2f),
            new Vector3(backX, 26.5f, halfW + 2f),
            new Vector3(backX, 26.5f, -halfW - 2f),
            Vector2.zero, new Vector2(8f, 6f)
        );
        CreateMeshObject("Roof_Canopy", roof.ToMesh("Stretford_Roof"), s_WhiteRoofMat, group.transform);

        // Transverse Under-Roof Trusses
        ProceduralMeshBuilder truss = new ProceduralMeshBuilder();
        for (int zIdx = -2; zIdx <= 2; zIdx++)
        {
            float zP = zIdx * 14f;
            truss.AddExtrudedLine(new Vector3(backX, 25.5f, zP), new Vector3(frontX, 21.0f, zP), 1.2f);
        }
        CreateMeshObject("Roof_Truss_Girders", truss.ToMesh("Stretford_Trusses"), s_SteelTrussMat, group.transform);
    }

    #endregion

    #region 04 East Stand & Facade

    private static void BuildEastStand(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "04_East_Stand_and_Facade");

        float standWidth = 66f;
        float halfW = standWidth * 0.5f;

        // Tier 1 (Lower)
        ProceduralMeshBuilder t1 = new ProceduralMeshBuilder();
        Vector3 t1Start = new Vector3(HalfPitchLength + 5.5f, 0.5f, halfW);
        t1.AddSteppedStand(
            startPos: t1Start,
            standWidthDir: Vector3.back,
            standLength: standWidth,
            forwardStepDir: Vector3.right,
            upDir: Vector3.up,
            stepCount: 22,
            treadDepth: 0.72f,
            riserHeight: 0.28f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_1_Lower", t1.ToMesh("East_T1"), s_RedSeatMat, group.transform);

        // Tier 2 (Upper)
        ProceduralMeshBuilder t2 = new ProceduralMeshBuilder();
        float t2StartX = HalfPitchLength + 5.5f + (22 * 0.72f) + 2.5f;
        Vector3 t2Start = new Vector3(t2StartX, 8.5f, halfW);
        t2.AddSteppedStand(
            startPos: t2Start,
            standWidthDir: Vector3.back,
            standLength: standWidth,
            forwardStepDir: Vector3.right,
            upDir: Vector3.up,
            stepCount: 26,
            treadDepth: 0.75f,
            riserHeight: 0.46f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_2_Upper", t2.ToMesh("East_T2"), s_RedSeatMat, group.transform);

        // Roof Canopy
        float backX = t2StartX + (26 * 0.75f);
        float frontX = HalfPitchLength + 3.5f;
        ProceduralMeshBuilder roof = new ProceduralMeshBuilder();
        roof.AddQuad(
            new Vector3(frontX, 22.0f, halfW + 2f),
            new Vector3(frontX, 22.0f, -halfW - 2f),
            new Vector3(backX, 26.5f, -halfW - 2f),
            new Vector3(backX, 26.5f, halfW + 2f),
            Vector2.zero, new Vector2(8f, 6f)
        );
        CreateMeshObject("Roof_Canopy", roof.ToMesh("East_Roof"), s_WhiteRoofMat, group.transform);

        // Scoreboard & Screen hung under East Stand roof
        ProceduralMeshBuilder scoreboard = new ProceduralMeshBuilder();
        scoreboard.AddBox(new Vector3(frontX + 6.0f, 18.5f, 0f), new Vector3(0.5f, 4.5f, 10f), Quaternion.identity, Vector2.one);
        CreateMeshObject("Electronic_Scoreboard", scoreboard.ToMesh("East_Scoreboard"), s_DarkSteelMat, group.transform);

        // Iconic East Stand Tinted Glass Facade
        ProceduralMeshBuilder glassFacade = new ProceduralMeshBuilder();
        glassFacade.AddQuad(
            new Vector3(backX + 2.0f, 0.5f, -halfW),
            new Vector3(backX + 2.0f, 27.0f, -halfW),
            new Vector3(backX + 2.0f, 27.0f, halfW),
            new Vector3(backX + 2.0f, 0.5f, halfW),
            Vector2.zero, new Vector2(8f, 4f)
        );
        CreateMeshObject("Iconic_Glass_Frontage", glassFacade.ToMesh("East_GlassFacade"), s_GlassFacadeMat, group.transform);

        // Structural Steel Mullions for Facade
        ProceduralMeshBuilder mullions = new ProceduralMeshBuilder();
        for (int i = -4; i <= 4; i++)
        {
            float zM = i * 7.5f;
            mullions.AddBox(new Vector3(backX + 2.15f, 13.5f, zM), new Vector3(0.35f, 27f, 0.35f), Quaternion.identity, Vector2.one);
        }
        for (int h = 1; h <= 4; h++)
        {
            float yH = h * 6.0f;
            mullions.AddBox(new Vector3(backX + 2.15f, yH, 0f), new Vector3(0.3f, 0.3f, standWidth), Quaternion.identity, Vector2.one);
        }

        // Munich Tunnel Arch Outline (South-East entrance)
        mullions.AddBox(new Vector3(backX - 5.0f, 2.5f, -halfW + 4f), new Vector3(14f, 5f, 4f), Quaternion.identity, Vector2.one);
        CreateMeshObject("Facade_Steel_Mullions_And_MunichTunnel", mullions.ToMesh("East_Mullions"), s_SteelTrussMat, group.transform);
    }

    #endregion

    #region 05 Sir Bobby Charlton Stand (South Stand)

    private static void BuildSirBobbyCharltonStand(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "05_Sir_Bobby_Charlton_Stand");

        float standLength = 94f;
        float halfL = standLength * 0.5f;

        // Single Main Low-Profile Tier
        ProceduralMeshBuilder t1 = new ProceduralMeshBuilder();
        Vector3 t1Start = new Vector3(halfL, 0.5f, -HalfPitchWidth - 5.5f);
        t1.AddSteppedStand(
            startPos: t1Start,
            standWidthDir: Vector3.left,
            standLength: standLength,
            forwardStepDir: Vector3.back,
            upDir: Vector3.up,
            stepCount: 28,
            treadDepth: 0.74f,
            riserHeight: 0.28f,
            buildUnderbody: true
        );
        CreateMeshObject("Tier_1_Main", t1.ToMesh("SBC_T1"), s_RedSeatMat, group.transform);

        // Executive Boxes & Media/TV Suite along the back
        float boxStartDepth = -HalfPitchWidth - 5.5f - (28 * 0.74f);
        ProceduralMeshBuilder execBoxes = new ProceduralMeshBuilder();
        execBoxes.AddBox(
            new Vector3(0f, 10.5f, boxStartDepth - 2.5f),
            new Vector3(standLength, 4.5f, 5.0f),
            Quaternion.identity,
            new Vector2(10f, 1f)
        );
        CreateMeshObject("Executive_Boxes_Structure", execBoxes.ToMesh("SBC_Boxes"), s_LightConcreteMat, group.transform);

        // Historic Old Player Tunnel
        ProceduralMeshBuilder tunnel = new ProceduralMeshBuilder();
        tunnel.AddBox(new Vector3(0f, 1.4f, -HalfPitchWidth - 3.5f), new Vector3(3.5f, 2.8f, 6.0f), Quaternion.identity, Vector2.one);
        CreateMeshObject("Historic_Player_Tunnel_Portal", tunnel.ToMesh("SBC_Tunnel"), s_DarkSteelMat, group.transform);

        // Low Sloped South Roof
        ProceduralMeshBuilder roof = new ProceduralMeshBuilder();
        float frontZ = -HalfPitchWidth - 3.5f;
        float backZ = boxStartDepth - 5.0f;
        roof.AddQuad(
            new Vector3(halfL + 2f, 16.5f, frontZ),
            new Vector3(-halfL - 2f, 16.5f, frontZ),
            new Vector3(-halfL - 2f, 18.5f, backZ),
            new Vector3(halfL + 2f, 18.5f, backZ),
            Vector2.zero, new Vector2(10f, 4f)
        );
        CreateMeshObject("Roof_Canopy", roof.ToMesh("SBC_Roof"), s_WhiteRoofMat, group.transform);

        // Suspended Television Broadcast Media Gantry
        ProceduralMeshBuilder tvGantry = new ProceduralMeshBuilder();
        tvGantry.AddBox(new Vector3(0f, 15.2f, frontZ + 4.5f), new Vector3(24.0f, 1.2f, 2.5f), Quaternion.identity, Vector2.one);
        tvGantry.AddExtrudedLine(new Vector3(-10f, 16.5f, frontZ + 4.5f), new Vector3(-10f, 15.2f, frontZ + 4.5f), 0.2f);
        tvGantry.AddExtrudedLine(new Vector3(10f, 16.5f, frontZ + 4.5f), new Vector3(10f, 15.2f, frontZ + 4.5f), 0.2f);
        CreateMeshObject("TV_Broadcast_Gantry", tvGantry.ToMesh("SBC_TVGantry"), s_DarkSteelMat, group.transform);
    }

    #endregion

    #region 06 Quadrants (Corner Infill Tiers)

    private static void BuildQuadrants(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "06_Quadrants");

        // North-West Quadrant
        BuildCurvedQuadrant(
            group.transform,
            center: new Vector3(-HalfPitchLength - 5.5f, 0f, HalfPitchWidth + 5.5f),
            startAngleDeg: 0f,
            endAngleDeg: 90f,
            name: "Quadrant_NorthWest"
        );

        // North-East Quadrant
        BuildCurvedQuadrant(
            group.transform,
            center: new Vector3(HalfPitchLength + 5.5f, 0f, HalfPitchWidth + 5.5f),
            startAngleDeg: 90f,
            endAngleDeg: 180f,
            name: "Quadrant_NorthEast"
        );
    }

    private static void BuildCurvedQuadrant(Transform parent, Vector3 center, float startAngleDeg, float endAngleDeg, string name)
    {
        GameObject quadObj = new GameObject(name);
        quadObj.transform.SetParent(parent, false);

        // 1. Lower Curved Tier
        ProceduralMeshBuilder lowerMb = new ProceduralMeshBuilder();
        lowerMb.AddQuadrantTier(
            pivotCenter: center,
            innerRadius: 0.5f,
            startAngleDeg: startAngleDeg,
            endAngleDeg: endAngleDeg,
            angularSegments: 16,
            stepCount: 20,
            treadDepth: 0.72f,
            riserHeight: 0.28f,
            baseElevation: 0.5f
        );
        CreateMeshObject("Seating_Lower", lowerMb.ToMesh(name + "_LowerMesh"), s_RedSeatMat, quadObj.transform);

        // 2. Upper Curved Tier
        ProceduralMeshBuilder upperMb = new ProceduralMeshBuilder();
        upperMb.AddQuadrantTier(
            pivotCenter: center,
            innerRadius: 15.5f,
            startAngleDeg: startAngleDeg,
            endAngleDeg: endAngleDeg,
            angularSegments: 16,
            stepCount: 22,
            treadDepth: 0.75f,
            riserHeight: 0.48f,
            baseElevation: 8.5f
        );
        CreateMeshObject("Seating_Upper", upperMb.ToMesh(name + "_UpperMesh"), s_RedSeatMat, quadObj.transform);

        // 3. Curved Glass Shell behind Quadrant
        ProceduralMeshBuilder glassMb = new ProceduralMeshBuilder();
        float rOut = 32.5f;
        int segs = 16;
        float angleStep = (endAngleDeg - startAngleDeg) / segs;

        for (int i = 0; i < segs; i++)
        {
            float a0 = (startAngleDeg + (i * angleStep)) * Mathf.Deg2Rad;
            float a1 = (startAngleDeg + ((i + 1) * angleStep)) * Mathf.Deg2Rad;

            Vector3 p0_b = center + new Vector3(Mathf.Cos(a0) * rOut, 0.5f, Mathf.Sin(a0) * rOut);
            Vector3 p1_b = center + new Vector3(Mathf.Cos(a1) * rOut, 0.5f, Mathf.Sin(a1) * rOut);
            Vector3 p0_t = p0_b + Vector3.up * 28f;
            Vector3 p1_t = p1_b + Vector3.up * 28f;

            glassMb.AddQuad(p0_b, p0_t, p1_t, p1_b, Vector2.zero, Vector2.one);
        }
        CreateMeshObject("Curved_Glass_Shell", glassMb.ToMesh(name + "_GlassMesh"), s_GlassFacadeMat, quadObj.transform);
    }

    #endregion

    #region 07 Roof & Truss System

    private static void BuildRoofAndTrussSystem(Transform parent)
    {
        GameObject group = CreateChildGroup(parent, "07_Roof_and_Truss_System");

        ProceduralMeshBuilder floodlights = new ProceduralMeshBuilder();
        ProceduralMeshBuilder glowLights = new ProceduralMeshBuilder();

        // North Inner Rim Lights (SAF Stand)
        BuildFloodlightRow(
            floodlights, glowLights,
            start: new Vector3(-HalfPitchLength, 37.0f, HalfPitchWidth + 4.5f),
            end: new Vector3(HalfPitchLength, 37.0f, HalfPitchWidth + 4.5f),
            lightCount: 16
        );

        // South Inner Rim Lights (SBC Stand)
        BuildFloodlightRow(
            floodlights, glowLights,
            start: new Vector3(-HalfPitchLength, 16.3f, -HalfPitchWidth - 3.8f),
            end: new Vector3(HalfPitchLength, 16.3f, -HalfPitchWidth - 3.8f),
            lightCount: 16
        );

        // West Inner Rim Lights (Stretford)
        BuildFloodlightRow(
            floodlights, glowLights,
            start: new Vector3(-HalfPitchLength - 3.8f, 21.8f, -HalfPitchWidth),
            end: new Vector3(-HalfPitchLength - 3.8f, 21.8f, HalfPitchWidth),
            lightCount: 12
        );

        // East Inner Rim Lights
        BuildFloodlightRow(
            floodlights, glowLights,
            start: new Vector3(HalfPitchLength + 3.8f, 21.8f, -HalfPitchWidth),
            end: new Vector3(HalfPitchLength + 3.8f, 21.8f, HalfPitchWidth),
            lightCount: 12
        );

        CreateMeshObject("Floodlight_Fixtures", floodlights.ToMesh("Floodlight_Fixtures"), s_DarkSteelMat, group.transform);
        CreateMeshObject("Floodlight_Emissive_Bulbs", glowLights.ToMesh("Floodlight_Glows"), s_FloodlightGlowMat, group.transform);

        // Suspended Inspection Catwalks under the North & South roof
        ProceduralMeshBuilder catwalks = new ProceduralMeshBuilder();
        catwalks.AddBox(new Vector3(0f, 36.8f, HalfPitchWidth + 6.0f), new Vector3(PitchLength + 10f, 0.4f, 1.2f), Quaternion.identity, Vector2.one);
        catwalks.AddBox(new Vector3(0f, 16.0f, -HalfPitchWidth - 5.0f), new Vector3(PitchLength + 10f, 0.4f, 1.2f), Quaternion.identity, Vector2.one);
        CreateMeshObject("Inspection_Catwalks", catwalks.ToMesh("CatwalksMesh"), s_DarkSteelMat, group.transform);
    }

    private static void BuildFloodlightRow(ProceduralMeshBuilder housing, ProceduralMeshBuilder bulbs, Vector3 start, Vector3 end, int lightCount)
    {
        for (int i = 0; i <= lightCount; i++)
        {
            float t = (float)i / lightCount;
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Fixture Housing
            housing.AddBox(pos, new Vector3(1.2f, 0.6f, 0.6f), Quaternion.identity, Vector2.one);
            housing.AddExtrudedLine(pos, pos + Vector3.up * 0.8f, 0.15f);

            // Bright Emissive Face
            bulbs.AddBox(pos - Vector3.up * 0.25f, new Vector3(1.0f, 0.1f, 0.45f), Quaternion.identity, Vector2.one);
        }
    }

    #endregion

    #region Helper Methods & Procedural Mesh Builder

    private static GameObject CreateChildGroup(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateMeshObject(string name, Mesh mesh, Material mat, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        return go;
    }

    /// <summary>
    /// High-performance Procedural Mesh generation container with support for Quads, 
    /// Boxes, Beams, Disks, and Old Trafford's Stepped Seating Rakes.
    /// </summary>
    private class ProceduralMeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv0, Vector2 uv2)
        {
            int baseIdx = Vertices.Count;

            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);

            UVs.Add(new Vector2(uv0.x, uv0.y));
            UVs.Add(new Vector2(uv0.x, uv2.y));
            UVs.Add(new Vector2(uv2.x, uv2.y));
            UVs.Add(new Vector2(uv2.x, uv0.y));

            Vector3 n = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            Normals.Add(n);
            Normals.Add(n);
            Normals.Add(n);
            Normals.Add(n);

            Triangles.Add(baseIdx);
            Triangles.Add(baseIdx + 1);
            Triangles.Add(baseIdx + 2);

            Triangles.Add(baseIdx);
            Triangles.Add(baseIdx + 2);
            Triangles.Add(baseIdx + 3);
        }

        public void AddBox(Vector3 center, Vector3 size, Quaternion rotation, Vector2 uvScale)
        {
            Vector3 ext = size * 0.5f;

            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(-ext.x, -ext.y, -ext.z), // 0
                new Vector3(-ext.x,  ext.y, -ext.z), // 1
                new Vector3( ext.x,  ext.y, -ext.z), // 2
                new Vector3( ext.x, -ext.y, -ext.z), // 3
                new Vector3(-ext.x, -ext.y,  ext.z), // 4
                new Vector3(-ext.x,  ext.y,  ext.z), // 5
                new Vector3( ext.x,  ext.y,  ext.z), // 6
                new Vector3( ext.x, -ext.y,  ext.z)  // 7
            };

            for (int i = 0; i < 8; i++)
            {
                localCorners[i] = center + (rotation * localCorners[i]);
            }

            // Front
            AddQuad(localCorners[0], localCorners[1], localCorners[2], localCorners[3], Vector2.zero, uvScale);
            // Back
            AddQuad(localCorners[7], localCorners[6], localCorners[5], localCorners[4], Vector2.zero, uvScale);
            // Top
            AddQuad(localCorners[1], localCorners[5], localCorners[6], localCorners[2], Vector2.zero, uvScale);
            // Bottom
            AddQuad(localCorners[4], localCorners[0], localCorners[3], localCorners[7], Vector2.zero, uvScale);
            // Left
            AddQuad(localCorners[4], localCorners[5], localCorners[1], localCorners[0], Vector2.zero, uvScale);
            // Right
            AddQuad(localCorners[3], localCorners[2], localCorners[6], localCorners[7], Vector2.zero, uvScale);
        }

        public void AddExtrudedLine(Vector3 start, Vector3 end, float thickness)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 up = (Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.9f) ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(dir, up).normalized * (thickness * 0.5f);
            Vector3 top = Vector3.Cross(right, dir).normalized * (thickness * 0.5f);

            Vector3 p0 = start - right - top;
            Vector3 p1 = start - right + top;
            Vector3 p2 = start + right + top;
            Vector3 p3 = start + right - top;

            Vector3 p4 = end - right - top;
            Vector3 p5 = end - right + top;
            Vector3 p6 = end + right + top;
            Vector3 p7 = end + right - top;

            AddQuad(p0, p1, p2, p3, Vector2.zero, Vector2.one);
            AddQuad(p7, p6, p5, p4, Vector2.zero, Vector2.one);
            AddQuad(p1, p5, p6, p2, Vector2.zero, Vector2.one);
            AddQuad(p4, p0, p3, p7, Vector2.zero, Vector2.one);
            AddQuad(p4, p5, p1, p0, Vector2.zero, Vector2.one);
            AddQuad(p3, p2, p6, p7, Vector2.zero, Vector2.one);
        }

        public void AddRibbon(Vector3 start, Vector3 end, float width)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * (width * 0.5f);

            AddQuad(start - side, start + side, end + side, end - side, Vector2.zero, Vector2.one);
        }

        public void AddDisk(Vector3 center, float radius, int segments)
        {
            int baseIdx = Vertices.Count;
            Vertices.Add(center);
            Normals.Add(Vector3.up);
            UVs.Add(new Vector2(0.5f, 0.5f));

            float step = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float rad = i * step * Mathf.Deg2Rad;
                Vector3 p = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
                Vertices.Add(p);
                Normals.Add(Vector3.up);
                UVs.Add(new Vector2((Mathf.Cos(rad) + 1f) * 0.5f, (Mathf.Sin(rad) + 1f) * 0.5f));
            }

            for (int i = 1; i <= segments; i++)
            {
                Triangles.Add(baseIdx);
                Triangles.Add(baseIdx + i);
                Triangles.Add(baseIdx + i + 1);
            }
        }

        public void AddRing(Vector3 center, float radius, float width, int segments)
        {
            float halfW = width * 0.5f;
            float rInner = radius - halfW;
            float rOuter = radius + halfW;
            float step = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step * Mathf.Deg2Rad;
                float a1 = (i + 1) * step * Mathf.Deg2Rad;

                Vector3 v0 = center + new Vector3(Mathf.Cos(a0) * rInner, 0f, Mathf.Sin(a0) * rInner);
                Vector3 v1 = center + new Vector3(Mathf.Cos(a0) * rOuter, 0f, Mathf.Sin(a0) * rOuter);
                Vector3 v2 = center + new Vector3(Mathf.Cos(a1) * rOuter, 0f, Mathf.Sin(a1) * rOuter);
                Vector3 v3 = center + new Vector3(Mathf.Cos(a1) * rInner, 0f, Mathf.Sin(a1) * rInner);

                AddQuad(v0, v1, v2, v3, Vector2.zero, Vector2.one);
            }
        }

        public void AddArc(Vector3 center, float radius, float startDeg, float endDeg, float width, int segments)
        {
            float halfW = width * 0.5f;
            float rInner = radius - halfW;
            float rOuter = radius + halfW;
            float step = (endDeg - startDeg) / segments;

            for (int i = 0; i < segments; i++)
            {
                float a0 = (startDeg + (i * step)) * Mathf.Deg2Rad;
                float a1 = (startDeg + ((i + 1) * step)) * Mathf.Deg2Rad;

                Vector3 v0 = center + new Vector3(Mathf.Cos(a0) * rInner, 0f, Mathf.Sin(a0) * rInner);
                Vector3 v1 = center + new Vector3(Mathf.Cos(a0) * rOuter, 0f, Mathf.Sin(a0) * rOuter);
                Vector3 v2 = center + new Vector3(Mathf.Cos(a1) * rOuter, 0f, Mathf.Sin(a1) * rOuter);
                Vector3 v3 = center + new Vector3(Mathf.Cos(a1) * rInner, 0f, Mathf.Sin(a1) * rInner);

                AddQuad(v0, v1, v2, v3, Vector2.zero, Vector2.one);
            }
        }

        /// <summary>
        /// Generates realistic stepped seating tiers (Tread & Riser stair-steps) with optional solid base.
        /// </summary>
        public void AddSteppedStand(
            Vector3 startPos,
            Vector3 standWidthDir,
            float standLength,
            Vector3 forwardStepDir,
            Vector3 upDir,
            int stepCount,
            float treadDepth,
            float riserHeight,
            bool buildUnderbody)
        {
            Vector3 currPos = startPos;
            Vector3 wOffset = standWidthDir * standLength;

            for (int s = 0; s < stepCount; s++)
            {
                // 1. Riser (Vertical Face)
                Vector3 r0 = currPos;
                Vector3 r1 = currPos + wOffset;
                Vector3 r2 = r1 + (upDir * riserHeight);
                Vector3 r3 = r0 + (upDir * riserHeight);

                AddQuad(r0, r3, r2, r1, Vector2.zero, new Vector2(standLength * 0.2f, 1f));

                // 2. Tread (Horizontal Step Surface)
                Vector3 t0 = r3;
                Vector3 t1 = r2;
                Vector3 t2 = t1 + (forwardStepDir * treadDepth);
                Vector3 t3 = t0 + (forwardStepDir * treadDepth);

                AddQuad(t0, t3, t2, t1, Vector2.zero, new Vector2(standLength * 0.2f, 1f));

                currPos = t0 + (forwardStepDir * treadDepth);
            }

            if (buildUnderbody)
            {
                // Solid Back wall
                Vector3 topBackL = currPos;
                Vector3 topBackR = currPos + wOffset;
                Vector3 botBackL = startPos + (forwardStepDir * (stepCount * treadDepth));
                Vector3 botBackR = botBackL + wOffset;

                AddQuad(botBackL, topBackL, topBackR, botBackR, Vector2.zero, new Vector2(standLength * 0.1f, 2f));
            }
        }

        /// <summary>
        /// Generates smoothly curving quadrant stepped seating for corners.
        /// </summary>
        public void AddQuadrantTier(
            Vector3 pivotCenter,
            float innerRadius,
            float startAngleDeg,
            float endAngleDeg,
            int angularSegments,
            int stepCount,
            float treadDepth,
            float riserHeight,
            float baseElevation)
        {
            float angleDelta = (endAngleDeg - startAngleDeg) / angularSegments;

            for (int step = 0; step < stepCount; step++)
            {
                float rStepInner = innerRadius + (step * treadDepth);
                float rStepOuter = rStepInner + treadDepth;
                float yStepBottom = baseElevation + (step * riserHeight);
                float yStepTop = yStepBottom + riserHeight;

                for (int seg = 0; seg < angularSegments; seg++)
                {
                    float a0 = (startAngleDeg + (seg * angleDelta)) * Mathf.Deg2Rad;
                    float a1 = (startAngleDeg + ((seg + 1) * angleDelta)) * Mathf.Deg2Rad;

                    float cos0 = Mathf.Cos(a0);
                    float sin0 = Mathf.Sin(a0);
                    float cos1 = Mathf.Cos(a1);
                    float sin1 = Mathf.Sin(a1);

                    // Riser Quad
                    Vector3 r_bl = pivotCenter + new Vector3(cos0 * rStepInner, yStepBottom, sin0 * rStepInner);
                    Vector3 r_tl = pivotCenter + new Vector3(cos0 * rStepInner, yStepTop, sin0 * rStepInner);
                    Vector3 r_tr = pivotCenter + new Vector3(cos1 * rStepInner, yStepTop, sin1 * rStepInner);
                    Vector3 r_br = pivotCenter + new Vector3(cos1 * rStepInner, yStepBottom, sin1 * rStepInner);
                    AddQuad(r_bl, r_tl, r_tr, r_br, Vector2.zero, Vector2.one);

                    // Tread Quad
                    Vector3 t_bl = r_tl;
                    Vector3 t_br = r_tr;
                    Vector3 t_tl = pivotCenter + new Vector3(cos0 * rStepOuter, yStepTop, sin0 * rStepOuter);
                    Vector3 t_tr = pivotCenter + new Vector3(cos1 * rStepOuter, yStepTop, sin1 * rStepOuter);
                    AddQuad(t_bl, t_tl, t_tr, t_br, Vector2.zero, Vector2.one);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh
            {
                name = name,
                indexFormat = IndexFormat.UInt32
            };

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            mesh.SetTriangles(Triangles, 0);

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }

    #endregion
}
#endif