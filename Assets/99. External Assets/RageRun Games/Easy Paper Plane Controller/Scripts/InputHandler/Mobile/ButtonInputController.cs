using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RageRunGames.EasyPaperPlaneController
{
    public abstract class ButtonInputController : MonoBehaviour
    {
        [SerializeField] private EventTrigger buttonTrigger;
        protected Image ButtonImage;

        public bool IsActive { get; protected set; }

        protected virtual void Start()
        {
            ButtonImage = GetComponent<Image>();
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            ButtonImage.color = IsActive ? Color.gray : Color.white;
        }
    }
}