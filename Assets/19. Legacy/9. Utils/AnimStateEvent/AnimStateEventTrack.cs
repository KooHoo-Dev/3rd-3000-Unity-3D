using System.Collections.Generic;
using UnityEngine;

namespace Jay.AnimStateEvent
{
    public class AnimStateEventTrack : StateMachineBehaviour
    {
        [SerializeField] private string trackKey;
        [SerializeField] private TimedEvent[] events;

        private AnimStateEventHub hub;
        
        public string TrackKey => trackKey;
        public IReadOnlyList<TimedEvent> Events => events;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(hub == null) hub = animator.GetComponent<AnimStateEventHub>();
            
            if (hub == null)
            {
                Debug.LogError($"{animator.gameObject.name} has no AnimStateEventHub attached!");
                return;
            }
            
            hub.EnterTrack(this);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (hub == null) return;
            hub.UpdateTrack(this, stateInfo.normalizedTime);
        }
    }
}