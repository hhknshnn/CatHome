using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds the shared premium fixed-architecture finish to every room shell.
/// Catalog products are intentionally not created here: this layer must remain
/// visible in a completely empty room and must never affect placement collision.
/// </summary>
public static class HomeRoomShellVisualPolishBuilder
{
    public const string RootName = "FixedArchitecturePolish";

    private const string MaterialFolder = "Assets/Art/RoomShellPolish/Materials";

    private static readonly Color32 Pearl = new Color32(255, 253, 246, 255);
    private static readonly Color32 Gold = new Color32(244, 190, 83, 255);
    private static readonly Color32 Aqua = new Color32(126, 229, 221, 255);
    private static readonly Color32 Mint = new Color32(179, 241, 207, 255);
    private static readonly Color32 Peach = new Color32(255, 204, 178, 255);
    private static readonly Color32 Lilac = new Color32(214, 190, 241, 255);
    private static readonly Color32 Pink = new Color32(255, 163, 190, 255);
    private static readonly Color32 Lemon = new Color32(255, 228, 130, 255);

    public static void Apply(Scene scene, string roomId, Transform parent)
    {
        if (!scene.IsValid() || parent == null)
            throw new ArgumentException("A valid room scene and parent are required.");

        RemoveExisting(scene);
        EnsureFolders();

        Dictionary<string, Material> materials = new Dictionary<string, Material>
        {
            ["Pearl"] = GetOrCreateMaterial("ShellPolish_Pearl", Pearl, .5f),
            ["Gold"] = GetOrCreateMaterial("ShellPolish_Gold", Gold, .7f, .08f),
            ["Aqua"] = GetOrCreateMaterial("ShellPolish_Aqua", Aqua, .48f),
            ["Mint"] = GetOrCreateMaterial("ShellPolish_Mint", Mint, .42f),
            ["Peach"] = GetOrCreateMaterial("ShellPolish_Peach", Peach, .42f),
            ["Lilac"] = GetOrCreateMaterial("ShellPolish_Lilac", Lilac, .46f),
            ["Pink"] = GetOrCreateMaterial("ShellPolish_Pink", Pink, .48f),
            ["Lemon"] = GetOrCreateMaterial("ShellPolish_Lemon", Lemon, .48f),
        };

        GameObject rootObject = new GameObject(RootName);
        rootObject.transform.SetParent(parent, false);
        Transform root = rootObject.transform;

        switch (roomId)
        {
            case HomeRoomService.LivingRoomId:
                BuildFrontThreshold(root, materials, "LivingPearlThreshold", materials["Aqua"]);
                BuildCornerJewels(root, materials, materials["Pink"], materials["Aqua"]);
                break;
            case HomeRoomService.BathroomId:
                BuildIndoorFrame(root, materials, materials["Aqua"], materials["Mint"]);
                BuildBubbleSignature(root, materials);
                break;
            case HomeRoomService.KitchenId:
                BuildFrontThreshold(root, materials, "KitchenPearlThreshold", materials["Lemon"]);
                BuildCornerJewels(root, materials, materials["Peach"], materials["Aqua"]);
                break;
            case HomeRoomService.BedroomId:
                BuildIndoorFrame(root, materials, materials["Lilac"], materials["Pink"]);
                BuildMoonSignature(root, materials);
                break;
            case HomeRoomService.GardenId:
                BuildGardenFinish(root, materials);
                break;
            case HomeRoomService.BalconyId:
                BuildBalconyFinish(root, materials);
                break;
            case HomeRoomService.PatioId:
                BuildPatioFinish(root, materials);
                break;
            case HomeRoomService.SecondFloorId:
                BuildLoftFinish(root, materials);
                break;
            default:
                BuildIndoorFrame(root, materials, materials["Aqua"], materials["Mint"]);
                break;
        }
        HomeRoomPremiumFinishBuilder.Apply(scene, roomId);
        // Other rooms finish inside their measured architecture pass.
        if (roomId == HomeRoomService.LivingRoomId)
            ModernWorldArtBuilder.Apply(scene, roomId);
    }

    private static void BuildIndoorFrame(
        Transform root,
        IReadOnlyDictionary<string, Material> materials,
        Material leftAccent,
        Material rightAccent)
    {
        Transform crown = CreateGroup(root, "Pearl Gold Crown Molding");
        CreateBox(crown, "Crown_Back_Pearl", new Vector3(0f, 2.83f, 2.68f),
            new Vector3(7.5f, .13f, .12f), materials["Pearl"]);
        CreateBox(crown, "Crown_Back_Gold", new Vector3(0f, 2.755f, 2.615f),
            new Vector3(7.34f, .035f, .035f), materials["Gold"]);
        CreateBox(crown, "Crown_Left_Pearl", new Vector3(-3.68f, 2.83f, 0f),
            new Vector3(.12f, .13f, 5.45f), materials["Pearl"]);
        CreateBox(crown, "Crown_Left_Gold", new Vector3(-3.615f, 2.755f, 0f),
            new Vector3(.035f, .035f, 5.3f), materials["Gold"]);
        CreateBox(crown, "Crown_Right_Pearl", new Vector3(3.68f, 2.83f, 0f),
            new Vector3(.12f, .13f, 5.45f), materials["Pearl"]);
        CreateBox(crown, "Crown_Right_Gold", new Vector3(3.615f, 2.755f, 0f),
            new Vector3(.035f, .035f, 5.3f), materials["Gold"]);

        Transform rails = CreateGroup(root, "Candy Picture Rails");
        CreateBox(rails, "PictureRail_Back", new Vector3(0f, 1.22f, 2.665f),
            new Vector3(7.42f, .075f, .055f), materials["Pearl"]);
        CreateBox(rails, "PictureRail_Back_Gold", new Vector3(0f, 1.18f, 2.63f),
            new Vector3(7.26f, .025f, .02f), materials["Gold"]);
        CreateBox(rails, "PictureRail_Left", new Vector3(-3.665f, 1.22f, 0f),
            new Vector3(.055f, .075f, 5.3f), leftAccent);
        CreateBox(rails, "PictureRail_Right", new Vector3(3.665f, 1.22f, 0f),
            new Vector3(.055f, .075f, 5.3f), rightAccent);

        Transform floorFrame = CreateGroup(root, "Inset Floor Frame");
        CreateBox(floorFrame, "FloorInlay_Back", new Vector3(0f, .075f, 2.58f),
            new Vector3(7.18f, .025f, .045f), materials["Gold"]);
        CreateBox(floorFrame, "FloorInlay_Left", new Vector3(-3.58f, .075f, 0f),
            new Vector3(.045f, .025f, 5.16f), materials["Gold"]);
        CreateBox(floorFrame, "FloorInlay_Right", new Vector3(3.58f, .075f, 0f),
            new Vector3(.045f, .025f, 5.16f), materials["Gold"]);
        BuildFrontThreshold(root, materials, "Pearl Room Threshold", leftAccent);
    }

    private static void BuildFrontThreshold(
        Transform root,
        IReadOnlyDictionary<string, Material> materials,
        string name,
        Material accent)
    {
        Transform threshold = CreateGroup(root, name);
        CreateBox(threshold, "PearlFrame", new Vector3(0f, .07f, -2.72f),
            new Vector3(7.38f, .09f, .14f), materials["Pearl"]);
        CreateBox(threshold, "GoldInset", new Vector3(0f, .12f, -2.70f),
            new Vector3(7.12f, .025f, .035f), materials["Gold"]);
        // The floating colored center strip is retired; keep the pearl/gold threshold.
    }

    private static void BuildCornerJewels(
        Transform root,
        IReadOnlyDictionary<string, Material> materials,
        Material left,
        Material right)
    {
        Transform jewels = CreateGroup(root, "Candy Corner Jewels");
        CreateSphere(jewels, "LeftPearl", new Vector3(-3.32f, .18f, -2.63f),
            new Vector3(.18f, .18f, .18f), materials["Pearl"]);
        CreateSphere(jewels, "LeftCandy", new Vector3(-3.32f, .20f, -2.66f),
            new Vector3(.10f, .10f, .10f), left);
        CreateSphere(jewels, "RightPearl", new Vector3(3.32f, .18f, -2.63f),
            new Vector3(.18f, .18f, .18f), materials["Pearl"]);
        CreateSphere(jewels, "RightCandy", new Vector3(3.32f, .20f, -2.66f),
            new Vector3(.10f, .10f, .10f), right);
    }

    private static void BuildBubbleSignature(
        Transform root, IReadOnlyDictionary<string, Material> materials)
    {
        Transform bubbles = CreateGroup(root, "Pearl Bubble Wall Signature");
        CreateSphere(bubbles, "BubbleLarge", new Vector3(-2.75f, 2.13f, 2.61f),
            new Vector3(.40f, .40f, .08f), materials["Pearl"]);
        CreateSphere(bubbles, "BubbleAqua", new Vector3(-2.75f, 2.13f, 2.56f),
            new Vector3(.28f, .28f, .05f), materials["Aqua"]);
        CreateSphere(bubbles, "BubbleMint", new Vector3(-2.29f, 2.43f, 2.59f),
            new Vector3(.19f, .19f, .06f), materials["Mint"]);
        CreateSphere(bubbles, "BubbleGold", new Vector3(-2.13f, 1.91f, 2.59f),
            new Vector3(.13f, .13f, .06f), materials["Gold"]);
    }

    private static void BuildMoonSignature(
        Transform root, IReadOnlyDictionary<string, Material> materials)
    {
        Transform moon = CreateGroup(root, "Dream Moon Wall Signature");
        CreateCylinder(moon, "MoonGold", new Vector3(-2.65f, 2.15f, 2.60f),
            new Vector3(.34f, .055f, .34f), new Vector3(90f, 0f, 0f), materials["Gold"]);
        CreateCylinder(moon, "MoonLilacInset", new Vector3(-2.53f, 2.23f, 2.55f),
            new Vector3(.28f, .06f, .28f), new Vector3(90f, 0f, 0f), materials["Lilac"]);
        CreateSphere(moon, "StarPearl", new Vector3(-2.09f, 2.45f, 2.57f),
            new Vector3(.13f, .13f, .05f), materials["Pearl"]);
        CreateSphere(moon, "StarPink", new Vector3(-2.14f, 1.84f, 2.57f),
            new Vector3(.09f, .09f, .04f), materials["Pink"]);
    }

    private static void BuildGardenFinish(
        Transform root, IReadOnlyDictionary<string, Material> materials)
    {
        Transform path = CreateGroup(root, "Garden Path Jewelry");
        CreateBox(path, "LeftMintEdge", new Vector3(-.72f, .075f, .85f),
            new Vector3(.055f, .035f, 3.55f), materials["Mint"]);
        CreateBox(path, "RightAquaEdge", new Vector3(.72f, .075f, .85f),
            new Vector3(.055f, .035f, 3.55f), materials["Aqua"]);
        for (int i = 0; i < 5; i++)
        {
            float z = -.65f + (i * .72f);
            CreateSphere(path, $"LeftGoldBud_{i + 1}", new Vector3(-.72f, .12f, z),
                new Vector3(.10f, .10f, .10f), materials["Gold"]);
            CreateSphere(path, $"RightPearlBud_{i + 1}", new Vector3(.72f, .12f, z),
                new Vector3(.10f, .10f, .10f), materials["Pearl"]);
        }
        BuildFrontThreshold(root, materials, "Garden Pearl Threshold", materials["Mint"]);
    }

    private static void BuildBalconyFinish(
        Transform root, IReadOnlyDictionary<string, Material> materials)
    {
        Transform frame = CreateGroup(root, "Balcony Pearl Gold Frame");
        CreateBox(frame, "FloorInlay_Back", new Vector3(0f, .075f, 2.52f),
            new Vector3(7.15f, .03f, .05f), materials["Gold"]);
        CreateBox(frame, "FloorInlay_Left", new Vector3(-3.52f, .075f, 0f),
            new Vector3(.05f, .03f, 4.95f), materials["Aqua"]);
        CreateBox(frame, "FloorInlay_Right", new Vector3(3.52f, .075f, 0f),
            new Vector3(.05f, .03f, 4.95f), materials["Pink"]);
        BuildFrontThreshold(root, materials, "Balcony Pearl Threshold", materials["Aqua"]);
        BuildCornerJewels(root, materials, materials["Mint"], materials["Pink"]);
    }

    private static void BuildPatioFinish(
        Transform root, IReadOnlyDictionary<string, Material> materials)
    {
        Transform frame = CreateGroup(root, "Patio Mosaic Border");
        CreateBox(frame, "BackGoldInlay", new Vector3(0f, .075f, 2.52f),
            new Vector3(7.1f, .03f, .05f), materials["Gold"]);
        CreateBox(frame, "LeftMintInlay", new Vector3(-3.52f, .075f, 0f),
            new Vector3(.05f, .03f, 4.95f), materials["Mint"]);
        CreateBox(frame, "RightPeachInlay", new Vector3(3.52f, .075f, 0f),
            new Vector3(.05f, .03f, 4.95f), materials["Peach"]);
        BuildFrontThreshold(root, materials, "Patio Pearl Threshold", materials["Peach"]);
        BuildCornerJewels(root, materials, materials["Lemon"], materials["Mint"]);
    }

    private static void BuildLoftFinish(
        Transform root, IReadOnlyDictionary<string, Material> materials)
    {
        Transform frame = CreateGroup(root, "Loft Pearl Gold Joinery");
        CreateBox(frame, "BackPictureRail", new Vector3(0f, 1.20f, 2.64f),
            new Vector3(7.35f, .07f, .05f), materials["Pearl"]);
        CreateBox(frame, "BackGoldInset", new Vector3(0f, 1.16f, 2.61f),
            new Vector3(7.18f, .025f, .02f), materials["Gold"]);
        CreateBox(frame, "LeftPictureRail", new Vector3(-3.64f, 1.20f, 0f),
            new Vector3(.05f, .07f, 5.18f), materials["Lilac"]);
        CreateBox(frame, "RightPictureRail", new Vector3(3.64f, 1.20f, 0f),
            new Vector3(.05f, .07f, 5.18f), materials["Aqua"]);
        CreateBox(frame, "FloorInlay_Back", new Vector3(0f, .075f, 2.54f),
            new Vector3(7.15f, .03f, .05f), materials["Gold"]);
        BuildFrontThreshold(root, materials, "Loft Pearl Threshold", materials["Lilac"]);
        BuildCornerJewels(root, materials, materials["Lilac"], materials["Aqua"]);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreateBox(
        Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        return CreatePrimitive(PrimitiveType.Cube, parent, name, position, scale, Vector3.zero, material);
    }

    private static GameObject CreateSphere(
        Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        return CreatePrimitive(PrimitiveType.Sphere, parent, name, position, scale, Vector3.zero, material);
    }

    private static GameObject CreateCylinder(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Vector3 euler,
        Material material)
    {
        return CreatePrimitive(PrimitiveType.Cylinder, parent, name, position, scale, euler, material);
    }

    private static GameObject CreatePrimitive(
        PrimitiveType primitiveType,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Vector3 euler,
        Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localEulerAngles = euler;
        gameObject.transform.localScale = scale;

        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);

        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return gameObject;
    }

    private static void RemoveExisting(Scene scene)
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            Transform[] transforms = sceneRoot.GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                if (transforms[i] != null && transforms[i].name == RootName)
                    UnityEngine.Object.DestroyImmediate(transforms[i].gameObject);
            }
        }
    }

    private static Material GetOrCreateMaterial(
        string name, Color color, float smoothness, float metallic = 0f)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("No compatible lit shader was found.");

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art", "RoomShellPolish");
        EnsureFolder("Assets/Art/RoomShellPolish", "Materials");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
