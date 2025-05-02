using UnityEngine;

public class DungeonGenerator : MonoBehaviour

{
    public enum TileType
    {
        Wall,
        Floor
    }

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

    void Start()
    {
        GenerateDungeon();
        BuildDungeon();
        SpawnCharacters();
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

            float dist = Vector3.Distance(playerPos, monsterPos);
            if(dist >= minSpawnDistance)
            {
                validSpawn = true;
                GameObject player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
                Instantiate(monsterPrefab, monsterPos, Quaternion.identity);

                CameraFollow camFollow = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<CameraFollow>();
                if (camFollow != null)
                {
                    camFollow.player = player.transform;
                }
            }
        }
    }
}
