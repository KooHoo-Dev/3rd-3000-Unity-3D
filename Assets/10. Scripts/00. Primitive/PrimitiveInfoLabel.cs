using System;
using TMPro;
using UnityEngine;

namespace Study3D.Primitive
{
    public class PrimitiveInfoLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void Start()
        {
            // 각 기본도형에 정점과 삼각형이 얼마나 있는지 체크

            // MeshFilter는 Mesh(vertex, edge, face 등등)의 정보를 가지고 있는 클래스
            // sharedMesh : 유니티에서 같은 Mesh를 반복해서 재사용 하게끔 만든 동유 Mesh 클래스.
            // (Def랑 비슷한 재사용성을 가지고 있다. 같은 정보를 갖는 객체를 하위 객체가 공유)
            // Mesh : 해당 MeshFilter만 가지고 있는 고유한 Mesh 정보라고 생각하면 된다.

            label = transform.parent.GetComponentInChildren<TMP_Text>();
            
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

            // 해당 Mesh의 정점 갯수 정보
            int vertexCount = mesh.vertexCount;
            // 해당 Mesh의 삼각형 갯수 정보
            int triangleCount = mesh.triangles.Length / 3;
            // 해당 Mesh의 bounds 크기(aabb로 표현한 영역)
            Vector3 size = mesh.bounds.size;

            // ":0##" 은 float의 #객수 자리수 까지 표현
            string infoText =
                $"{name}\n 정점: {vertexCount}" +
                $" \n 삼각형: {triangleCount}" +
                $" \n 메쉬 크기: {size.x:0##}, {size.y:0##}, {size.z:0##}";
            
            label.SetText(infoText);
        }
    }
}