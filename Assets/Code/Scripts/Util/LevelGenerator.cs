using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class RoomNode
{
    public GameObject roomPrefab;
    public List<RoomNode> connectedRooms;
    public bool isEndRoom = false;
}


public class LevelGenerator : MonoBehaviour
{

    [SerializeField] private GameObject startRoomPrefab;
    [SerializeField] private List<RoomNode> starterConnections;
    [SerializeField] private GameObject wallPrefab;


    [Header("Valuables")]
    [SerializeField] private List<GameObject> itemPrefabs;
    [SerializeField, Range(0f, 1f)] private float itemSpawnChance = 0.5f;


    private List<Bounds> placedRoomBounds  = new List<Bounds>();


    private void Start()
    {
        
        if (starterRoom == null)
        {
            Debug.LogWarning("No starter room assigned");
            return;
        }

        placedRoomBounds.Add(GetRoomBounds(starterRoom));
        SpawnValuables(starterRoom);

        Transform spawnLocs = starterRoom.transform.Find("SpawnLocs");

        for (int i = 0; i < spawnLocs.childCount; i++)
        {
            
            if (i >= spawnLocs.childCount) break;

            Transform exit = spawnLocs.GetChild(i);

            PlaceRoom(starterConnections[i], exit);
        }

    }


    private void PlaceRoom(RoomNode roomNode, Transform incomingExit)
    {
        
        if (roomNode == null || roomNode.roomPrefab == null)
        {
            if (wallCapPrefab != null)
                Instantiate(wallCapPrefab, incomingExit.position, incomingExit.rotation);
            return;
        }


        GameObject newRoom = Instantiate(roomNode.roomPrefab);

        Transform entryPoint = newRoom.transform.Find("SpawnAt");

        if (entryPoint == null)
        {
            Debug.LogWarning($"Room {newRoom.name} does not have a 'SpawnAt' transform.");
            Destroy(newRoom);
            return;
        }

        AlignRoomToPoint(newRoom, entryPoint, incomingExit);

        Bounds newBounds = GetRoomBounds(newRoom);

        foreach (Bounds existingBounds in placedRoomBounds)
        {
            if (newBounds.Intersects(existingBounds))
            {
                Debug.LogWarning($"Room {newRoom.name} overlaps with an existing room. Destroying it.");
                Destroy(newRoom);
                return;
            }
        }
        placedRoomBounds.Add(newBounds);

        SpawnValuables(newRoom);


        if (roomNode.isEndRoom)
        {
            return;
        }

        Transform spawnLocs = newRoom.transform.Find("SpawnLocs");

        if (spawnLocs == null || spawnLocs.childCount == 0)
        {
            Debug.LogWarning($"Room {newRoom.name} does not have any 'SpawnLocs' for further connections.");
            return;
        }

        for (int i = 0; i< spawnLocs.childCount; i++)
        {
            if (i >= roomNode.connectedRooms.Count) break;

            Transform exit = spawnLocs.GetChild(i);
            RoomNode child = (roomNode.connectedRooms != null && i < roomNode.connectedRooms.Count) ? roomNode.connectedRooms[i] : null;
            PlaceRoom(child, exit);
        }
    }

    private void SpawnValuables(GameObject room)
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0) return;

        Transform valuables = room.transform.Find("Valuables");

        if (valuables == null) return;

        foreach (Transform point in valuables)
        {
            if (Random.value <= itemSpawnChance)
            {
                GameObject itemPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Count)];
                Instantiate(itemPrefab, point.position, point.rotation);
            }
        }
        
    }

    private void AlignRoomToPoint(GameObject room, Transform roomEntry, Transform targetExit)
    {
        float angleDiff = Vector3.SignedAngle(roomEntry.forward, -targetExit.forward, Vector3.up);
        room.transform.Rotate(Vector3.up, angleDiff);

        Vector3 offset = targetExit.position - roomEntry.transform.position;
        room.transform.position += offset;

    }

    private Bounds GetRoomBounds(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"Room {room.name} has no renderers to calculate bounds.");
            return new Bounds(room.transform.position, Vector3.zero);
        }

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
        {
            combinedBounds.Encapsulate(rend.bounds);
        }
        return combinedBounds;

    }




    }



























