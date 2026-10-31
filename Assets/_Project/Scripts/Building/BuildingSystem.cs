using UnityEngine;
using UnityEngine.InputSystem;

// PlayerArmature objesine ekle (PlayerInteractor ve Inventory ile ayni objeye).
// B: insa modunu ac/kapat | Sol tik: yerlestir | R: 90 derece cevir
// Not: Bu surum tek oyunculu demo icindir. Ag (multiplayer) surumunde Place()
// istegi server'a gonderilip blok orada Spawn edilecek.
public class BuildingSystem : MonoBehaviour
{
    [Header("Insa Ayarlari")]
    public GameObject piecePrefab;
    public float maxBuildDistance = 30f;   // Kamera karakterin arkasinda, bu yuzden genis tutuldu
    public LayerMask groundLayer = ~0;     // Varsayilan: tum katmanlar
    public float rotateStep = 90f;

    [Header("Maliyet (Inventory varsa)")]
    public string costItem = "Odun";
    public int cost = 1;                   // 0 yaparsan bedava

    Camera cam;
    Inventory inventory;
    GameObject preview;
    float bottomOffset;                    // Pivot'tan blogun tabanina uzaklik
    bool isBuilding;
    bool hasValidSpot;
    Vector3 spot;
    float yaw;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
            SetBuilding(!isBuilding);

        if (!isBuilding) return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
            yaw = (yaw + rotateStep) % 360f;

        UpdateSpot();
        UpdatePreview();

        if (hasValidSpot && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Place();
    }

    void OnDisable()
    {
        SetBuilding(false);
    }

    void SetBuilding(bool on)
    {
        isBuilding = on;

        if (on)
        {
            if (preview == null) CreatePreview();
        }
        else if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }
    }

    void CreatePreview()
    {
        if (piecePrefab == null)
        {
            Debug.LogError("BuildingSystem: Piece Prefab atanmamis.");
            isBuilding = false;
            return;
        }

        preview = Instantiate(piecePrefab);
        preview.name = "BuildPreview";

        // Hayalet isinlari engellemesin, fizige girmesin
        foreach (Collider c in preview.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (Rigidbody rb in preview.GetComponentsInChildren<Rigidbody>()) Destroy(rb);

        // Yesil renk (URP Lit icin material.color _BaseColor'a gider)
        foreach (Renderer r in preview.GetComponentsInChildren<Renderer>())
            r.material.color = new Color(0f, 1f, 0f, 0.5f);

        Bounds b = GetBounds(preview);
        bottomOffset = preview.transform.position.y - b.min.y;
    }

    void UpdateSpot()
    {
        hasValidSpot = false;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Ekranin ortasindan isin at (imlec kilitli oldugu icin fare konumu kullanilmaz)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildDistance, groundLayer, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;                              // kendi karakterimiz
            if (preview != null && h.transform.IsChildOf(preview.transform)) continue;   // hayalet
            if (h.distance < best)
            {
                best = h.distance;
                spot = h.point;
                hasValidSpot = true;
            }
        }
    }

    void UpdatePreview()
    {
        if (preview == null) return;

        preview.SetActive(hasValidSpot);
        if (!hasValidSpot) return;

        preview.transform.SetPositionAndRotation(
            spot + Vector3.up * bottomOffset,
            Quaternion.Euler(0f, yaw, 0f));
    }

    void Place()
    {
        if (inventory != null && cost > 0 && !inventory.Remove(costItem, cost))
        {
            inventory.Message($"{cost} {costItem} gerekli");
            return;
        }

        GameObject placed = Instantiate(
            piecePrefab,
            spot + Vector3.up * bottomOffset,
            Quaternion.Euler(0f, yaw, 0f));
        placed.name = "Placed_" + piecePrefab.name;
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    void OnGUI()
    {
        if (!isBuilding) return;

        // Nisangah
        GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);

        GUI.Box(new Rect(Screen.width / 2f - 230, Screen.height - 70, 460, 36),
            $"INSA MODU  |  Sol tik: koy ({cost} {costItem})  |  R: cevir  |  B: cik");
    }
}