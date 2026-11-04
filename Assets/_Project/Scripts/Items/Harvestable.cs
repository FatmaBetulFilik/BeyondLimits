using System.Collections;
using UnityEngine;

// Ağaç, taş, çalı gibi "vurdukça kaynak veren" objeler.
// Required Tool doluysa (örn. "Balta") o eşya envanterde yoksa kesilemez.
public class Harvestable : MonoBehaviour, IInteractable
{
    [SerializeField] string actionName = "Kes";
    [SerializeField] string resourceName = "Odun";
    [SerializeField] int hitsToBreak = 3;
    [SerializeField] int amountPerHit = 1;
    [SerializeField] string requiredTool = "";   // Boş = alet gerekmez
    [Tooltip("0 = tükenince yok olur. 0'dan büyükse bu kadar saniye sonra geri gelir.")]
    [SerializeField] float respawnTime = 0f;

    int hitsLeft;
    Vector3 originalPos;

    public string Prompt => string.IsNullOrEmpty(requiredTool)
        ? $"{actionName} ({hitsLeft})"
        : $"{actionName} ({hitsLeft}) - {requiredTool} gerekli";

    public bool CanInteract => hitsLeft > 0;

    void Awake()
    {
        hitsLeft = hitsToBreak;
        originalPos = transform.localPosition;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        Inventory inv = interactor.GetComponent<Inventory>();

        if (!string.IsNullOrEmpty(requiredTool) && (inv == null || inv.Count(requiredTool) == 0))
        {
            if (inv != null) inv.Message($"Önce {requiredTool} bulmalısın!");
            return;
        }

        hitsLeft--;
        if (inv != null) inv.Add(resourceName, amountPerHit);

        StartCoroutine(Shake());

        if (hitsLeft <= 0)
        {
            if (respawnTime > 0) StartCoroutine(Respawn());
            else Destroy(gameObject, 0.25f);
        }
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

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(0.25f);
        SetVisible(false);
        yield return new WaitForSeconds(respawnTime);
        hitsLeft = hitsToBreak;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = visible;
    }
}