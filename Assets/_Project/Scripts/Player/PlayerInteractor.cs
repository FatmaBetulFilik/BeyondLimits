using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyuncu prefab'ına ekle. Sahibinin bilgisayarında en yakın etkileşimli objeyi bulur, "[E] ..." yazar.
// E'ye basınca isteği SERVER'a gönderir; server mesafeyi doğrular ve Interact()'ı server'da çalıştırır.
public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] float interactRange = 1.5f;   // Algılama yarıçapı
    [SerializeField] float forwardOffset = 1f;     // Oyuncunun ne kadar önüne bakılacak
    [SerializeField] LayerMask interactMask = ~0;  // Varsayılan: tüm layer'lar
    [SerializeField] float serverExtraRange = 3f;  // Server'ın doğrulamada tanıdığı ek mesafe (gecikme payı)

    IInteractable current;
    NetworkObject currentObject;

    Vector3 Center => transform.position + Vector3.up + transform.forward * forwardOffset;

    void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        current = FindNearest(out currentObject);

        if (current != null && currentObject != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            InteractRpc(new NetworkObjectReference(currentObject));
    }

    // Sahibinden server'a: "şu nesneyle etkileşmek istiyorum"
    [Rpc(SendTo.Server, RequireOwnership = true)]
    void InteractRpc(NetworkObjectReference target)
    {
        if (!target.TryGet(out NetworkObject no)) return;

        IInteractable it = no.GetComponentInChildren<IInteractable>();
        if (it == null || !it.CanInteract) return;                                            // doğrula: hâlâ açık mı?
        if (DistanceTo(no) > interactRange + forwardOffset + serverExtraRange) return;        // doğrula: yakın mı?

        it.Interact(gameObject);                                                              // uygula
    }

    float DistanceTo(NetworkObject no)
    {
        float best = Vector3.Distance(transform.position, no.transform.position);
        foreach (Collider c in no.GetComponentsInChildren<Collider>())
            best = Mathf.Min(best, Vector3.Distance(transform.position, c.bounds.ClosestPoint(transform.position)));
        return best;
    }

    IInteractable FindNearest(out NetworkObject nearestObject)
    {
        nearestObject = null;
        Vector3 center = Center;
        Collider[] hits = Physics.OverlapSphere(center, interactRange, interactMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable it = hit.GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract) continue;

            // Ağda çalışması için nesnenin NetworkObject'i olmalı
            NetworkObject no = hit.GetComponentInParent<NetworkObject>();
            if (no == null || !no.IsSpawned) continue;

            float d = (hit.bounds.ClosestPoint(center) - center).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = it;
                nearestObject = no;
            }
        }
        return best;
    }

    // Basit ekran yazısı (Canvas kurmadan çalışır; sonra UI ile değiştirebilirsiniz)
    void OnGUI()
    {
        if (!IsSpawned || !IsOwner || current == null) return;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width / 2f - 160, Screen.height * 0.75f, 320, 44), $"[E] {current.Prompt}", style);
    }

    // Sahnede algılama alanını sarı küre olarak gösterir
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Center, interactRange);
    }
}
