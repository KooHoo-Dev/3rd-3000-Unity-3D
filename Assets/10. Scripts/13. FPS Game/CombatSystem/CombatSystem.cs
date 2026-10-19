using System;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.FpsGame
{
    public class CombatSystem : MonoBehaviour
    {
        [Header("처리량")]
        [Tooltip("한 프레임에 처리할 최대 전투 이벤트 수. 남은 이벤트는 다음 프레임으로 넘어갑니다")]
        [SerializeField] private int maxEventsPerFrame = 10;

        [Header("적 판정")]
        [Tooltip("총·유탄이 맞힐 적의 레이어 이름. Project Settings > Tags and Layers의 이름과 같아야 합니다")]
        [SerializeField] private string enemyLayerName = "Enemy";

        public static CombatSystem Instance { get; private set; }

        public int EnemyMask { get; private set; }

        public event Action<CombatEvent> OnCombatEvent;
        public event Action<HealEvent> OnHealEvent;

        private readonly Dictionary<Collider, IDamageable> monsterDic = new Dictionary<Collider, IDamageable>();
        private readonly Queue<InGameEvent> inGameEventQueue = new Queue<InGameEvent>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{name}: CombatSystem이 이미 있습니다. 이 컴포넌트는 쓰지 않습니다");
                enabled = false;
                return;
            }

            Instance = this;
            EnemyMask = LayerMask.GetMask(enemyLayerName);
            if (EnemyMask == 0) Debug.LogWarning($"{enemyLayerName} 레이어가 없어 총·유탄이 아무것도 맞히지 못합니다");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            int processCount = 0;

            while (inGameEventQueue.Count > 0 && processCount < maxEventsPerFrame)
            {
                var inGameEvent = inGameEventQueue.Dequeue();
                processCount++;

                if (inGameEvent.Receiver is UnityEngine.Object receiverObject && receiverObject == null) continue;

                switch (inGameEvent.Type)
                {
                    case InGameEvent.EventType.Combat:
                        var combatEvent = inGameEvent as CombatEvent;
                        inGameEvent.Receiver.TakeDamage(combatEvent);
                        OnCombatEvent?.Invoke(combatEvent);
                        break;
                    case InGameEvent.EventType.Heal:
                        var healEvent = inGameEvent as HealEvent;
                        inGameEvent.Receiver.TakeHeal(healEvent);
                        OnHealEvent?.Invoke(healEvent);
                        break;
                }
            }
        }

        public void RegisterMonster(IDamageable monster)
        {
            RegisterMonster(monster.MainCollider, monster);
        }

        public void RegisterMonster(Collider collider, IDamageable monster)
        {
            if (monsterDic.TryAdd(collider, monster) == false)
            {
                Debug.LogWarning($"{monster.GameObject.name}가 등록되어 있습니다." +
                                 $"{monsterDic[collider]}를 대체합니다");
                monsterDic[collider] = monster;
            }
        }

        public void UnregisterMonster(Collider collider)
        {
            monsterDic.Remove(collider);
        }

        public IDamageable GetMonsterOrNull(Collider collider)
        {
            if (monsterDic.TryGetValue(collider, out var monster)) return monster;
            return null;
        }

        public void AddInGameEvent(InGameEvent inGameEvent)
        {
            inGameEventQueue.Enqueue(inGameEvent);
        }
    }
}
