using System.IO;
using System.Text;
using Study3D.FpsGame.Weapon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Study3D.FpsGame.EditorTools
{
    // Railgun / Ricochet Rifle / Tesla Coil을 Player 프리팹에 배치해주는 에디터 도구입니다.
    //
    // 하는 일
    // 1. Player 프리팹을 열고, 기존 총기 모델을 복제해서 새 총기 3종을 만듭니다.
    //    - Railgun, Ricochet Rifle : 어썰트 라이플(SK_AR_02) 모델 복제
    //    - Tesla Coil              : 유탄 발사기(SK_GL_01) 모델 복제
    //    → 모델 안에 들어있는 총구 화염(SM_MuzzleFlash)도 함께 복제되어 그대로 연결됩니다.
    // 2. 구분이 되도록 모델 머티리얼에 색을 입힌 복사본을 만들어 적용합니다.
    // 3. 궤적/빔용 LineRenderer를 자식으로 만들고 무기 컴포넌트에 연결합니다.
    // 4. 설계서의 수치(FireRate, Damage, Ammo, Range)를 넣고 WeaponController 슬롯에 등록합니다.
    //
    // 여러 번 실행해도 안전합니다. (이전에 만든 새 총기 오브젝트를 지우고 다시 만듭니다)
    // 메뉴 : Tools > FPS Game > Setup New Weapons

    [InitializeOnLoad]
    public static class FpsWeaponSetup
    {
        private const string PlayerPrefabPath = "Assets/00. Study/13. FPS Game/Prefabs/Player.prefab";
        private const string MaterialFolder = "Assets/00. Study/13. FPS Game/Materials/Weapons";

        // 이 파일이 있으면 스크립트가 컴파일된 직후 한 번 자동 실행합니다. (외부 도구에서 실행을 요청할 때 사용)
        private const string RequestFile = "Temp/FpsWeaponSetup.request";
        private const string LogFile = "Temp/FpsWeaponSetup.log";

        private static readonly StringBuilder log = new StringBuilder();

        private static double nextRequestCheckTime;

        static FpsWeaponSetup()
        {
            EditorApplication.update += CheckRequest;
        }

        // 1초마다 요청 파일이 있는지 확인합니다. (플레이 중이거나 컴파일 중에는 실행하지 않습니다)
        private static void CheckRequest()
        {
            if (EditorApplication.timeSinceStartup < nextRequestCheckTime) return;
            nextRequestCheckTime = EditorApplication.timeSinceStartup + 1.0;

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(RequestFile) == false) return;

            File.Delete(RequestFile);
            Run();
        }

        [MenuItem("Tools/FPS Game/Setup New Weapons")]
        public static void Run()
        {
            log.Clear();
            try
            {
                Setup();
                Log("RESULT: OK");
            }
            catch (System.Exception e)
            {
                Log("RESULT: FAILED " + e);
                Debug.LogException(e);
            }
            finally
            {
                File.WriteAllText(LogFile, log.ToString());
                Debug.Log("[FpsWeaponSetup]\n" + log);
            }
        }

        private static void Setup()
        {
            EnsureFolder(MaterialFolder);

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var controller = root.GetComponentInChildren<WeaponController>(true);
                if (controller == null) throw new System.Exception("WeaponController를 찾지 못했습니다");

                var controllerSo = new SerializedObject(controller);
                var assaultRifle = controllerSo.FindProperty("assaultRifle").objectReferenceValue as Weapon.Weapon;
                if (assaultRifle == null) throw new System.Exception("WeaponController.assaultRifle이 비어있습니다");

                GameObject arModel = assaultRifle.gameObject;
                Transform weaponParent = arModel.transform.parent;
                Log($"AR model: {GetPath(arModel.transform, root.transform)}");

                GameObject glModel = FindChildByName(weaponParent, "SK_GL_01");
                if (glModel == null) throw new System.Exception("SK_GL_01 모델을 찾지 못했습니다");
                Log($"GL model: {GetPath(glModel.transform, root.transform)}");

                // 다시 실행할 때를 대비해 이전 결과물을 지웁니다.
                foreach (string oldName in new[] { "Railgun", "RicochetRifle", "TeslaCoil" })
                {
                    GameObject old = FindChildByName(weaponParent, oldName);
                    if (old != null) { Object.DestroyImmediate(old); Log($"removed old {oldName}"); }
                }

                var railgun = CreateWeapon<Railgun>(arModel, "Railgun",
                    tint: new Color(0.45f, 0.75f, 1.0f),
                    fireRate: 0.8f, damage: 150, totalAmmo: 5, range: 300f);
                var ricochet = CreateWeapon<RicochetRifle>(arModel, "RicochetRifle",
                    tint: new Color(1.0f, 0.8f, 0.3f),
                    fireRate: 0.35f, damage: 15, totalAmmo: 12, range: 60f);
                var tesla = CreateWeapon<TeslaCoil>(glModel, "TeslaCoil",
                    tint: new Color(0.7f, 0.5f, 1.0f),
                    fireRate: 0.1f, damage: 6, totalAmmo: 100, range: 30f);

                // 연출용 LineRenderer 연결
                {
                    var so = new SerializedObject(railgun);
                    so.FindProperty("trailLine").objectReferenceValue =
                        CreateLine(railgun.transform, "TrailLine", new Color(0.4f, 0.9f, 1.0f), 0.05f);
                    so.FindProperty("trailStartPoint").objectReferenceValue = GetMuzzle(railgun);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                {
                    var so = new SerializedObject(ricochet);
                    so.FindProperty("pathLine").objectReferenceValue =
                        CreateLine(ricochet.transform, "PathLine", new Color(1.0f, 0.85f, 0.2f), 0.02f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                {
                    var so = new SerializedObject(tesla);
                    so.FindProperty("chainLine").objectReferenceValue =
                        CreateLine(tesla.transform, "ChainLine", new Color(0.75f, 0.55f, 1.0f), 0.04f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // WeaponController 슬롯 등록
                controllerSo.Update();
                controllerSo.FindProperty("railgun").objectReferenceValue = railgun;
                controllerSo.FindProperty("ricochetRifle").objectReferenceValue = ricochet;
                controllerSo.FindProperty("teslaCoil").objectReferenceValue = tesla;
                controllerSo.ApplyModifiedPropertiesWithoutUndo();
                Log("WeaponController slots assigned");

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath, out bool success);
                if (success == false) throw new System.Exception("프리팹 저장 실패");
                Log("Player.prefab saved");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        // 원본 모델을 복제해서 기존 무기 컴포넌트를 떼고 새 무기 컴포넌트를 붙입니다.
        private static T CreateWeapon<T>(GameObject sourceModel, string name,
            Color tint, float fireRate, int damage, int totalAmmo, float range) where T : Weapon.Weapon
        {
            GameObject copy = Object.Instantiate(sourceModel, sourceModel.transform.parent);
            copy.name = name;
            copy.transform.SetSiblingIndex(sourceModel.transform.parent.childCount - 1);

            // 원본에 붙어있던 무기 컴포넌트(예: AssaultRifle)는 제거합니다.
            foreach (var oldWeapon in copy.GetComponents<Weapon.Weapon>())
                Object.DestroyImmediate(oldWeapon);
            foreach (var tester in copy.GetComponents<WeaponTester>())
                Object.DestroyImmediate(tester);

            // 총구 화염 : 모델 안의 SM_MuzzleFlash를 찾습니다.
            Transform muzzle = FindDescendantStartsWith(copy.transform, "SM_MuzzleFlash");
            if (muzzle == null) throw new System.Exception($"{name}: 총구 화염(SM_MuzzleFlash)을 찾지 못했습니다");

            var weapon = copy.AddComponent<T>();
            var so = new SerializedObject(weapon);
            so.FindProperty("weaponName").stringValue = name;
            so.FindProperty("fireRate").floatValue = fireRate;
            so.FindProperty("damage").intValue = damage;
            so.FindProperty("totalAmmo").intValue = totalAmmo;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("muzzleFlash").objectReferenceValue = muzzle.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            TintModel(copy, muzzle, name, tint);

            // WeaponController.OnEnable에서 어차피 꺼주지만, 프리팹에서도 꺼둡니다.
            copy.SetActive(false);

            Log($"{name}: created from {sourceModel.name}, muzzle = {muzzle.name}");
            return weapon;
        }

        private static Transform GetMuzzle(Weapon.Weapon weapon)
        {
            return FindDescendantStartsWith(weapon.transform, "SM_MuzzleFlash");
        }

        // 모델의 머티리얼을 복사해서 색을 곱한 뒤 적용합니다. (총구 화염은 건드리지 않습니다)
        private static void TintModel(GameObject model, Transform muzzle, string weaponName, Color tint)
        {
            int count = 0;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.IsChildOf(muzzle)) continue;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;

                    string path = $"{MaterialFolder}/M_{weaponName}_{SafeName(source.name)}.mat";
                    var tinted = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (tinted == null)
                    {
                        tinted = new Material(source);
                        AssetDatabase.CreateAsset(tinted, path);
                    }
                    else
                    {
                        tinted.CopyPropertiesFromMaterial(source);
                    }

                    if (tinted.HasProperty("_BaseColor"))
                        tinted.SetColor("_BaseColor", source.GetColor("_BaseColor") * tint);
                    if (tinted.HasProperty("_Color"))
                        tinted.SetColor("_Color", source.GetColor("_Color") * tint);
                    EditorUtility.SetDirty(tinted);

                    materials[i] = tinted;
                    count++;
                }
                renderer.sharedMaterials = materials;
            }
            Log($"{weaponName}: tinted {count} material slot(s)");
        }

        // 궤적/빔용 LineRenderer를 자식으로 만듭니다. 무기 스크립트가 켜고 끄며 위치를 지정합니다.
        private static LineRenderer CreateLine(Transform parent, string name, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = color;
            line.endColor = color;
            line.sharedMaterial = GetLineMaterial(name, color);
            line.enabled = false;
            return line;
        }

        private static Material GetLineMaterial(string lineName, Color color)
        {
            string path = $"{MaterialFolder}/M_{lineName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        #region 유틸

        private static GameObject FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
                if (child.name == name) return child.gameObject;
            return null;
        }

        private static Transform FindDescendantStartsWith(Transform root, string prefix)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name.StartsWith(prefix)) return t;
            return null;
        }

        private static string GetPath(Transform t, Transform root)
        {
            string path = t.name;
            while (t.parent != null && t != root) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        private static string SafeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(" (Instance)", "");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static void Log(string message) => log.AppendLine(message);

        #endregion
    }
}
