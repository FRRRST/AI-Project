using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

public class QLearningAgent : MonoBehaviour
{
    [Header("Grid-Position")]
    public int gridX;
    public int gridY;
    public TileType[,] grid;
    public float tileSize = 1.0f;

    [Header("Player-Ziel")]
    public Transform player;

    [Header("Q-Learning Parameter")]
    public float learningRate = 0.1f; //schnelligkeit des Lernens
    public float discountFactor = 0.9f; //wichtigkeit der Zukunft
    public float explorationRate = 0.5f; //
    public float minExplorationRate = 0.05f;
    public float explorationDecay = 0.995f;
    public float qValueDecay = 0.999f;
    float maxPenalty = -100f;

    [Header("Entscheidungstiming")]
    public float decisionInterval = 0.2f;
    private float decisionTimer = 0f;

    [Header("Q-Learning Modus")]
    public bool saveLearning = false;
    public bool loadLearning = false;

    [Header("Auto-Save")]
    public bool autoSave = false;
    public float autoSaveInterval = 120f;
    private float autoSaveTimer = 0f;

    [Header("Episoden")]
    public int episodeCounter = 0;

    public Dictionary<StateActionPair, float> qTable = new();
    private MonsterAgent movement;

    public Vector2Int[] actions = {
        new Vector2Int(0, 1),   //up
        new Vector2Int(0, -1),  //down
        new Vector2Int(1, 0),   //right
        new Vector2Int(-1, 0),  //left
        new Vector2Int(1, 1),    //ur
        new Vector2Int(1, -1),   //dr
        new Vector2Int(-1, 1),   //ul
        new Vector2Int(-1, -1)   //dl
    };

    private bool waitingForMovement = false;
    private Vector2Int lastState;
    private Vector2Int lastAction;

    void Start()
    {
        movement = GetComponent<MonsterAgent>();

        if (loadLearning) LoadQTable("qtable.txt");
    }

    void Update()
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            Step();
            decisionTimer = decisionInterval;
        }

        if (Input.GetKeyDown(KeyCode.S) && saveLearning)
        {
            SaveQTable("qtable.txt");
            UnityEngine.Debug.Log("Q-Tabelle manuell gespeichert.");
        }

        if(autoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveQTable("qtable.txt");
                UnityEngine.Debug.Log("Q-Tabelle automatisch gespeichert.");
                autoSaveTimer = 0f;
            }
        }
    }

    void Step()
    {
        if (waitingForMovement) return;

        Vector2Int currentState = new Vector2Int(gridX, gridY);
        Vector2Int action = SelectAction(currentState);
        Vector2Int newState = currentState + action;

        if (!IsWalkable(newState))
        {
            //massive Strafe wenn das Monster gegen die Wand läuft
            float reward = maxPenalty;
            float oldQ = GetQ(currentState, action);
            float maxFutureQ = GetMaxQ(currentState); //beste erwartete Zukunft
            float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
            qTable[new StateActionPair(currentState, action)] = newQ;

            //kein Movement möglich, einfach weiter machen
            UnityEngine.Debug.Log("Not walkable");
            return;
        }

        lastState = currentState;
        lastAction = action;
        gridX = newState.x;
        gridY = newState.y;

        //bewegung starten
        waitingForMovement = true;
        movement.onMoveComplete = () =>
        {
            float reward = GetReward(newState);

            Vector2Int playerGrid = new Vector2Int(
                Mathf.FloorToInt(player.position.x / tileSize),
                Mathf.FloorToInt(player.position.z / tileSize)
            );

            int prevDist = Mathf.Abs(playerGrid.x - lastState.x) + Mathf.Abs(playerGrid.y - lastState.y);
            int newDist = Mathf.Abs(playerGrid.x - gridX) + Mathf.Abs(playerGrid.y - gridY);

            if (newDist > prevDist)
            {
                reward -= 2f; //extra Strafe bei Entfernung vom Spieler
            }

            float oldQ = GetQ(lastState, lastAction);
            float maxFutureQ = GetMaxQ(newState);

            float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
            qTable[new StateActionPair(lastState, lastAction)] = newQ;

            foreach (var key in qTable.Keys.ToList())
            {
                qTable[key] *= qValueDecay;
            }

            explorationRate *= explorationDecay;
            explorationRate = Mathf.Max(explorationRate, minExplorationRate);

            waitingForMovement = false;
            UnityEngine.Debug.Log($"@{currentState} → {action} | Reward: {reward:F2} | Q: {newQ:F2} | ε: {explorationRate:F2} | ({gridX},{gridY})");
        };

        movement.MoveTo(action);
        
    }

    Vector2Int SelectAction(Vector2Int state) //greedy auswahl
    {
        if (UnityEngine.Random.value < explorationRate) //exploration rate ist epsilon
        {
            return actions[UnityEngine.Random.Range(0, actions.Length)]; //exploration aktion zum erkunden
        }

        float maxQ = float.NegativeInfinity;
        Vector2Int bestAction = actions[0];

        foreach (var action in actions)
        {
            float q = GetQ(state, action);
            if (q > maxQ)
            {
                maxQ = q;
                bestAction = action;
            }
        }

        return bestAction; //greedy wahl
    }

    float GetReward(Vector2Int newState)
    {

        Vector2Int playerGrid = new Vector2Int(
            Mathf.FloorToInt(player.position.x / tileSize),
            Mathf.FloorToInt(player.position.z / tileSize)
        );

        int dx = Mathf.Abs(playerGrid.x - gridX);
        int dy = Mathf.Abs(playerGrid.y - gridY);
        int distance = Mathf.Max(dx, dy);

        if (distance <= 1)
        {
            RespawnCharacters();
            episodeCounter++;
            return 100f; //großer Reward, wenn Spieler gefunden wird
        }

        float reward = -distance;

        return reward;
    }

    float GetQ(Vector2Int state, Vector2Int action)
    {
        StateActionPair key = new(state, action);
        return qTable.TryGetValue(key, out float value) ? value : 0f;
    }

    float GetMaxQ(Vector2Int state)
    {
        float maxQ = float.NegativeInfinity;
        foreach (var action in actions)
        {
            float q = GetQ(state, action);
            if (q > maxQ) maxQ = q;
        }
        return maxQ;
    }

    bool IsWalkable(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= grid.GetLength(0) || pos.y >= grid.GetLength(1))
            return false;

        return grid[pos.x, pos.y] == TileType.Floor;
    }

    public void SaveQTable(string filename)
    {
        List<string> lines = new();

        foreach (var entry in qTable)
        {
            var s = entry.Key.state;
            var a = entry.Key.action;
            lines.Add($"{s.x},{s.y},{a.x},{a.y},{entry.Value}");
        }
        lines.Add($"EXPLORATION,{explorationRate}");

        File.WriteAllLines(Path.Combine(UnityEngine.Application.persistentDataPath, filename), lines);
        UnityEngine.Debug.Log("Q-Table saved to " + filename);
    }

    public void LoadQTable(string filename)
    {
        qTable.Clear();
        string[] lines = File.ReadAllLines(Path.Combine(UnityEngine.Application.persistentDataPath, filename));

        foreach (string line in lines)
        {
            if (line.StartsWith("EXPLORATION"))
            {
                string[] tokens = line.Split(',');
                explorationRate = float.Parse(tokens[1]);
                continue;
            }

            string[] parts = line.Split(',');
            Vector2Int state = new(int.Parse(parts[0]), int.Parse(parts[1]));
            Vector2Int action = new(int.Parse(parts[2]), int.Parse(parts[3]));
            float qValue = float.Parse(parts[4]);

            qTable[new StateActionPair(state, action)] = qValue;
        }

        UnityEngine.Debug.Log("Q-Table loaded from " + filename);
    }

    public void RespawnCharacters()
    {
        Vector2Int playerPos, monsterPos;
        int tries = 0;

        do
        {
            int px = UnityEngine.Random.Range(1, grid.GetLength(0) - 1);
            int py = UnityEngine.Random.Range(1, grid.GetLength(1) - 1);
            int mx = UnityEngine.Random.Range(1, grid.GetLength(0) - 1);
            int my = UnityEngine.Random.Range(1, grid.GetLength(1) - 1);

            playerPos = new Vector2Int(px, py);
            monsterPos = new Vector2Int(mx, my);
            tries++;

        } while ((grid[playerPos.x, playerPos.y] != TileType.Floor
                 || grid[monsterPos.x, monsterPos.y] != TileType.Floor
                 || Vector2Int.Distance(playerPos, monsterPos) < 5f) && tries < 100);

        player.position = new Vector3(playerPos.x * tileSize, 0.5f, playerPos.y * tileSize);
        transform.position = new Vector3(monsterPos.x * tileSize, 0.5f, monsterPos.y * tileSize);

        gridX = monsterPos.x;
        gridY = monsterPos.y;
    }

    public struct StateActionPair
    {
        public Vector2Int state;
        public Vector2Int action;

        public StateActionPair(Vector2Int s, Vector2Int a)
        {
            state = s;
            action = a;
        }

        public override bool Equals(object obj)
        {
            return obj is StateActionPair pair &&
                   state.Equals(pair.state) &&
                   action.Equals(pair.action);
        }

        public override int GetHashCode()
        {
            return state.GetHashCode() ^ action.GetHashCode();
        }
    }
}
