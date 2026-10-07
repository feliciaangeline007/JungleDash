// TrackSpawner.cs – Pools and recycles track segments + content.
using System.Collections.Generic;
using UnityEngine;

public class TrackSpawner : MonoBehaviour
{
    [Header("Segment")]
    public TrackSegment segmentPrefab;
    public int   segmentCount       = 6;
    public int   emptyStartSegments = 2;

    [Header("Content")]
    public float rowSpacing  = 7.5f;
    public float laneWidth   = 2f;

    [Header("Obstacles")]
    public GameObject[] obstaclePrefabs;
    [Range(0,1)] public float obstacleChance = 0.55f;

    [Header("Coins")]
    public GameObject coinPrefab;
    public GameObject gemPrefab;
    [Range(0,1)] public float coinChance = 0.7f;
    [Range(0,1)] public float gemChance  = 0.15f;
    public int coinsPerLine = 5;

    // Segment pool
    readonly List<TrackSegment> _active = new List<TrackSegment>(8);
    float _nextZ;

    // Coin / gem pools
    readonly Stack<GameObject> _coinPool = new Stack<GameObject>(64);
    readonly Stack<GameObject> _gemPool  = new Stack<GameObject>(16);

    // Obstacle pools per prefab index
    Dictionary<int, Stack<GameObject>> _obsPools = new Dictionary<int, Stack<GameObject>>();

    // Items per segment for recycling
    Dictionary<TrackSegment, List<GameObject>> _items = new Dictionary<TrackSegment, List<GameObject>>();

    readonly bool[] _blocked = new bool[3];

    Transform _player;

    void Awake() => WarmUp();

    void WarmUp()
    {
        if (coinPrefab != null)
            for (int i = 0; i < 48; i++) _coinPool.Push(Spawn(coinPrefab));
        if (gemPrefab != null)
            for (int i = 0; i < 12; i++) _gemPool.Push(Spawn(gemPrefab));
        if (obstaclePrefabs != null)
            for (int t = 0; t < obstaclePrefabs.Length; t++)
            {
                if (obstaclePrefabs[t] == null) continue;
                var stk = new Stack<GameObject>(12);
                for (int i = 0; i < 10; i++) stk.Push(Spawn(obstaclePrefabs[t]));
                _obsPools[t] = stk;
            }
    }

    static GameObject Spawn(GameObject pfb) { var g = Object.Instantiate(pfb); g.SetActive(false); return g; }

    void Start()
    {
        _player = GameManager.Instance != null ? GameManager.Instance.PlayerTransform : null;
        for (int i = 0; i < segmentCount; i++) AddSegment(i < emptyStartSegments);
    }

    void Update()
    {
        if (_player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;
        float pz = _player.position.z;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].EndZ < pz - _active[i].segmentLength)
            {
                Recycle(_active[i]);
                _active.RemoveAt(i);
                AddSegment(false);
            }
        }
    }

    void AddSegment(bool empty)
    {
        if (segmentPrefab == null) return;
        TrackSegment seg = Object.Instantiate(segmentPrefab, new Vector3(0, 0, _nextZ), Quaternion.identity);
        seg.ApplyGroundScale();
        _nextZ += seg.segmentLength;
        _active.Add(seg);
        _items[seg] = new List<GameObject>(16);
        if (!empty) Populate(seg);
    }

    void Recycle(TrackSegment seg)
    {
        if (_items.TryGetValue(seg, out var list))
        {
            foreach (var g in list) { if (g != null) ReturnItem(g); }
            list.Clear();
        }
        seg.ResetTo(new Vector3(0, 0, _nextZ));
        seg.ApplyGroundScale();
        _nextZ += seg.segmentLength;
    }

    void ReturnItem(GameObject g)
    {
        g.SetActive(false);
        if (coinPrefab != null && g.name.StartsWith(coinPrefab.name)) { _coinPool.Push(g); return; }
        if (gemPrefab  != null && g.name.StartsWith(gemPrefab.name))  { _gemPool.Push(g);  return; }
        if (obstaclePrefabs != null)
            for (int t = 0; t < obstaclePrefabs.Length; t++)
                if (obstaclePrefabs[t] != null && g.name.StartsWith(obstaclePrefabs[t].name) && _obsPools.TryGetValue(t, out var s))
                { s.Push(g); return; }
    }

    void Populate(TrackSegment seg)
    {
        float z0  = seg.transform.position.z;
        float z1  = z0 + seg.segmentLength;
        int prevFree = 1;

        for (float rz = z0 + rowSpacing; rz < z1 - 1f; rz += rowSpacing)
        {
            _blocked[0] = _blocked[1] = _blocked[2] = false;

            // Obstacles
            if (obstaclePrefabs != null && obstaclePrefabs.Length > 0 && Random.value < obstacleChance)
            {
                int n = Random.Range(1, 3); int placed = 0; int safety = 0;
                while (placed < n && safety++ < 20)
                {
                    int l = Random.Range(0, 3);
                    if (!_blocked[l] && placed + 1 < 3) { _blocked[l] = true; placed++; }
                }
                int thisFree = FreeLane(_blocked);
                if (Mathf.Abs(thisFree - prevFree) > 1)
                {
                    int dir = prevFree > thisFree ? 1 : -1;
                    int nf  = Mathf.Clamp(thisFree + dir, 0, 2);
                    _blocked[thisFree] = true; _blocked[nf] = false; thisFree = nf;
                }
                prevFree = thisFree;
                int obsType = Random.Range(0, obstaclePrefabs.Length);
                for (int l = 0; l < 3; l++)
                    if (_blocked[l]) SpawnObs(obsType, new Vector3((l-1)*laneWidth, 0, rz), seg);
            }

            // Coins
            if (coinPrefab != null && Random.value < coinChance)
            {
                int fl = FreeLane(_blocked);
                if (fl >= 0) { SpawnCoins((fl-1)*laneWidth, rz, seg); _blocked[fl] = true; }
            }
        }
    }

    int FreeLane(bool[] b) { if (!b[1]) return 1; if (!b[0]) return 0; if (!b[2]) return 2; return -1; }

    void SpawnObs(int t, Vector3 pos, TrackSegment seg)
    {
        if (!_obsPools.TryGetValue(t, out var pool) || pool.Count == 0) return;
        var g = pool.Pop(); g.transform.position = pos; g.SetActive(true);
        _items[seg].Add(g);
    }

    void SpawnCoins(float x, float cz, TrackSegment seg)
    {
        int h = coinsPerLine / 2;
        for (int i = -h; i <= h; i++)
        {
            bool isGem = (i == 0) && Random.value < gemChance;
            Stack<GameObject> pool = isGem ? _gemPool : _coinPool;
            if (pool.Count == 0) continue;
            var g = pool.Pop();
            g.transform.position = new Vector3(x, isGem ? 0.7f : 0.55f, cz + i * 1.2f);
            g.SetActive(true);
            _items[seg].Add(g);
        }
    }
}
