using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour

{
    //public enum TileType
    //{
    //    Wall,
    //    Floor
    //}

    public int width = 20;
    public int height = 20;
    public int walkSteps = 100;
    public int numWalkers = 5;
    public GameObject floorPreFab;
    public GameObject wallPefab;
    public GameObject playerPrefab;
    public GameObject monsterPrefab;
    public float tileSize = 1.0f;
    public float minSpawnDistance = 5f;

    private TileType[,] grid;

    private bool dungeonAccepted = false;

    void Start()
    {
        int tries = 0;

        while(!dungeonAccepted)
        {
            tries++;
            UnityEngine.Debug.Log("try: " + tries);
            GenerateDungeon();
            BuildDungeon();
            SpawnCharacters();

            if(tries > 100)
            {
                UnityEngine.Debug.LogWarning("Unlösbarer Dungeon! Bitte Konfiguration prüfen.");
            }
        }
        
    }

    void GenerateDungeon()
    {
        grid = new TileType[width, height];

        for(int x = 0; x < width; x++)
        {
            for( int y = 0; y < height; y++)
            {
                grid[x, y] = TileType.Wall;
            }
        }

        for(int i = 0; i < numWalkers; i++)
        {
            int walkerX = Random.Range(1, width - 1);
            int walkerY = Random.Range(1, height - 1);

            for(int j = 0; j < walkSteps; j++)
            {
                grid[walkerX, walkerY] = TileType.Floor;

                int dir = Random.Range(0, 4);
                switch(dir)
                {
                    case 0: walkerX++; break;
                    case 1: walkerX--; break;
                    case 2: walkerY++; break;
                    case 3: walkerY--; break;
                }

                walkerX = Mathf.Clamp(walkerX, 1, width - 2);
                walkerY = Mathf.Clamp(walkerY, 1, height - 2);
            }
        }
    }

    void BuildDungeon()
    {
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                if (grid[x, y] == TileType.Floor)
                {
                    Instantiate(floorPreFab, pos, Quaternion.identity, transform);
                }
                else
                {
                    Instantiate(wallPefab, pos, Quaternion.identity, transform);
                }
            }
        }
    }

    void SpawnCharacters()
    {
        Vector3 playerPos = Vector3.zero;
        Vector3 monsterPos = Vector3.zero;
        bool validSpawn = false;

        while(!validSpawn)
        {
            int px = Random.Range(1, width - 1);
            int py = Random.Range(1, height - 1);
            int mx = Random.Range(1, width - 1);
            int my = Random.Range(1, height - 1);

            if (grid[px, py] != TileType.Floor || grid[mx, my] != TileType.Floor)
                continue;

            playerPos = new Vector3(px * tileSize, 0.5f, py * tileSize);
            monsterPos = new Vector3(mx * tileSize, 0.5f, my * tileSize);

            Vector2Int monsterGridPos = new Vector2Int(
                Mathf.FloorToInt(monsterPos.x / tileSize),
                Mathf.FloorToInt(monsterPos.z / tileSize)
            );

            Vector2Int playerGridPos = new Vector2Int(
                Mathf.FloorToInt(playerPos.x / tileSize),
                Mathf.FloorToInt(playerPos.z / tileSize)
            );

            if (IsReachable(monsterGridPos, playerGridPos))
                dungeonAccepted = true;

            UnityEngine.Debug.Log("PLAYERPOS: " + playerPos + " MONSTERPOS: " + monsterPos);

            float dist = Vector3.Distance(playerPos, monsterPos);
            if(dist >= minSpawnDistance)
            {
                validSpawn = true;
                GameObject player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
                Instantiate(monsterPrefab, monsterPos, Quaternion.identity);

                MonsterAgent monster = GameObject.FindGameObjectWithTag("Monster")?.GetComponent<MonsterAgent>();

                //monster.player = player.transform;

                // Koordinaten speichern
                QLearningAgent qAgent = monster.GetComponent<QLearningAgent>();
                if (qAgent != null)
                {
                    qAgent.gridX = mx;
                    qAgent.gridY = my;
                    qAgent.grid = grid; // Übergib das Dungeon-Grid
                    qAgent.player = player.transform;
                }

                CameraFollow camFollow = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<CameraFollow>();
                if (camFollow != null)
                {
                    camFollow.player = player.transform;
                }
            }
        }
    }

    private bool IsReachable(Vector2Int start, Vector2Int goal)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(start, null, 0, Heuristic(start, goal));
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Günstigster Knoten suchen
            Node current = openSet.OrderBy(n => n.fCost).First();

            if (current.position == goal)
                return true; // Pfad gefunden

            openSet.Remove(current);
            closedSet.Add(current.position);

            foreach (var neighborPos in GetNeighbors(current.position))
            {
                if (closedSet.Contains(neighborPos))
                    continue;

                if (grid[neighborPos.x, neighborPos.y] != TileType.Floor)
                    continue;

                int tentativeG = current.gCost + 1;

                Node existing = openSet.FirstOrDefault(n => n.position == neighborPos);
                if (existing == null)
                {
                    openSet.Add(new Node(
                        neighborPos,
                        current,
                        tentativeG,
                        Heuristic(neighborPos, goal)
                    ));
                }
                else if (tentativeG < existing.gCost)
                {
                    existing.gCost = tentativeG;
                    existing.parent = current;
                }
            }
        }

        return false; // Kein Pfad
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new();

        Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
    };

        foreach (var dir in directions)
        {
            Vector2Int check = pos + dir;
            if (check.x >= 0 && check.x < width && check.y >= 0 && check.y < height)
            {
                neighbors.Add(check);
            }
        }

        return neighbors;
    }

    private class Node
    {
        public Vector2Int position;
        public Node parent;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;

        public Node(Vector2Int pos, Node parent, int g, int h)
        {
            this.position = pos;
            this.parent = parent;
            this.gCost = g;
            this.hCost = h;
        }
    }

}



