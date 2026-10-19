using System;

namespace Study3D.FpsGame.Player
{
    public partial class LocalPlayer
    {
        //public event Action<TouchItemEvent> OnTouchedItem;
        public event Action OnStartChangedWeapon;
        public event Action OnEndedChangedWeapon;
        public event Action OnRequestedReload;
        public event Action OnEndedReload;
        public event Action<int, int, int> OnTakenDamage; // (데미지, 현재 HP, 최대 HP)
        public event Action OnDied;
    }

    // 원래는 아래처럼 썼습니다.
    // 나만 (남들은 모르겠음)
    // 진짜 잘 안보여서 묶어 씁니다.
    // 성능 안 좋은건 알고 있습니다.
    
    public partial class Legacy_LocalPlayer
    {
        public class Callbacks
        {
            public Action OnStartChangedWeapon;
            public Action OnEndedChangedWeapon;
            public Action OnStartedReload;
            public Action OnEndedReload;
            public Action<int, int, int> OnTakenDamage; // (데미지, 현재 HP, 최대 HP)
            public Action OnDied;
        }

        public Callbacks Events = new Callbacks();
    }

    // 비교해서 사용했을때의 예시
    class TestCode
    {
        public void Note()
        {
            LocalPlayer lp = new LocalPlayer();
            lp.OnEndedChangedWeapon += () => { } ;

            Legacy_LocalPlayer p = new Legacy_LocalPlayer();
            p.Events.OnEndedChangedWeapon = null;
            p.Events.OnStartChangedWeapon = null;
        }
    }
}