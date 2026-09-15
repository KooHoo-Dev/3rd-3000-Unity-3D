using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Study3D.Rendering
{
    public class MeshFilterSwapper : MonoBehaviour
    { 
        [SerializeField] private Mesh[] meshes = new Mesh[0];
        
        private MeshFilter myMeshFilter;
        private int index = 0;

        private void Awake()
        {
            // Renderer 클래스는 모든 Renderer들의 부모 클래스 이다.
            // SpriteRenderer, MeshRenderer등이 해당 클래스를 상속 받아서
            // 구현된 컴포넌트들 
            myMeshFilter = GetComponent<MeshFilter>();
            Apply();
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Space))
            {
                index = (index + 1) % meshes.Length;
                Apply();
            }
        }

        private void Apply()
        {
            // 먼저 Mesh 객체를 가져온다.
            Mesh mesh = meshes[index];
            // myMeshFilter의 Mesh를 변경
            myMeshFilter.sharedMesh = mesh;
        }
    }
}