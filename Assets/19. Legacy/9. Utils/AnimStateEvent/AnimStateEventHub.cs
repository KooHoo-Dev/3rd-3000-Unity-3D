using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jay.AnimStateEvent
{
    public class AnimStateEventHub : MonoBehaviour
    {
        private class TrackState
        {
            public readonly List<TimedEvent> Events = new List<TimedEvent>();
            public readonly HashSet<TimedEvent> FiredEvents = new HashSet<TimedEvent>();
        }

        private Animator animator;
        private readonly Dictionary<AnimStateEventTrack, TrackState> trackStates = new Dictionary<AnimStateEventTrack, TrackState>();
        private readonly Dictionary<string, AnimStateEventTrack> tracksByKey = new Dictionary<string, AnimStateEventTrack>();

        public event Action<AnimStateEventTrack, TimedEvent> OnEventFired;
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError($"{gameObject.name} Animator component not found");
                this.enabled = false;
                return;
            }
            
            var tracks = animator.GetBehaviours<AnimStateEventTrack>();
            foreach (var track in tracks) Register(track);
        }


         public void Register(AnimStateEventTrack track)
        {
            if (trackStates.ContainsKey(track)) return;

            var state = new TrackState();
            var defaultEvents = track.Events;
            if (defaultEvents != null)
            {
                for (int i = 0; i < defaultEvents.Count; i++)
                {
                    state.Events.Add(defaultEvents[i].Clone());
                }
            }
            trackStates.Add(track, state);

            if (string.IsNullOrEmpty(track.TrackKey)) return;
            if (tracksByKey.ContainsKey(track.TrackKey))
            {
                Debug.LogWarning($"[AnimStateEventHub] 같은 TrackKey가 이미 있습니다: {track.TrackKey}", this);
                return;
            }
            tracksByKey.Add(track.TrackKey, track);
        }

        public void Unregister(AnimStateEventTrack track)
        {
            if (trackStates.Remove(track) == false) return;

            if (string.IsNullOrEmpty(track.TrackKey)) return;
            if (tracksByKey.TryGetValue(track.TrackKey, out var registered) && registered == track)
            {
                tracksByKey.Remove(track.TrackKey);
            }
        }

        public AnimStateEventTrack GetTrackOrNull(string trackKey)
        {
            tracksByKey.TryGetValue(trackKey, out var track);
            return track;
        }

        public bool TryGetTrack(string trackKey, out AnimStateEventTrack track)
        {
            return tracksByKey.TryGetValue(trackKey, out track);
        }

        public IReadOnlyList<TimedEvent> GetEvents(AnimStateEventTrack track)
        {
            var state = GetStateOrNull(track);
            if (state == null) return Array.Empty<TimedEvent>();
            return state.Events;
        }

        public void AddEvent(AnimStateEventTrack track, TimedEvent timedEvent)
        {
            var state = GetStateOrNull(track);
            if (state == null) return;

            state.Events.Add(timedEvent);
        }

        public bool RemoveEvent(AnimStateEventTrack track, string eventName)
        {
            var state = GetStateOrNull(track);
            if (state == null) return false;

            var timedEvent = FindEventOrNull(state, eventName);
            if (timedEvent == null) return false;

            state.FiredEvents.Remove(timedEvent);
            return state.Events.Remove(timedEvent);
        }

        public void ClearEvents(AnimStateEventTrack track)
        {
            var state = GetStateOrNull(track);
            if (state == null) return;

            state.Events.Clear();
            state.FiredEvents.Clear();
        }

        public bool SetEventTime(AnimStateEventTrack track, string eventName, float normalizedTime)
        {
            var state = GetStateOrNull(track);
            if (state == null) return false;

            var timedEvent = FindEventOrNull(state, eventName);
            if (timedEvent == null) return false;

            timedEvent.NormalizedTime = normalizedTime;
            return true;
        }

        public void EnterTrack(AnimStateEventTrack track)
        {
            if (trackStates.ContainsKey(track) == false) Register(track);

            trackStates[track].FiredEvents.Clear();
        }

        public void UpdateTrack(AnimStateEventTrack track, float normalizedTime)
        {
            var state = GetStateOrNull(track);
            if (state == null) return;

            for (int i = 0; i < state.Events.Count; i++)
            {
                var timedEvent = state.Events[i];
                if (state.FiredEvents.Contains(timedEvent)) continue;
                if (timedEvent.NormalizedTime > normalizedTime) continue;

                state.FiredEvents.Add(timedEvent);
                OnEventFired?.Invoke(track, timedEvent);
            }
        }

        private TrackState GetStateOrNull(AnimStateEventTrack track)
        {
            trackStates.TryGetValue(track, out var state);
            return state;
        }

        private TimedEvent FindEventOrNull(TrackState state, string eventName)
        {
            for (int i = 0; i < state.Events.Count; i++)
            {
                if (state.Events[i].EventName == eventName) return state.Events[i];
            }
            return null;
        }
    }
}


