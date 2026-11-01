using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyuncu prefab'ına ekli. Can, açlık, susuzluk SERVER'da hesaplanır, NetworkVariable ile herkese senkronlanır.
// Damage / Heal / Eat / Drink sadece server'da çalışır.
public class PlayerStats : NetworkBehaviour
{
    // Sahnedeki tüm oyuncuların stats'ı (Campfire gibi yakındaki herkesi iyileştirenler için).
    public static readonly List<PlayerStats> All = new List<PlayerStats>();

    public float maxValue = 100f;

    [SerializeField] float hungerPerSecond = 0.5f;   // Açlık azalma hızı
    [SerializeField] float thirstPerSecond = 0.8f;   // Susuzluk azalma hızı
    [SerializeField] float starveDamagePerSecond = 2f;

    readonly NetworkVariable<float> healthNet = new NetworkVariable<float>(100f);
    readonly NetworkVariable<float> hungerNet = new NetworkVariable<float>(100f);
    readonly NetworkVariable<float> thirstNet = new NetworkVariable<float>(100f);

    public float health => healthNet.Value;
    public float hunger => hungerNet.Value;
    public float thirst => thirstNet.Value;
    public bool IsDead => health <= 0;

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        if (IsServer) ResetAll();
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
    }

    void Update()
    {
        if (!IsSpawned) return;

        // Ölünce sahibi R'ye basar, server yeniden başlatır
        if (IsOwner && IsDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RespawnRpc();

        if (!IsServer || IsDead) return;

        hungerNet.Value = Mathf.Max(0, hunger - hungerPerSecond * Time.deltaTime);
        thirstNet.Value = Mathf.Max(0, thirst - thirstPerSecond * Time.deltaTime);

        if (hunger <= 0 || thirst <= 0)
            Damage(starveDamagePerSecond * Time.deltaTime);
    }

    // ---- Sadece server ----
    public void Damage(float amount) { if (IsServer) healthNet.Value = Mathf.Max(0, health - amount); }
    public void Heal(float amount)   { if (IsServer) healthNet.Value = Mathf.Min(maxValue, health + amount); }
    public void Eat(float amount)    { if (IsServer) hungerNet.Value = Mathf.Min(maxValue, hunger + amount); }
    public void Drink(float amount)  { if (IsServer) thirstNet.Value = Mathf.Min(maxValue, thirst + amount); }

    void ResetAll()
    {
        healthNet.Value = hungerNet.Value = thirstNet.Value = maxValue;
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    void RespawnRpc()
    {
        if (IsDead) ResetAll();
    }

    void OnGUI()
    {
        if (!IsSpawned || !IsOwner) return;

        DrawBar(0, "Can", health, Color.red);
        DrawBar(1, "Açlık", hunger, new Color(1f, 0.6f, 0f));
        DrawBar(2, "Susuzluk", thirst, new Color(0.2f, 0.6f, 1f));

        if (IsDead)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120), "ÖLDÜN\n<size=24>R ile yeniden başla</size>", style);
        }
    }

    void DrawBar(int row, string label, float value, Color color)
    {
        float w = 220, h = 20;
        float x = Screen.width - w - 20;
        float y = 20 + row * (h + 8);

        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, w * (value / maxValue), h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 6, y + 1, w, h), $"{label}: {Mathf.CeilToInt(value)}");
    }
}
