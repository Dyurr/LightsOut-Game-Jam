using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;

public class LevelGenerator : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;


    [Header("Rooms")]
    [SerializeField] private GameObject starterRoomPrefab;
    [SerializeField] private List<GameObject> roomPrefabs; // pool for hallways/bedrooms etc
    [SerializeField] private GameObject endRoomPrefab; // the single exit-door room
    [SerializeField] private GameObject wallCapPrefab;

    [Header("Branch Depth")]
    [SerializeField] private int minDeadEndDepth = 1;
    [SerializeField] private int maxDeadEndDepth = 3;
    [SerializeField] private int minExitDepth = 3;
    [SerializeField] private int maxExitDepth = 5;

    [Header("Safety")]
    [SerializeField] private int maxTotalRooms = 20; // hard cap so branching can't spiral out of control


    private bool exitRoomSpawned = false;
    private List<Transform> exitBranchDeadEnds = new List<Transform>();



    // Hard cap on rooms
    private int globalRoomCap = 40;     
    private int totalRoomsSpawned = 0;

    [Header("Valuables")]
    [SerializeField] private List<GameObject> itemPrefabs;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;

    private List<Bounds> placedRoomBounds = new List<Bounds>();


    [SerializeField] private NavMeshSurface navMeshSurface;

private void Start()
{
    if (starterRoomPrefab == null)
    {
        Debug.LogWarning("No starter room prefab assigned");
        return;
    }

    GameObject starterRoom = Instantiate(starterRoomPrefab, Vector3.zero, Quaternion.identity);


    /// Player Spawn
    Transform playerSpawn = starterRoom.transform.Find("SpawnPoint");

    if (playerSpawn != null && playerPrefab != null)
    {
        Instantiate(playerPrefab, playerSpawn.position, playerSpawn.rotation);
    }

    placedRoomBounds.Add(GetRoomBounds(starterRoom));
    SpawnValuables(starterRoom);
    totalRoomsSpawned++;

    Transform spawnLocs = starterRoom.transform.Find("SpawnLocs");
    if (spawnLocs == null || spawnLocs.childCount == 0)
    {
        Debug.LogWarning("Starter room has no SpawnLocs");
        return;
    }

    int exitBranchIndex = Random.Range(0, spawnLocs.childCount);

    // Give each starter branch its own slice of the room budget so none can starve the others
    int budgetPerBranch = Mathf.Max(maxTotalRooms / spawnLocs.childCount, maxExitDepth + 2);

    for (int i = 0; i < spawnLocs.childCount; i++)
    {
        Transform exit = spawnLocs.GetChild(i);
        bool isExitBranch = (i == exitBranchIndex);

        int depth = isExitBranch
            ? Random.Range(minExitDepth, maxExitDepth + 1)
            : Random.Range(minDeadEndDepth, maxDeadEndDepth + 1);

        GenerateBranch(exit, depth, isExitBranch, null, isStarterExit: true, branchRoomsSpawned: 0, branchRoomBudget: budgetPerBranch);
    }

    if (!exitRoomSpawned)
        ForceSpawnExitRoom();

    if (navMeshSurface != null)
        navMeshSurface.BuildNavMesh();
}

private void GenerateBranch(Transform incomingExit, int remainingDepth, bool isExitBranch, GameObject lastPrefabUsed,
    bool isStarterExit, int branchRoomsSpawned, int branchRoomBudget)
{
    if (branchRoomsSpawned >= branchRoomBudget || totalRoomsSpawned >= globalRoomCap)
    {
        if (isExitBranch) exitBranchDeadEnds.Add(incomingExit);
        else if (wallCapPrefab != null) Instantiate(wallCapPrefab, incomingExit.position, incomingExit.rotation);
        return;
    }

    bool isFinalRoom = remainingDepth <= 0;

    if (isFinalRoom && isExitBranch)
    {
        bool placed = TryPlaceRoom(endRoomPrefab, incomingExit, remainingDepth, isExitBranch, lastPrefabUsed, allowRetry: false,
            isStarterExit, branchRoomsSpawned, branchRoomBudget);
        if (placed) exitRoomSpawned = true;
        else exitBranchDeadEnds.Add(incomingExit); // couldn't place even with allowRetry:false — save slot for forced retry
        return;
    }

    if (isFinalRoom)
    {
        if (wallCapPrefab != null)
            Instantiate(wallCapPrefab, incomingExit.position, incomingExit.rotation);
        return;
    }

    int maxAttempts = isStarterExit ? 10 : 4;
    List<GameObject> triedPrefabs = new List<GameObject>();

    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        GameObject candidate = PickRandomPrefab(lastPrefabUsed, triedPrefabs);
        if (candidate == null) break;

        bool success = TryPlaceRoom(candidate, incomingExit, remainingDepth, isExitBranch, lastPrefabUsed, allowRetry: true,
            isStarterExit, branchRoomsSpawned, branchRoomBudget);
        if (success) return;

        triedPrefabs.Add(candidate);
    }

    // Ran out of attempts placing a connector room on the exit branch — this exit is now unreachable this way
    if (isExitBranch) exitBranchDeadEnds.Add(incomingExit);
    else if (wallCapPrefab != null) Instantiate(wallCapPrefab, incomingExit.position, incomingExit.rotation);
}

// Returns true if the room was placed successfully (no overlap), false if it was destroyed due to overlap
private bool TryPlaceRoom(GameObject prefabToUse, Transform incomingExit, int remainingDepth, bool isExitBranch, GameObject lastPrefabUsed,
    bool allowRetry, bool isStarterExit, int branchRoomsSpawned, int branchRoomBudget)
{
    if (prefabToUse == null) return false;

    GameObject newRoom = Instantiate(prefabToUse);
    Transform entryPoint = newRoom.transform.Find("SpawnAt");

    if (entryPoint == null)
    {
        Debug.LogWarning($"{prefabToUse.name} is missing a SpawnAt transform");
        Destroy(newRoom);
        return false;
    }

    AlignRoomToPoint(newRoom, entryPoint, incomingExit);

    Bounds newBounds = GetRoomBounds(newRoom);
    Bounds shrunkNewBounds = ShrinkBounds(newBounds, 0.3f);
    bool overlaps = false;
    foreach (Bounds existing in placedRoomBounds)
    {
        if (shrunkNewBounds.Intersects(ShrinkBounds(existing, 0.3f)))
        {
            overlaps = true;
            break;
        }
    }

    if (overlaps)
    {
        if (allowRetry)
        {
            Destroy(newRoom);
            return false;
        }
        Debug.LogWarning($"{prefabToUse.name} overlaps but has no fallback - keeping anyway.");
    }

    placedRoomBounds.Add(newBounds);
    totalRoomsSpawned++;
    int newBranchCount = branchRoomsSpawned + 1;
    SpawnValuables(newRoom);

    bool isFinalRoom = remainingDepth <= 0;
    if (isFinalRoom && isExitBranch)
    {    exitRoomSpawned = true;
        return true;
    }
    Transform spawnLocs = newRoom.transform.Find("SpawnLocs");
    if (spawnLocs == null || spawnLocs.childCount == 0)
        return true;

    for (int i = 0; i < spawnLocs.childCount; i++)
    {
        Transform exit = spawnLocs.GetChild(i);
        if (i == 0)
        {
            GenerateBranch(exit, remainingDepth - 1, isExitBranch, prefabToUse, isStarterExit, newBranchCount, branchRoomBudget);
        }
        else
        {
            int subBranchDepth = Random.Range(minDeadEndDepth, maxDeadEndDepth + 1);
            GenerateBranch(exit, subBranchDepth, false, prefabToUse, false, newBranchCount, branchRoomBudget);
        }
    }

    return true;
}
private GameObject PickRandomPrefab(GameObject excludePrefab, List<GameObject> alsoExclude)
{
    List<GameObject> options = roomPrefabs
        .Where(p => p != excludePrefab && !alsoExclude.Contains(p))
        .ToList();

    if (options.Count == 0) return null;
    return options[Random.Range(0, options.Count)];
}

    private void SpawnValuables(GameObject room)
{
    if (itemPrefabs == null || itemPrefabs.Count == 0) return;
    Transform valuables = room.transform.Find("Valuables");
    if (valuables == null) return;

    // Snapshot the points first — don't iterate the live hierarchy
    List<Transform> points = new List<Transform>();
    foreach (Transform point in valuables)
        points.Add(point);

    foreach (Transform point in points)
    {
        if (Random.value <= spawnChance)
        {
            GameObject chosenItem = itemPrefabs[Random.Range(0, itemPrefabs.Count)];
            Instantiate(chosenItem, point.position, point.rotation, valuables);
        }
    }
}

    private void AlignRoomToPoint(GameObject newRoom, Transform entryPoint, Transform targetExit)
    {
        float angleDiff = Vector3.SignedAngle(entryPoint.forward, targetExit.forward, Vector3.up);
        newRoom.transform.Rotate(Vector3.up, angleDiff);
        Vector3 offset = targetExit.position - entryPoint.position;
        newRoom.transform.position += offset;
    }

    private Bounds GetRoomBounds(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(room.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
    


    private Bounds ShrinkBounds(Bounds original, float margin)
{
    Bounds shrunk = original;
    shrunk.Expand(-margin * 2f);
    return shrunk;
}


    private void ForceSpawnExitRoom()
{
    if (endRoomPrefab == null || exitBranchDeadEnds.Count == 0)
    {
        Debug.LogError("Could not force-spawn exit room — no valid slots recorded!");
        return;
    }

    foreach (Transform slot in exitBranchDeadEnds)
    {
        if (slot == null) continue; // slot's GameObject may have been destroyed (e.g. wall cap swapped in)

        bool placed = TryPlaceRoom(endRoomPrefab, slot, 0, true, null, allowRetry: false,
            isStarterExit: false, branchRoomsSpawned: 0, branchRoomBudget: int.MaxValue);
        if (placed)
        {
            exitRoomSpawned = true;
            return;
        }
    }

    Debug.LogError("Exhausted all fallback slots — exit room still not placed!");
}

}