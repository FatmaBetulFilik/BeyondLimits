using Unity.Netcode;
using UnityEngine;

// Bonfire modeline ekle. Odunla yakılır, alev ve ışık açılır, yakındaki oyuncuların canını doldurur.
// AĞ: lit NetworkVariable'dır (herkeste yanık/sönük görünür). Yanma süresi ve iyileştirme SERVER'da hesaplanır.
// Nesnede NetworkObject bileşeni olmalı.
public class Campfire : NetworkBehaviour, IInteractable
{
    [SerializeField] int woodCost = 3;
    [SerializeField] float burnTime = 60f;
    [SerializeField] Light fireLight;              // Point Light
    [SerializeField] GameObject fireVisual;        // Alev efekti (Particle System)
    [SerializeField] float lightIntensity = 6f;    // Işık gücü
    [SerializeField] float healRadius = 4f;
    [SerializeField] float healPerSecond = 3f;

    readonly NetworkVariable<bool> lit = new NetworkVariable<bool>(false);
    float burnLeft;   // sadece server'da kullanılır

    public string Prompt => $"Ateşi yak ({woodCost} Odun)";
    public bool CanInteract => IsSpawned && !lit.Value;

    public override void OnNetworkSpawn()
    {
        lit.OnValueChanged += OnLitChanged;
        SetFire(lit.Value);
    }

    public override void OnNetworkDespawn()
    {
        lit.OnValueChanged -= OnLitChanged;
    }

    void OnLitChanged(bool previous, bool current)
    {
        SetFire(current);
    }

    // Sadece server çağırır
    public void Interact(GameObject interactor)
    {
        if (!IsServer || lit.Value) return;

        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv == null) return;

        if (!inv.Remove("Odun", woodCost))
        {
            inv.Message($"Yeterli odun yok! {woodCost} Odun gerekli.");
            return;
        }

        burnLeft = burnTime;
        lit.Value = true;
        inv.Message("Ateş yandı!");
    }

    void Update()
    {
        if (!IsSpawned || !lit.Value) return;

        // Titreyen ışık: her bilgisayarda kendi görüntüsü
        if (fireLight != null)
            fireLight.intensity = lightIntensity * (0.7f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.6f);

        if (!IsServer) return;

        burnLeft -= Time.deltaTime;

        foreach (PlayerStats p in PlayerStats.All)
        {
            if (Vector3.Distance(p.transform.position, transform.position) < healRadius)
                p.Heal(healPerSecond * Time.deltaTime);
        }

        if (burnLeft <= 0) lit.Value = false;
    }

    void SetFire(bool on)
    {
        if (fireLight != null) fireLight.enabled = on;
        if (fireVisual != null) fireVisual.SetActive(on);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
