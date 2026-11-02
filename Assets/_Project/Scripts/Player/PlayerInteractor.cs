using UnityEngine;
using UnityEngine.InputSystem;

// PlayerArmature objesine ekle. Oyuncunun önündeki en yakın
// etkileşimli objeyi bulur, ekranda "[E] ..." yazar, E'ye basınca çalıştırır.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float interactRange = 1.5f;   // Algılama yarıçapı
    [SerializeField] float forwardOffset = 1f;     // Oyuncunun ne kadar önüne bakılacak
    [SerializeField] LayerMask interactMask = ~0;  // Varsayılan: tüm layer'lar

    IInteractable current;

    void Update()
    {
        current = FindNearest();

        if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            current.Interact(gameObject);
    }

    Vector3 Center => transform.position + Vector3.up + transform.forward * forwardOffset;

    IInteractable FindNearest()
    {
        Vector3 center = Center;
        Collider[] hits = Physics.OverlapSphere(center, interactRange, interactMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable it = hit.GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract) continue;

            float d = (hit.bounds.ClosestPoint(center) - center).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = it;
            }
        }
        return best;
    }

    // Basit ekran yazısı (Canvas kurmadan çalışır; sonra UI ile değiştirebilirsiniz)
    void OnGUI()
    {
        if (current == null) return;
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