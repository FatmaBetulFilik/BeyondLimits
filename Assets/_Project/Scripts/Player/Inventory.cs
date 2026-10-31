using System.Collections.Generic;
using UnityEngine;

// PlayerArmature'a ekli. Kaynakları tutar, sol üstte gösterir, ekrana kısa mesajlar yazar.
public class Inventory : MonoBehaviour
{
    readonly Dictionary<string, int> items = new Dictionary<string, int>();

    string message;
    float messageUntil;

    public void Add(string item, int amount = 1)
    {
        items.TryGetValue(item, out int count);
        items[item] = count + amount;
        Message($"+{amount} {item}");
    }

    public int Count(string item) => items.TryGetValue(item, out int c) ? c : 0;

    public bool Remove(string item, int amount = 1)
    {
        if (Count(item) < amount) return false;
        items[item] -= amount;
        return true;
    }

    public void Message(string text, float duration = 2f)
    {
        message = text;
        messageUntil = Time.time + duration;
    }

    void OnGUI()
    {
        float y = 10;
        foreach (var kv in items)
        {
            GUI.Label(new Rect(10, y, 250, 24), $"{kv.Key}: {kv.Value}");
            y += 22;
        }

        if (Time.time < messageUntil)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, 40), message, style);
        }
    }
}