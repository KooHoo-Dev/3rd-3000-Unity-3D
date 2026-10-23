using System;
using UnityEngine;

namespace Jay.AnimStateEvent
{
    [Serializable]
    public class TimedEvent
    {
        [SerializeField] private string eventName;
        [SerializeField] [Range(0f, 1f)] private float normalizedTime;
        [SerializeField] private string parameter;

        public string EventName => eventName;

        public float NormalizedTime
        {
            get => normalizedTime;
            set => Mathf.Clamp(value, 0f, 1f);
        }
        public string Parameter => parameter;

        public TimedEvent() { }

        public TimedEvent(string eventName, float normalizedTime, string parameter = null)
        {
            this.eventName = eventName;
            this.normalizedTime = normalizedTime;
            this.parameter = parameter;
        }

        public TimedEvent Clone()
        {
            return new TimedEvent(eventName, normalizedTime, parameter);
        }
    }
}