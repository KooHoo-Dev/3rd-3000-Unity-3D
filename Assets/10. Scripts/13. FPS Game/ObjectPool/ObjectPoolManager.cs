using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.FpsGame
{
    public class ObjectPoolManager : MonoBehaviour
    {
        // 초기화를 위해 ObjectPoolManager에 입력하는 데이터가 됩니다.
        // 런타임에는 해당 데이터 클래스를 사용하지 않습니다.
        // (struct였었는데 왜 바뀌었지..?)
        [Serializable]
        public class ObjectPoolData
        {
            [SerializeField] private string key;
            [SerializeField] private GameObject prefab;
            [SerializeField] private byte expandSize;
            [SerializeField] private Transform parent;

            public string Key => key;
            public GameObject Prefab => prefab;
            public byte ExpandSize => expandSize;
            public Transform Parent => parent;
        }

        private class ObjectPool
        {
            private readonly Stack<IObjectPoolItem> pool = new Stack<IObjectPoolItem>();
            private readonly GameObject prefab;
            private readonly Transform parent;
            private readonly int expandSize;
            private readonly string key;

            public ObjectPool(GameObject prefab, Transform parent, int expandSize, string key)
            {
                this.prefab = prefab;
                this.parent = parent;
                this.expandSize = Mathf.Max(1, expandSize);
                this.key = key;
                Expand();
            }

            private void Expand()
            {
                for (int i = 0; i < expandSize; i++)
                {
                    var instance = Instantiate(prefab, parent).GetComponent<IObjectPoolItem>();
                    instance.Key = key;
                    Return(instance);
                }
            }

            public IObjectPoolItem Get()
            {
                if (pool.Count == 0) Expand();
                return pool.Pop();
            }

            public void Return(IObjectPoolItem item)
            {
                item.GameObject.SetActive(false);
                pool.Push(item);
            }
        }

        public static ObjectPoolManager Instance { get; private set; }

        [Header("만들 풀 목록")]
        [SerializeField] private ObjectPoolData[] objectPoolDatas;

        private readonly Dictionary<string, ObjectPool> objectPoolDic = new Dictionary<string, ObjectPool>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{name}: ObjectPoolManager가 이미 있습니다. 이 컴포넌트는 쓰지 않습니다");
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            foreach (var data in objectPoolDatas)
            {
                CreatePool(data);
            }
        }

        // ObjectPoolData를 이용해서 ObjectPool 한 단위를 만들고
        // 내부 딕셔너리에 등록합니다
        public void CreatePool(ObjectPoolData data)
        {
            if (objectPoolDic.ContainsKey(data.Key))
            {
                Debug.LogWarning($"{data.Key}값이 Pool에 이미 존재합니다.");
                return;
            }

            if (data.Prefab.GetComponent<IObjectPoolItem>() == null)
            {
                Debug.LogWarning($"{data.Prefab.name}의 프리펩에 IObjectPoolItem을" +
                                 $"상속받은 컴포넌트가 없습니다.");
                return;
            }

            Transform poolParent = data.Parent == null ? transform : data.Parent;
            objectPoolDic.Add(data.Key, new ObjectPool(data.Prefab, poolParent, data.ExpandSize, data.Key));
        }

        public IObjectPoolItem GetObjectOrNull(string key)
        {
            if (objectPoolDic.TryGetValue(key, out var pool)) return pool.Get();

            Debug.LogWarning($"{key}값의 Pool이 존재하지 않습니다");
            return null;
        }

        // 아이템을 입력받아서 해당 아이템을 관리하는 풀에 집어넣는 함수입니다.
        // - 어떤 아이템은 그 자체로 귀환할 수 있지만, 그렇지 못하는 케이스에 사용합니다.
        // - 현재의 오브젝트 풀은 새로운 IObjectPoolItem을 push 할 때 게임오브젝트를 비활성화 합니다
        //  (내가 너무 많이 실수를 해서 저만 이렇게 처리할수도 있어요)
        public void ReturnToPool(IObjectPoolItem item)
        {
            if (objectPoolDic.TryGetValue(item.Key, out var pool) == false)
            {
                Debug.LogWarning($"{item.Key}값의 Pool이 존재하지 않습니다");
                return;
            }

            pool.Return(item);
        }
    }
}
