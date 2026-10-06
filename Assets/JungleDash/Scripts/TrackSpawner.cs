// TrackSpawner.cs
// Manages an object-pooled ring of TrackSegments.
// Spawns rows of coins, obstacles and power-up pickups on new segments.
// Never calls Instantiate or Destroy during play after warm-up.
// No per-frame allocations.
using System.Collections.Generic;
using UnityEngine;

public class TrackSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Segment Setup")]
    [Tooltip("TrackSegment prefab to instantiate during warm-up.")]
    public TrackSegment segmentPrefab;
    [Tooltip("How many segments to keep in the world at once.")]
    public int segmentCount = 6;
    [Tooltip("How many of the first segments to keep empty (no obstacles/coins).")]
    public int emptyStartSegments = 2;

    [Header("Content Spacing")]
    [Tooltip("Distance in metres between content rows.")]
    public float rowSpacing = 7.5f;

    [Header("Obstacles")]
    [Tooltip("Obstacle prefabs to randomly pick from.")]
    public GameObject[] obstaclePrefabs;
    [Tooltip("Max obstacles per row (1 or 2).")]
    public int maxObstaclesPerRow = 2;
    [Tooltip("Chance a row gets any obstacle at all (0..1).")]
    [Range(0f, 1f)]
    public float obstacleChance = 0.55f;

    [Header("Coins")]
    [Tooltip("Coin prefab.")]
    public GameObject coinPrefab;
    [Tooltip("Gem prefab.")]
    public GameObject gemPrefab;
    [Tooltip("Coins per coin line.")]
    public int coinsPerLine = 5;
    [Tooltip("Chance a free lane gets a coin line.")]
    [Range(0f, 1f)]
    public float coinChance = 0.7f;
    [Tooltip("Chance the middle coin is replaced by a gem.")]
    [Range(0f, 1f)]
    public float gemChance = 0.15f;

    [Header("Power-Ups")]
    [Tooltip("Power-up pickup prefabs (order matches PowerUpType enum).")]
    public GameObject[] powerUpPrefabs;
    [Tooltip("Chance a segment gets one power-up.")]
    [Range(0f, 1f)]
    public float powerUpChance = 0.18f;

    [Header("Lane")]
    [Tooltip("Lane width must match PlayerRunner.laneWidth.")]
    public float laneWidth = 2f;

    // ─────────────────────────────────────────────
    //  Internal pools
    // ─────────────────────────────────────────────
    private List<TrackSegment> _activeSegments = new List<TrackSegment>(8);
    private float _nextSegmentZ = 0f;

    // Object pools: stacks of pre-created GameObjects
    private Stack<GameObject> _coinPool     = new Stack<GameObject>(64);
    private Stack<GameObject> _gemPool      = new Stack<GameObject>(16);
    private Dictionary<int, Stack<GameObject>> _obstaclePools
        = new Dictionary<int, Stack<GameObject>>(8);
    private Dictionary<int, Stack<GameObject>> _puPools
        = new Dictionary<int, Stack<GameObject>>(8);

    // Active spawned items — for recycle on segment reset
    // Each segment has a list of its active items
    private Dictionary<TrackSegment, List<GameObject>> _segmentItems
        = new Dictionary<TrackSegment, List<GameObject>>(8);

    // Reusable list for lane bitmasks (no alloc)
    // laneBlocked[lane+1]: 0=free, 1=blocked
    private readonly bool[] _laneBlocked = new bool[3]; // lanes -1,0,+1 mapped to 0,1,2
    private int _prevFreeLane = 1; // track consecutive row safety

    // Player reference
    private Transform _playerTransform;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // Pre-warm pools
        WarmPools();
    }

    private void Start()
    {
        _playerTransform = GameManager.Instance != null
            ? GameManager.Instance.PlayerTransform : null;

        // Spawn initial segments
        for (int i = 0; i < segmentCount; i++)
            SpawnNextSegment(i < emptyStartSegments);
    }

    private void Update()
    {
        if (_playerTransform == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        // Recycle segments that have fallen behind the player
        float playerZ = _playerTransform.position.z;
        for (int i = _activeSegments.Count - 1; i >= 0; i--)
        {
            TrackSegment seg = _activeSegments[i];
            if (seg.EndZ < playerZ - seg.segmentLength)
            {
                RecycleSegment(seg);
                _activeSegments.RemoveAt(i);
                SpawnNextSegment(false);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Pool warm-up
    // ─────────────────────────────────────────────
    private void WarmPools()
    {
        // Coins
        if (coinPrefab != null)
        {
            for (int i = 0; i < 48; i++)
            {
                GameObject go = Instantiate(coinPrefab);
                go.SetActive(false);
                _coinPool.Push(go);
            }
        }
        // Gems
        if (gemPrefab != null)
        {
            for (int i = 0; i < 12; i++)
            {
                GameObject go = Instantiate(gemPrefab);
                go.SetActive(false);
                _gemPool.Push(go);
            }
        }
        // Obstacles
        if (obstaclePrefabs != null)
        {
            for (int t = 0; t < obstaclePrefabs.Length; t++)
            {
                if (obstaclePrefabs[t] == null) continue;
                var stk = new Stack<GameObject>(16);
                for (int i = 0; i < 12; i++)
                {
                    GameObject go = Instantiate(obstaclePrefabs[t]);
                    go.SetActive(false);
                    stk.Push(go);
                }
                _obstaclePools[t] = stk;
            }
        }
        // Power-ups
        if (powerUpPrefabs != null)
        {
            for (int t = 0; t < powerUpPrefabs.Length; t++)
            {
                if (powerUpPrefabs[t] == null) continue;
                var stk = new Stack<GameObject>(8);
                for (int i = 0; i < 6; i++)
                {
                    GameObject go = Instantiate(powerUpPrefabs[t]);
                    go.SetActive(false);
                    stk.Push(go);
                }
                _puPools[t] = stk;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Segment spawn / recycle
    // ─────────────────────────────────────────────
    private void SpawnNextSegment(bool empty)
    {
        if (segmentPrefab == null) return;

        TrackSegment seg = Instantiate(segmentPrefab,
            new Vector3(0f, 0f, _nextSegmentZ), Quaternion.identity);
        seg.ApplyGroundScale();
        _nextSegmentZ += seg.segmentLength;
        _activeSegments.Add(seg);
        _segmentItems[seg] = new List<GameObject>(32);

        if (!empty) PopulateSegment(seg);
    }

    private void RecycleSegment(TrackSegment seg)
    {
        // Return all spawned items to pools
        if (_segmentItems.TryGetValue(seg, out List<GameObject> items))
        {
            for (int i = 0; i < items.Count; i++)
            {
                GameObject go = items[i];
                if (go == null) continue;
                go.SetActive(false);
                ReturnToPool(go);
            }
            items.Clear();
        }
        // Reposition segment to end of track
        seg.ResetTo(new Vector3(0f, 0f, _nextSegmentZ));
        seg.ApplyGroundScale();
        _nextSegmentZ += seg.segmentLength;
    }

    // Pool-return by tag (coins/gems/obstacles/powerups all have different root components)
    private void ReturnToPool(GameObject go)
    {
        // Try coin
        if (coinPrefab != null && go.name.StartsWith(coinPrefab.name))
        { _coinPool.Push(go); return; }
        if (gemPrefab != null && go.name.StartsWith(gemPrefab.name))
        { _gemPool.Push(go); return; }

        // Try obstacle
        if (obstaclePrefabs != null)
        {
            for (int t = 0; t < obstaclePrefabs.Length; t++)
            {
                if (obstaclePrefabs[t] != null
                    && go.name.StartsWith(obstaclePrefabs[t].name)
                    && _obstaclePools.TryGetValue(t, out var stk))
                { stk.Push(go); return; }
            }
        }
        // Try power-up
        if (powerUpPrefabs != null)
        {
            for (int t = 0; t < powerUpPrefabs.Length; t++)
            {
                if (powerUpPrefabs[t] != null
                    && go.name.StartsWith(powerUpPrefabs[t].name)
                    && _puPools.TryGetValue(t, out var stk))
                { stk.Push(go); return; }
            }
        }
        // Fallback: just disable
        go.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Segment population
    // ─────────────────────────────────────────────
    private void PopulateSegment(TrackSegment seg)
    {
        float startZ  = seg.transform.position.z;
        float endZ    = startZ + seg.segmentLength;
        bool puPlaced = false;

        for (float rowZ = startZ + rowSpacing; rowZ < endZ - 1f; rowZ += rowSpacing)
        {
            // Reset lane blocked flags
            _laneBlocked[0] = _laneBlocked[1] = _laneBlocked[2] = false;

            // ── Obstacles ──────────────────────────────
            if (obstaclePrefabs != null && obstaclePrefabs.Length > 0
                && Random.value < obstacleChance)
            {
                int count  = Random.Range(1, maxObstaclesPerRow + 1);
                // Pick which lanes to block — never all 3
                int blocked = 0;
                int safeCount = 0;
                // Keep trying until at least one lane is free
                while (blocked < count && safeCount < 20)
                {
                    safeCount++;
                    int lane = Random.Range(0, 3);
                    if (!_laneBlocked[lane])
                    {
                        // Count how many lanes would remain free
                        int wouldBlock = blocked + 1;
                        if (wouldBlock < 3)
                        {
                            _laneBlocked[lane] = true;
                            blocked++;
                        }
                    }
                }

                // Consecutive row safety: free lanes must be adjacent
                int thisFreeLane = GetFreeLane(_laneBlocked);
                if (Mathf.Abs(thisFreeLane - _prevFreeLane) > 1)
                {
                    // Shift the free lane 1 step toward previous free lane
                    int dir = (_prevFreeLane > thisFreeLane) ? 1 : -1;
                    int newFree = Mathf.Clamp(thisFreeLane + dir, 0, 2);
                    // Make newFree free, block old free
                    _laneBlocked[thisFreeLane] = true;
                    _laneBlocked[newFree]      = false;
                    thisFreeLane = newFree;
                }
                _prevFreeLane = thisFreeLane;

                // Spawn obstacles in blocked lanes
                int obsType = Random.Range(0, obstaclePrefabs.Length);
                for (int l = 0; l < 3; l++)
                {
                    if (_laneBlocked[l])
                    {
                        float x = (l - 1) * laneWidth;
                        SpawnObstacle(obsType, new Vector3(x, 0f, rowZ), seg);
                    }
                }
            }

            // ── Coins in a free lane ───────────────────
            if (coinPrefab != null && Random.value < coinChance)
            {
                // Pick a free lane
                int freeLane = GetFreeLane(_laneBlocked);
                if (freeLane >= 0)
                {
                    float x = (freeLane - 1) * laneWidth;
                    SpawnCoinLine(x, rowZ, seg);
                    _laneBlocked[freeLane] = true; // mark occupied for PU
                }
            }

            // ── Power-up (at most one per segment) ────
            if (!puPlaced && powerUpPrefabs != null && powerUpPrefabs.Length > 0
                && Random.value < powerUpChance)
            {
                int freeLane = GetFreeLane(_laneBlocked);
                if (freeLane >= 0)
                {
                    float x    = (freeLane - 1) * laneWidth;
                    int   type = Random.Range(0, powerUpPrefabs.Length);
                    SpawnPowerUp(type, new Vector3(x, 1f, rowZ), seg);
                    puPlaced = true;
                }
            }
        }
    }

    // Returns index 0..2 of first free lane, or -1 if all blocked
    private int GetFreeLane(bool[] blocked)
    {
        // Prefer middle
        if (!blocked[1]) return 1;
        if (!blocked[0]) return 0;
        if (!blocked[2]) return 2;
        return -1;
    }

    // ─────────────────────────────────────────────
    //  Spawn helpers (use pools)
    // ─────────────────────────────────────────────
    private void SpawnObstacle(int typeIdx, Vector3 pos, TrackSegment seg)
    {
        if (!_obstaclePools.TryGetValue(typeIdx, out var pool) || pool.Count == 0) return;
        GameObject go = pool.Pop();
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);
        // Reset hidden obstacle if it was hidden by shield
        Obstacle obs = go.GetComponent<Obstacle>();
        if (obs != null) obs.ResetObstacle();
        _segmentItems[seg].Add(go);
    }

    private void SpawnCoinLine(float x, float centerZ, TrackSegment seg)
    {
        int half = coinsPerLine / 2;
        for (int i = -half; i <= half; i++)
        {
            float z = centerZ + i * 1.2f;
            bool isMiddle = (i == 0);
            bool spawnGem = isMiddle && Random.value < gemChance;

            if (spawnGem)
            {
                if (_gemPool.Count == 0) continue;
                GameObject go = _gemPool.Pop();
                go.transform.position = new Vector3(x, 0.6f, z);
                go.SetActive(true);
                _segmentItems[seg].Add(go);
            }
            else
            {
                if (_coinPool.Count == 0) continue;
                GameObject go = _coinPool.Pop();
                go.transform.position = new Vector3(x, 0.5f, z);
                go.SetActive(true);
                _segmentItems[seg].Add(go);
            }
        }
    }

    private void SpawnPowerUp(int typeIdx, Vector3 pos, TrackSegment seg)
    {
        if (!_puPools.TryGetValue(typeIdx, out var pool) || pool.Count == 0) return;
        GameObject go = pool.Pop();
        go.transform.position = pos;
        go.SetActive(true);
        _segmentItems[seg].Add(go);
    }
}
