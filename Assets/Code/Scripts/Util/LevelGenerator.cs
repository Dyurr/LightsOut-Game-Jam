using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;

public class LevelGenerator : MonoBehaviour
{
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
        placedRoomBounds.Add(GetRoomBounds(starterRoom));
        SpawnValuables(starterRoom);

        Transform spawnLocs = starterRoom.transform.Find("SpawnLocs");
        if (spawnLocs == null || spawnLocs.childCount == 0)
        {
            Debug.LogWarning("Starter room has no SpawnLocs");
            return;
        }

        // Randomly pick which starter exit becomes the path to the end room
        int exitBranchIndex = Random.Range(0, spawnLocs.childCount);

        for (int i = 0; i < spawnLocs.childCount; i++)
        {
            Transform exit = spawnLocs.GetChild(i);
            bool isExitBranch = (i == exitBranchIndex);

            int depth = isExitBranch
                ? Random.Range(minExitDepth, maxExitDepth + 1)
                : Random.Range(minDeadEndDepth, maxDeadEndDepth + 1);

            GenerateBranch(exit, depth, isExitBranch, null);
        }

        navMeshSurface.BuildNavMesh();
    }

    // Generates a single linear chain of rooms from an exit point
    private void GenerateBranch(Transform incomingExit, int remainingDepth, bool isExitBranch, GameObject lastPrefabUsed)
    {
        bool isFinalRoom = remainingDepth <= 0;

        GameObject prefabToUse;
        if (isFinalRoom && isExitBranch)
        {
            prefabToUse = endRoomPrefab;
        }
        else if (isFinalRoom)
        {
            // Dead end reached, seal it and stop
            if (wallCapPrefab != null)
                Instantiate(wallCapPrefab, incomingExit.position, incomingExit.rotation);
            return;
        }
        else
        {
            prefabToUse = PickRandomPrefab(lastPrefabUsed);
        }

        if (prefabToUse == null)
        {
            Debug.LogWarning("No prefab available to place, sealing with wall instead.");
            if (wallCapPrefab != null)
                Instantiate(wallCapPrefab, incomingExit.position, incomingExit.rotation);
            return;
        }

        GameObject newRoom = Instantiate(prefabToUse);
        Transform entryPoint = newRoom.transform.Find("SpawnAt");

        if (entryPoint == null)
        {
            Debug.LogWarning($"{prefabToUse.name} is missing a SpawnAt transform");
            Destroy(newRoom);
            return;
        }

        AlignRoomToPoint(newRoom, entryPoint, incomingExit);

        Bounds newBounds = GetRoomBounds(newRoom);
        foreach (Bounds existing in placedRoomBounds)
        {
            if (newBounds.Intersects(existing))
            {
                Debug.LogWarning($"{prefabToUse.name} overlaps a previously placed room.");
                break;
            }
        }
        placedRoomBounds.Add(newBounds);

        SpawnValuables(newRoom);

        // End room reached, stop recursing this branch
        if (isFinalRoom && isExitBranch)
            return;

        Transform spawnLocs = newRoom.transform.Find("SpawnLocs");
        if (spawnLocs == null || spawnLocs.childCount == 0)
            return;

        // Continue the chain through exit 0, cap any other exits on this room immediately
        for (int i = 0; i < spawnLocs.childCount; i++)
        {
            Transform exit = spawnLocs.GetChild(i);
            if (i == 0)
            {
                GenerateBranch(exit, remainingDepth - 1, isExitBranch, prefabToUse);
            }
            else if (wallCapPrefab != null)
            {
                Instantiate(wallCapPrefab, exit.position, exit.rotation);
            }
        }
    }

    private GameObject PickRandomPrefab(GameObject excludePrefab)
    {
        List<GameObject> options = roomPrefabs;
        if (excludePrefab != null)
            options = roomPrefabs.Where(p => p != excludePrefab).ToList();

        if (options.Count == 0) return null;
        return options[Random.Range(0, options.Count)];
    }

    private void SpawnValuables(GameObject room)
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0) return;
        Transform valuables = room.transform.Find("Valuables");
        if (valuables == null) return;

        foreach (Transform point in valuables)
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
        float angleDiff = Vector3.SignedAngle(entryPoint.forward, -targetExit.forward, Vector3.up);
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
}