using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Header("Pool Setting")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 10;
    [SerializeField] private bool canExpand = true;
    [SerializeField] private Transform poolParent;

    public static ObjectPool Instance { get; private set; } // 싱글톤 인스턴스, BulletBase에서 접근하기 위해 public으로 설정

    private Queue<GameObject> pool = new Queue<GameObject>();
    private HashSet<GameObject> activeObjects = new HashSet<GameObject>();

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateObject();
        }
    }

    private GameObject CreateObject()
    {
        GameObject obj = Instantiate(prefab, poolParent);

        obj.SetActive(false);

        PoolObject poolObject = obj.GetComponent<PoolObject>();

        if (poolObject == null)
        {
            poolObject = obj.AddComponent<PoolObject>();
        }

        poolObject.OwnerPool = this;

        pool.Enqueue(obj);

        return obj;
    }

    // 기본 Get
    public GameObject Get()
    {
        if (pool.Count == 0)
        {
            if (canExpand)
            {
                CreateObject();
            }
            else
            {
                Debug.LogWarning("Pool is empty.");
                return null;
            }
        }

        GameObject obj = pool.Dequeue();

        activeObjects.Add(obj);

        obj.SetActive(true);

        return obj;
    }

    // 위치/회전 지정 Get
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = Get();

        if (obj == null) return null;

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        Debug.Log($"[Pool] 발사 위치: {obj.transform.position}");

        return obj;
    }

    // 반납
    public void Return(GameObject obj)
    {
        if (!activeObjects.Contains(obj))
        {
            Debug.LogWarning($"{obj.name} is already returned.");
            return;
        }

        activeObjects.Remove(obj);

        obj.SetActive(false);

        obj.transform.SetParent(poolParent);

        pool.Enqueue(obj);
    }
}