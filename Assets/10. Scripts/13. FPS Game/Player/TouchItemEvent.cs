using UnityEngine;

namespace Study3D.FpsGame
{
    public struct TouchItemEvent
    {
        public TriggerItem.TriggerItemType Type { get; set; }
        public Vector3 Position { get; set; }
    }
}
