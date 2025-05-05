using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QValueVisualizer : MonoBehaviour
{
    public GameObject textPrefab;
    public QLearningAgent agent;
    public float tileSize = 1f;
    public float refreshInterval = 1f;

    private Dictionary<Vector2Int, TextMeshPro> qDisplays = new();
    private float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            UpdateQTexts();
            timer = refreshInterval;
        }
    }

    void UpdateQTexts()
    {
        foreach (var entry in agent.qTable)
        {
            var state = entry.Key.state;

            // Erstellt Text einmal pro Grid-Feld
            if (!qDisplays.ContainsKey(state))
            {
                Vector3 worldPos = new Vector3(state.x * tileSize, 0.1f, state.y * tileSize);
                GameObject txtObj = Instantiate(textPrefab, worldPos, Quaternion.Euler(90, 0, 0), transform);
                txtObj.transform.localScale = Vector3.one * 0.2f;
                qDisplays[state] = txtObj.GetComponent<TextMeshPro>();
            }
        }

        // Pro Text alle Richtungen anzeigen
        foreach (var kvp in qDisplays)
        {
            Vector2Int state = kvp.Key;
            TextMeshPro txt = kvp.Value;
            txt.text = FormatQText(state);
        }
    }

    string FormatQText(Vector2Int state)
    {
        string result = "";
        foreach (var dir in agent.actions)
        {
            var key = new QLearningAgent.StateActionPair(state, dir);
            float val = agent.qTable.ContainsKey(key) ? agent.qTable[key] : 0f;

            string arrow = dir switch
            {
                var d when d == Vector2Int.up => "↑",
                var d when d == Vector2Int.down => "↓",
                var d when d == Vector2Int.left => "←",
                var d when d == Vector2Int.right => "→",
                var d when d == new Vector2Int(1, 1) => "UR",
                var d when d == new Vector2Int(-1, 1) => "UL",
                var d when d == new Vector2Int(1, -1) => "DR",
                var d when d == new Vector2Int(-1, -1) => "DL",
                _ => "·"
            };

            result += $"{arrow} {val:F1}\n";
        }

        return result.TrimEnd();
    }
}
