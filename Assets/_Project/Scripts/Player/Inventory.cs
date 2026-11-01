using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Oyuncu prefab'ına ekli. Gerçek envanter SERVER'da tutulur; oyuncunun kendisi ekranda göstermek için kopyasını alır.
// Add / Remove sadece server'da çalışır (Harvestable, Pickup, BuildingSystem... zaten server'da çağırıyor).
public class Inventory : NetworkBehaviour
{
    readonly Dictionary<string, int> items = new Dictionary<string, int>();

    string message;
    float messageUntil;

    public int Count(string item) => items.TryGetValue(item, out int c) ? c : 0;

    // ---- Sadece server ----
    public void Add(string item, int amount = 1)
    {
        if (!IsServer) return;
        items[item] = Count(item) + amount;
        SyncItemRpc(item, items[item]);
        Message($"+{amount} {item}");
    }

    public bool Remove(string item, int amount = 1)
    {
        if (!IsServer) return false;
        if (Count(item) < amount) return false;
        items[item] -= amount;
        SyncItemRpc(item, items[item]);
        return true;
    }

    // Server çağırırsa mesaj oyuncunun kendi ekranına gider; oyuncu kendisi çağırırsa direkt gösterilir.
    public void Message(string text, float duration = 2f)
    {
        if (IsServer) MessageRpc(text, duration);
        else if (IsOwner) ShowMessage(text, duration);
    }

    [Rpc(SendTo.Owner)]
    void SyncItemRpc(string item, int count)
    {
        items[item] = count;
    }

    [Rpc(SendTo.Owner)]
    void MessageRpc(string text, float duration)
    {
        ShowMessage(text, duration);
    }

    void ShowMessage(string text, float duration)
    {
        message = text;
        messageUntil = Time.time + duration;
    }

    void OnGUI()
    {
        if (!IsSpawned || !IsOwner) return;

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
