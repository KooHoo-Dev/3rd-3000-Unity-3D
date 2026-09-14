using System;
using UnityEngine;

namespace _10._Scripts._01._Rendering
{
    public class SimpleQuadMesh : MonoBehaviour
    {
        [Header("크기")] 
        public float width = 2f;
        public float height = 2f;

        private Mesh mesh;

        private void Awake()
        {
            // 새로운 Mesh 객체 생성
            mesh = new Mesh { name = "SimpleQuad" };
            // 생성한 Mesh 객체의 참조정보(주소)를 MeshFilter에 연결
            // (연결괸 Renderer에서 자동으로 Mesh 정보를 가지고 렌더링)
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            Build();
        }

        private void Update()
        {
            
        }

        private void Build()
        {
            mesh.Clear();
            
            // 첫번째로 정점(버텍스)을 생성해 준다.
            // 왼쪽 밑, 오른쪽 밑, 왼쪽 상단, 오른쪽 상단
            Vector3[] vertices =
            {
                new Vector3(-width / 2f, 0f, 0f), // Left Bottom
                new Vector3(width / 2f, 0f, 0f), // Right Bottom
                new Vector3(-width / 2f, height, 0f), // Left Top
                new Vector3(width / 2f, height, 0f), // Right Top
            };
            
            // 정점 생성이 끝났으면 삼각형 정보를 넣어 준다.
            int[] triangles = { 0, 2, 1, 2, 3, 1 };
            Vector3[] normals = { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            Vector2[] uv =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.RecalculateNormals();
        }
    }
}