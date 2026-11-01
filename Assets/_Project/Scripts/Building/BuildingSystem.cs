using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Oyuncu prefab'ına ekle (PlayerInteractor ve Inventory ile aynı objeye).
// B: inşa modunu aç/kapat | Sol tık: yerleştir | R: 90 derece çevir
//
// AĞ: Hayalet (preview) sadece sahibinin ekranında görünür. Yerleştirirken sahibi isteği server'a gönderir,
// server mesafeyi ve maliyeti doğrular, bloğu oluşturup Spawn() eder; böylece herkes bloğu görür.
// Gereken: Piece Prefab'ın kökünde NetworkObject + BuildPiece olmalı ve DefaultNetworkPrefabs listesinde olmalı.
public class BuildingSystem : NetworkBehaviour
{
    [Header("Inşa Ayarları")]
    public GameObject piecePrefab;
    public float maxBuildDistance = 30f;   // Kamera karakterin arkasında, bu yüzden geniş tutuldu
    public LayerMask groundLayer = ~0;     // Varsayılan: tüm katmanlar
    public float rotateStep = 90f;

    [Header("Maliyet (Inventory varsa)")]
    public string costItem = "Odun";
    public int cost = 1;                   // 0 yaparsan bedava

    [Header("Server doğrulaması")]
    public float serverMaxPlaceDistance = 40f;   // Oyuncudan en fazla bu kadar uzağa blok konabilir

    Camera cam;
    Inventory inventory;
    GameObject preview;
    float bottomOffset;                    // Pivot'tan bloğun tabanına uzaklık
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
        if (!IsSpawned || !IsOwner) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
            SetBuilding(!isBuilding);

        if (!isBuilding) return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
            yaw = (yaw + rotateStep) % 360f;

        UpdateSpot();
        UpdatePreview();

        if (hasValidSpot && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlace();
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
            Debug.LogError("BuildingSystem: Piece Prefab atanmamış.");
            isBuilding = false;
            return;
        }

        preview = Instantiate(piecePrefab);
        preview.name = "BuildPreview";

        // Hayalet ağ nesnesi olmasın: sadece görüntü
        foreach (NetworkBehaviour nb in preview.GetComponentsInChildren<NetworkBehaviour>()) Destroy(nb);
        foreach (NetworkObject no in preview.GetComponentsInChildren<NetworkObject>()) Destroy(no);

        // Işınları engellemesin, fiziğe girmesin
        foreach (Collider c in preview.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (Rigidbody rb in preview.GetComponentsInChildren<Rigidbody>()) Destroy(rb);

        // Yeşil renk (URP Lit için material.color _BaseColor'a gider)
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

        // Ekranın ortasından ışın at (imleç kilitli olduğu için fare konumu kullanılmaz)
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

    // Sahibin tarafı: sadece hızlı bir ön kontrol, asıl karar server'da
    void TryPlace()
    {
        if (inventory != null && cost > 0 && inventory.Count(costItem) < cost)
        {
            inventory.Message($"{cost} {costItem} gerekli");
            return;
        }

        PlaceRpc(spot + Vector3.up * bottomOffset, yaw);
    }

    // Sahibinden server'a: "şuraya blok koymak istiyorum"
    [Rpc(SendTo.Server, RequireOwnership = true)]
    void PlaceRpc(Vector3 position, float yawDegrees)
    {
        if (piecePrefab == null) return;
        if (Vector3.Distance(position, transform.position) > serverMaxPlaceDistance) return;   // doğrula: yakın mı?

        if (!piecePrefab.TryGetComponent(out NetworkObject _))
        {
            Debug.LogError("BuildingSystem: Piece Prefab'ın kökünde NetworkObject yok.");
            return;
        }

        if (inventory != null && cost > 0 && !inventory.Remove(costItem, cost))                // doğrula: odun var mı?
        {
            inventory.Message($"{cost} {costItem} gerekli");
            return;
        }

        GameObject placed = Instantiate(piecePrefab, position, Quaternion.Euler(0f, yawDegrees, 0f));   // uygula
        placed.name = "Placed_" + piecePrefab.name;
        placed.GetComponent<NetworkObject>().Spawn();                                                    // herkes görsün
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
        if (!IsSpawned || !IsOwner || !isBuilding) return;

        // Nişangah
        GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);

        GUI.Box(new Rect(Screen.width / 2f - 230, Screen.height - 70, 460, 36),
            $"İNŞA MODU  |  Sol tık: koy ({cost} {costItem})  |  R: çevir  |  B: çık");
    }
}
