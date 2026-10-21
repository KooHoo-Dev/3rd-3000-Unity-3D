using Study3D.FpsGame.Weapon;
using UnityEngine;

namespace Study3D.FpsGame.Player
{
    // LocalPlayer
    // : 유저가 직접 제어하는 게임 속 내의 플레이어 객체
    //  HP, 무기 제어, 아이템 줍기, 재장전, 무기 교체, 다양한 알림등
    //  게임 이벤트(게임의 고유한 사건)와 연결되어있는 주요 클래스 입니다.
    // (고유한 : 내가 만든 게임이면 다른 게임이랑 처리되는 내용이 다르겠죠?
    //  로아의 전투 처리와 리니의 전투 처리가 다른것처럼. 그 고유한임)
    
    public partial class LocalPlayer : Player, IDamageable
    {
        [Header("Stat")] 
        [SerializeField] private int maxHp = 100;

        [Header("Settings.Weapon")] [SerializeField]
        private Weapon.Weapon.WeaponType startWeapon 
            = Weapon.Weapon.WeaponType.AssaultRifle;
        
        // Weapon을 연결하기 위한 참조들을 설정해 줍니다.
        [SerializeField] private Transform weaponParent;
        [SerializeField] private WeaponController weaponController;
        
        [Header("애니메이션 콜백")]
        [SerializeField] private string reloadCompleteEvent = "ReloadComplete";
        [SerializeField] private string swapCompleteEvent = "SwapComplete";
        
        [Header("Ref")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AnimStateEventListener animStateEventListener;

        // (제 방식) MainCollider를 따로 지정하는것을 선호합니다. 
        public Collider MainCollider => characterController;
        // 이 부분은 인터페이스 보시면 됩니다.
        public GameObject GameObject => gameObject;
        public Weapon.Weapon CurrentWeapon { get; private set; }
        public Health Health { get; private set; } // 단순 데미지 차감용
        public bool IsDead { get; private set; }
        public bool IsReloading { get; private set; } = false;

        #region 유니티 함수들

        private void Awake()
        {
            Player.LocalPlayer = this;
            Health = new Health(maxHp);
        }
        
        private void OnDestroy()
        {
            if (Player.LocalPlayer == this) Player.LocalPlayer = null;
        }

        private void Start()
        {
            Weapon.Weapon changeTarget = weaponController.GetWeaponOrNull(startWeapon);
            ChangeWeapon(changeTarget);
        }

        // 인스펙터에 아직 연결되지 않은 무기 슬롯이면 교체하지 않습니다.
        private void TryChangeWeapon(Weapon.Weapon.WeaponType type)
        {
            Weapon.Weapon target = weaponController.GetWeaponOrNull(type);
            if (target == null) return;
            ChangeWeapon(target);
        }

        private void OnEnable()
        {
            animStateEventListener.OnOccursAnimStateEvent += OnAnimEvent;
        }

        private void OnDisable()
        {
            animStateEventListener.OnOccursAnimStateEvent -= OnAnimEvent;
        }

        #endregion

        #region 애니메이션 이벤트 처리
        private void OnAnimEvent(string eventName, string eventParam)
        {
            // 재장전이 완료된 시점에 호출되는 애니메이션 상태 이벤트 입니다.
            if (eventName == reloadCompleteEvent)
            {
                IsReloading = false;
                // 현재 무기에 리로드 하라고 명령 합니다.
                CurrentWeapon.Reload();
                // 리로드 완료 시점의 델리게이트를 깨워줍니다.
                // (알림해놓은 구독자들에게 완료 됐음을 전달합니다.)
                OnEndedReload?.Invoke();
            }
            // 무기가 교체 애니메이션 상태가 완료된 시점에 호출되는 이벤트 입니다.
            else if (eventName == swapCompleteEvent)
            {
                OnEndedChangedWeapon?.Invoke();
            }
        }
        #endregion

        #region 총기 명령

        public bool Fire()
        {
            return CurrentWeapon.Fire();
        }

        // 발사 버튼을 뗀 순간. 실제로 발사가 일어났으면 true
        public bool ReleaseFire()
        {
            return CurrentWeapon.ReleaseFire();
        }

        public void RequestReload()
        {
            if (IsReloading) return;
            IsReloading = true;
            OnRequestedReload?.Invoke();
        }

        public void ChangeWeapon(Weapon.Weapon weapon)
        {
            if (CurrentWeapon != null)
            {
                var prevWeapon = CurrentWeapon;
                prevWeapon.gameObject.SetActive(false);
            }

            CurrentWeapon = weapon;
            CurrentWeapon.gameObject.SetActive(true);
            OnStartChangedWeapon?.Invoke();
            CurrentWeapon.Reload(); // 재장전 애니메이션 재생 없이. 그냥 탄창 채워주는 겁니다.
        }

        #endregion
        
        #region 게임 이벤트 처리

        public void TakeDamage(CombatEvent combatEvent)
        {
            if (IsDead) return;

            Health.TakeDamage(combatEvent.Damage);
            OnTakenDamage?.Invoke(combatEvent.Damage, Health.HP, Health.MaxHP);

            if (Health.IsDead)
            {
                IsDead = true;
                OnDied?.Invoke();
            }
        }

        public void TakeHeal(HealEvent healEvent)
        {
            if (IsDead) return;
            Health.Heal(healEvent.Heal);
        }

        #endregion
        
        private void OnTriggerEnter(Collider other)
        {
            var triggerItem = other.GetComponent<TriggerItem>();
            if (triggerItem == null) return;

            TouchItemEvent touchItemEvent = new TouchItemEvent();
            touchItemEvent.Type = triggerItem.Type;
            touchItemEvent.Position = other.ClosestPoint(transform.position);
            OnTouchedItem?.Invoke(touchItemEvent);

            var weaponType = Mapper.GetWeaponType(triggerItem.Type);
            var changeTarget = weaponController.GetWeaponOrNull(weaponType);
            if (changeTarget != null)
            {
                if (CurrentWeapon != changeTarget) ChangeWeapon(changeTarget);
                else RequestReload();
            }

            Destroy(triggerItem.gameObject);
        }
    }
}