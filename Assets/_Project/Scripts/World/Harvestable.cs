using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Ağaç, taş, çalı gibi "vurdukça kaynak veren" objeler.
// Required Tool doluysa (örn. "Balta") o eşya envanterde yoksa kesilemez.
// AĞ: Kalan vuruş sayısı NetworkVariable'dır; biri kesince herkeste aynı anda azalır / yok olur.
// Sahnedeki nesnede NetworkObject bileşeni olmalı.
public class Harvestable : NetworkBehaviour, IInteractable
{
    [SerializeField] string actionName = "Kes";
    [SerializeField] string resourceName = "Odun";
    [SerializeField] int hitsToBreak = 3;
    [SerializeField] int amountPerHit = 1;
    [SerializeField] string requiredTool = "";   // Boş = alet gerekmez
    [Tooltip("0 = tükenince yok olur. 0'dan büyükse bu kadar saniye sonra geri gelir.")]
    [SerializeField] float respawnTime = 0f;

    readonly NetworkVariable<int> hitsLeft = new NetworkVariable<int>(0);
    Vector3 originalPos;

    public string Prompt => string.IsNullOrEmpty(requiredTool)
        ? $"{actionName} ({hitsLeft.Value})"
        : $"{actionName} ({hitsLeft.Value}) - {requiredTool} gerekli";

    public bool CanInteract => IsSpawned && hitsLeft.Value > 0;

    void Awake()
    {
        originalPos = transform.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) hitsLeft.Value = hitsToBreak;
        hitsLeft.OnValueChanged += OnHitsChanged;
        InteractableUtil.SetVisible(gameObject, hitsLeft.Value > 0);   // sonradan katılan oyuncu için
    }

    public override void OnNetworkDespawn()
    {
        hitsLeft.OnValueChanged -= OnHitsChanged;
    }

    // Sadece server çağırır (PlayerInteractor)
    public void Interact(GameObject interactor)
    {
        if (!IsServer || !CanInteract) return;

        Inventory inv = interactor.GetComponent<Inventory>();

        if (!string.IsNullOrEmpty(requiredTool) && (inv == null || inv.Count(requiredTool) == 0))
        {
            if (inv != null) inv.Message($"Önce {requiredTool} bulmalısın!");
            return;
        }

        hitsLeft.Value--;
        if (inv != null) inv.Add(resourceName, amountPerHit);

        ShakeRpc();

        if (hitsLeft.Value <= 0 && respawnTime > 0)
            StartCoroutine(RespawnOnServer());
    }

    // Her bilgisayarda değer değişince: tükendiyse kısa süre sonra gizle, geri geldiyse göster
    void OnHitsChanged(int previous, int current)
    {
        if (current > 0) InteractableUtil.SetVisible(gameObject, true);
        else StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(0.25f);
        if (hitsLeft.Value <= 0) InteractableUtil.SetVisible(gameObject, false);
    }

    IEnumerator RespawnOnServer()
    {
        yield return new WaitForSeconds(respawnTime);
        hitsLeft.Value = hitsToBreak;
    }

    [Rpc(SendTo.Everyone)]
    void ShakeRpc()
    {
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * 0.05f;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}
