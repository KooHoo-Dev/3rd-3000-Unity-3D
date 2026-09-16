using System;
using UnityEngine;

namespace Study3D.Rendering
{
    public class TextureOffsetScroller : MonoBehaviour
    {
        [Header("Settings")]
        public Vector2 speed = new(0f, 0.5f);

        private string textureProperty = "_BaseMap";
        private int propertyId;
        private Material targetMaterial;
        private Vector2 offset;
        
        private void Awake()
        {
            targetMaterial = GetComponent<Renderer>().material;
            
            // Shader에게 특정 메테리얼이 갖고 있는 속성이나 함수에 접근하기 위해
            // 문자열을 이용해 Id를 가져온다.
            propertyId = Shader.PropertyToID(textureProperty);
            
            // targetMaterial에서 현재 _BaseMap을 적용하는 Offset 값을 가져온다.
            offset = targetMaterial.GetTextureOffset(propertyId);
        }

        private void Update()
        {
            offset += speed * Time.deltaTime;
            offset.x = Mathf.Repeat(offset.x, 1);
            offset.y = Mathf.Repeat(offset.y, 1);
            targetMaterial.SetTextureOffset(propertyId, offset);
        }

        private void OnDestroy()
        {
            Destroy(targetMaterial);
        }
    }
}