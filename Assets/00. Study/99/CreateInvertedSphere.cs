using UnityEngine;
using UnityEditor;

public class InvertedSphereCreator
{
    /// <summary>
    /// 하이어라키 창 우클릭 -> 3D Object 메뉴에 "Inverted Sphere" 항목을 추가합니다.
    /// </summary>
    [MenuItem("GameObject/3D Object/Inverted Sphere", false, 10)]
    private static void CreateInvertedSphere(MenuCommand menuCommand)
    {
        // 1. 기본 구체(Sphere) 생성
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Inverted Sphere";

        // 2. 메쉬의 안팎(노멀 및 삼각형 바인딩 순서) 뒤집기
        MeshFilter meshFilter = sphere.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // 기본 메쉬 수정 방지를 위해 복사본 생성
            Mesh invertedMesh = Object.Instantiate(meshFilter.sharedMesh);
            invertedMesh.name = "Inverted_Sphere_Mesh";

            // 2-1. 노멀(Normals) 방향 반전 (안쪽을 바라보도록 함)
            Vector3[] normals = invertedMesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            invertedMesh.normals = normals;

            // 2-2. 삼각형(Triangles) 바인딩 순서 뒤집기 (시계 <-> 반시계)
            for (int submesh = 0; submesh < invertedMesh.subMeshCount; submesh++)
            {
                int[] triangles = invertedMesh.GetTriangles(submesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i];
                    triangles[i] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                invertedMesh.SetTriangles(triangles, submesh);
            }

            meshFilter.sharedMesh = invertedMesh;
        }

        // 3. 하이어라키 선택 및 Undo(실행 취소) 등록
        GameObjectUtility.SetParentAndAlign(sphere, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(sphere, "Create " + sphere.name);
        Selection.activeObject = sphere;
    }
}