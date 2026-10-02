
namespace RageRunGames.EasyPaperPlaneController
{
        public class LateralForceInputController : ButtonInputController
        {
            public void SetLateralForce(bool isLateralForceDown)
            {
                SetActive(isLateralForceDown);
            }
        }
}