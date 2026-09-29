using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine;

public class PlayerTerrainMove : MonoBehaviour
{
    public Terrain randTerrain;

    public float moveSpeed = 3f;

    public Space space = Space.World;
    public float baseY;
    
    private void Start()
    {
        randTerrain = Terrain.activeTerrain;
    }

    private void Update()
    {
        Keyboard current = Keyboard.current;
        baseY = randTerrain.GetPosition().y;
            
        // 이동 처리
        float x = GetAxis(current.aKey, current.dKey);
        float z = GetAxis(current.sKey, current.wKey);
        Vector3 moveVector;
        moveVector.x = x * moveSpeed;
        if(transform.position.y<randTerrain.SampleHeight(transform.position))
            moveVector.y = baseY + randTerrain.SampleHeight(transform.position);
        else
            moveVector.y = baseY - randTerrain.SampleHeight(transform.position);;
        moveVector.z = z * moveSpeed;
        

        transform.Translate(moveVector * Time.deltaTime, space);
    }
    
    private float GetAxis(KeyControl negative, KeyControl positive)
    {
        return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
    }
}
