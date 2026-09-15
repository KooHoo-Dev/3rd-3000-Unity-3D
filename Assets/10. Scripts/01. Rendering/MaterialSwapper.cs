using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.Rendering
{
    public class MaterialSwapper : MonoBehaviour
    {
        public Material[] Materials = new Material [0];

        private Renderer myRenderer;
        private int index = 0;

        private void Awake()
        {
            // Renderer 클래스는 모든 Renderer들의 부모 클래스 이다.
            // SpriteRenderer, MeshRenderer등이 해당 클래스를 상속 받아서
            // 구현된 컴포넌트들 
            myRenderer = GetComponent<MeshRenderer>();
            Apply();
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Space))
            {
                index = (index + 1) % Materials.Length;
                Apply();
            }
        }

        private void Apply()
        {
            // 먼저 매테리얼 객체를 가져온다.
            Material mat = Materials[index];
            // myRenderer의 메테리얼을 변경
            myRenderer.sharedMaterial = mat;
        }
    }
}