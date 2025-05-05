using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;

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
    public float learningRate = 0.1f;
    public float discountFactor = 0.9f;
    public float explorationRate = 0.5f; // Anfang hoch → später sinkt
    public float minExplorationRate = 0.05f;
    public float explorationDecay = 0.995f;
    float maxPenalty = -100f;

    [Header("Entscheidungstiming")]
    public float decisionInterval = 0.2f;
    private float decisionTimer = 0f;

    public Dictionary<StateActionPair, float> qTable = new();
    private MonsterAgent movement;

    public Vector2Int[] actions = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 1),    // ↗
        new Vector2Int(1, -1),   // ↘
        new Vector2Int(-1, 1),   // ↖
        new Vector2Int(-1, -1)   // ↙
    };

    private bool waitingForMovement = false;
    private Vector2Int lastState;
    private Vector2Int lastAction;

    void Start()
    {
        movement = GetComponent<MonsterAgent>();
    }

    void Update()
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            Step();
            decisionTimer = decisionInterval;
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
            // Bonus: Feedback trotzdem geben
            float reward = maxPenalty;
            float oldQ = GetQ(currentState, action);
            float maxFutureQ = GetMaxQ(currentState);
            float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
            qTable[new StateActionPair(currentState, action)] = newQ;

            // KEIN Movement – wir machen sofort weiter
            UnityEngine.Debug.Log("Not walkable");
            return;
        }


        //if (!IsWalkable(newState))
        //    return;

        lastState = currentState;
        lastAction = action;
        gridX = newState.x;
        gridY = newState.y;

        // Bewegung starten
        waitingForMovement = true;
        movement.onMoveComplete = () =>
        {
            float reward = GetReward(newState);

            // ➕ 1. Extra-Strafe, wenn sich das Monster vom Spieler entfernt hat
            Vector2Int playerGrid = new Vector2Int(
                Mathf.FloorToInt(player.position.x / tileSize),
                Mathf.FloorToInt(player.position.z / tileSize)
            );

            int prevDist = Mathf.Abs(playerGrid.x - lastState.x) + Mathf.Abs(playerGrid.y - lastState.y);
            int newDist = Mathf.Abs(playerGrid.x - gridX) + Mathf.Abs(playerGrid.y - gridY);

            if (newDist > prevDist)
            {
                reward -= 2f; // Kleine Strafe für sich Entfernen
            }

            float oldQ = GetQ(lastState, lastAction);
            float maxFutureQ = GetMaxQ(newState);

            float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
            qTable[new StateActionPair(lastState, lastAction)] = newQ;

            foreach (var key in qTable.Keys.ToList())
            {
                qTable[key] *= 0.999f;
            }

            // Exploration anpassen
            explorationRate *= explorationDecay;
            explorationRate = Mathf.Max(explorationRate, minExplorationRate);

            waitingForMovement = false;
            UnityEngine.Debug.Log($"@{currentState} → {action} | Reward: {reward:F2} | Q: {newQ:F2} | ε: {explorationRate:F2} | ({gridX},{gridY})");
        };

        movement.MoveTo(action);
        
    }

    //void Step()
    //{
    //    Vector2Int currentState = new Vector2Int(gridX, gridY);
    //    Vector2Int action = SelectAction(currentState);
    //    Vector2Int newState = currentState + action;

    //    if (!IsWalkable(newState))
    //        return;

    //    // Belohnung holen
    //    float reward = GetReward(newState);

    //    // Q-Wert-Update
    //    float oldQ = GetQ(currentState, action);
    //    float maxFutureQ = GetMaxQ(newState);
    //    float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
    //    qTable[new StateActionPair(currentState, action)] = newQ;

    //    // Bewegung ausführen
    //    movement.MoveTo(action);
    //    gridX = newState.x;
    //    gridY = newState.y;

    //    // Exploration senken
    //    explorationRate *= explorationDecay;
    //    explorationRate = Mathf.Max(explorationRate, minExplorationRate);

    //    // Debug-Ausgabe (optional)
    //    UnityEngine.Debug.Log($"@{currentState} → {action} | Reward: {reward:F2} | Q: {newQ:F2} | ε: {explorationRate:F2} | ({gridX},{gridY})");
    //}

    Vector2Int SelectAction(Vector2Int state)
    {
        if (UnityEngine.Random.value < explorationRate)
        {
            return actions[UnityEngine.Random.Range(0, actions.Length)];
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

        return bestAction;
    }

    float GetReward(Vector2Int newState)
    {
        // Spieler-Grid ermitteln
        Vector2Int playerGrid = new Vector2Int(
            Mathf.FloorToInt(player.position.x / tileSize),
            Mathf.FloorToInt(player.position.z / tileSize)
        );

        int distance = Mathf.Abs(playerGrid.x - newState.x) + Mathf.Abs(playerGrid.y - newState.y);

        if (distance <= 1)
            return 100f; // Ziel erreicht

        // Je weiter weg → desto negativer
        float reward = -distance;

        //return Mathf.Max(reward, maxPenalty);
        return reward;
    }



    //float GetReward(Vector2Int newState)
    //{
    //    int playerGridX = Mathf.RoundToInt(player.position.x / tileSize);
    //    int playerGridY = Mathf.RoundToInt(player.position.z / tileSize);
    //    Vector2Int playerPos = new(playerGridX, playerGridY);

    //    float distance = Vector2Int.Distance(newState, playerPos);

    //    // Nähebasiert:
    //    if (distance < 0.5f)
    //        return 100f; // Treffer

    //    // Je näher, desto besser (linear abgeschwächt)
    //    float maxPenalty = -10f;
    //    float maxDistance = 10f;
    //    float scaled = Mathf.Lerp(0f, maxPenalty, distance / maxDistance);
    //    return Mathf.Max(scaled, maxPenalty); // nie schlimmer als -10
    //}

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

    // Struct bleibt wie gehabt
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
