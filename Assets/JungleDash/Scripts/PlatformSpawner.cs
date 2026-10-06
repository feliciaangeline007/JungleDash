// PlatformSpawner.cs – Procedurally spawns and recycles platforms, coins, and enemies.
// No per-frame allocations after warm-up.
using System.Collections.Generic;
using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] platformPrefabs;   // various widths / styles
    public GameObject coinPrefab;
    public GameObject[] enemyPrefabs;      // monkey, venus fly trap

    [Header("Spawn Settings")]
    public Transform playerTransform;
    public float spawnAheadDistance = 24f; // spawn this far ahead of player
    public float despawnBehindDistance = 12f;
    public float minPlatformGap  = 1.5f;
    public float maxPlatformGap  = 3.5f;
    public float minPlatformY    = -1.5f;
    public float maxPlatformY    =  2.5f;

    [Header("Difficulty")]
    [Range(0f,1f)] public float enemyChance = 0.3f;
    [Range(0f,1f)] public float coinChance  = 0.7f;
    public int coinsPerPlatform = 3;

    // Pools
    Stack<GameObject>[] _platPools;
    Stack<GameObject>   _coinPool  = new Stack<GameObject>(64);
    Stack<GameObject>[] _enemyPools;

    List<GameObject> _activePlats  = new List<GameObject>(32);
    List<GameObject> _activeCoins  = new List<GameObject>(64);
    List<GameObject> _activeEnemies = new List<GameObject>(32);

    float _nextSpawnX;

    void Awake() => WarmUp();

    void WarmUp()
    {
        // Platform pools
        _platPools = new Stack<GameObject>[platformPrefabs != null ? platformPrefabs.Length : 0];
        if (platformPrefabs != null)
            for (int i = 0; i < platformPrefabs.Length; i++)
            {
                _platPools[i] = new Stack<GameObject>(16);
                for (int j = 0; j < 8; j++) _platPools[i].Push(MakeInactive(platformPrefabs[i]));
            }

        // Coin pool
        if (coinPrefab != null)
            for (int i = 0; i < 48; i++) _coinPool.Push(MakeInactive(coinPrefab));

        // Enemy pools
        _enemyPools = new Stack<GameObject>[enemyPrefabs != null ? enemyPrefabs.Length : 0];
        if (enemyPrefabs != null)
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                _enemyPools[i] = new Stack<GameObject>(8);
                for (int j = 0; j < 6; j++) _enemyPools[i].Push(MakeInactive(enemyPrefabs[i]));
            }
    }

    void Start()
    {
        if (playerTransform == null) return;
        _nextSpawnX = playerTransform.position.x + 4f;
        // Spawn initial burst
        for (int i = 0; i < 12; i++) SpawnNext();
    }

    void Update()
    {
        if (playerTransform == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        float playerX = playerTransform.position.x;

        // Spawn ahead
        while (_nextSpawnX < playerX + spawnAheadDistance)
            SpawnNext();

        // Recycle behind
        RecycleBehind(playerX);
    }

    void SpawnNext()
    {
        if (platformPrefabs == null || platformPrefabs.Length == 0) return;

        int idx   = Random.Range(0, platformPrefabs.Length);
        float gap = Random.Range(minPlatformGap, maxPlatformGap);
        float y   = Random.Range(minPlatformY, maxPlatformY);

        GameObject plat = PopPool(_platPools, idx, platformPrefabs[idx]);
        plat.transform.position = new Vector3(_nextSpawnX + gap, y, 0f);
        plat.SetActive(true);
        _activePlats.Add(plat);

        // Get platform width
        float platW = 3f;
        SpriteRenderer sr = plat.GetComponent<SpriteRenderer>();
        if (sr) platW = sr.bounds.size.x;

        // Spawn coins on platform
        if (coinPrefab != null && Random.value < coinChance)
        {
            float coinY = y + 0.7f;
            int n = coinsPerPlatform;
            float step = platW / (n + 1f);
            float startX = plat.transform.position.x - platW * 0.5f + step;
            for (int c = 0; c < n; c++)
            {
                if (_coinPool.Count == 0) break;
                GameObject coin = _coinPool.Pop();
                coin.transform.position = new Vector3(startX + c * step, coinY, 0f);
                coin.SetActive(true);
                _activeCoins.Add(coin);
            }
        }

        // Spawn enemy on platform
        if (enemyPrefabs != null && enemyPrefabs.Length > 0 && Random.value < enemyChance)
        {
            int ei = Random.Range(0, enemyPrefabs.Length);
            GameObject enemy = PopPool(_enemyPools, ei, enemyPrefabs[ei]);
            float ex = plat.transform.position.x + Random.Range(-platW * 0.3f, platW * 0.3f);
            enemy.transform.position = new Vector3(ex, y + 0.6f, 0f);
            enemy.SetActive(true);
            _activeEnemies.Add(enemy);
        }

        _nextSpawnX = plat.transform.position.x + platW * 0.5f;
    }

    void RecycleBehind(float playerX)
    {
        float cutX = playerX - despawnBehindDistance;

        for (int i = _activePlats.Count - 1; i >= 0; i--)
        {
            if (_activePlats[i] == null || _activePlats[i].transform.position.x < cutX)
            {
                ReturnToPool(_activePlats[i], platformPrefabs, _platPools);
                _activePlats.RemoveAt(i);
            }
        }
        for (int i = _activeCoins.Count - 1; i >= 0; i--)
        {
            if (_activeCoins[i] == null || _activeCoins[i].transform.position.x < cutX)
            {
                if (_activeCoins[i] != null) { _activeCoins[i].SetActive(false); _coinPool.Push(_activeCoins[i]); }
                _activeCoins.RemoveAt(i);
            }
        }
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] == null || _activeEnemies[i].transform.position.x < cutX)
            {
                ReturnToPool(_activeEnemies[i], enemyPrefabs, _enemyPools);
                _activeEnemies.RemoveAt(i);
            }
        }
    }

    static GameObject MakeInactive(GameObject prefab)
    {
        GameObject go = Instantiate(prefab);
        go.SetActive(false);
        return go;
    }

    static GameObject PopPool(Stack<GameObject>[] pools, int idx, GameObject prefab)
    {
        if (idx < pools.Length && pools[idx] != null && pools[idx].Count > 0)
            return pools[idx].Pop();
        return MakeInactive(prefab); // expand pool if empty
    }

    static void ReturnToPool(GameObject go, GameObject[] prefabs, Stack<GameObject>[] pools)
    {
        if (go == null) return;
        go.SetActive(false);
        if (prefabs == null) return;
        for (int i = 0; i < prefabs.Length; i++)
            if (prefabs[i] != null && go.name.StartsWith(prefabs[i].name))
            { pools[i].Push(go); return; }
    }
}
