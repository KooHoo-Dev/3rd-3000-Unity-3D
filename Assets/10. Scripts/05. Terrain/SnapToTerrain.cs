using UnityEngine;

public class SnapToTerrain : MonoBehaviour
{
    public Terrain terrain;
    
    [ContextMenu("Snap To Terrain")]
    public void SnapChildren()
    {
        // 자식 오브젝트들의 y값을 터레인의 height로 조절
        // - 자식 오브젝트들
        // -터레인과 터레인의 특정 위치의 y값
        terrain = Terrain.activeTerrain;
        
        // GetPosition()
        // : 현재 터레인이 붙어있는 transform 컴포넌트의 위치와
        //   본인의 Height값을 고려하여 터레인의 기본 위치를 반환
        //   (터레인의 월드 위치)
        float baseY = terrain.GetPosition().y;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Vector3 childPosition = child.position;
            
            // SampleHeight(world 위치 좌표)
            // : 매개변수로 전달된 위치좌표로
            //   터레인의 해당 위치 height값을 추정하여 반환
            //   (get이 아닌 이유는 보간해서 추정하기 때문)
            childPosition.y = baseY + terrain.SampleHeight(childPosition);
            child.position = childPosition;
        }
    }
}
