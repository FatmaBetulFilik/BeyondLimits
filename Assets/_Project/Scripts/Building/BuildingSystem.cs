using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// PlayerArmature objesine ekle (PlayerInteractor ve Inventory ile aynı objeye).
// B: inşa modunu aç/kapat | Sol tık: yerleştir | R: 90 derece çevir
//
// Izgara: hücre boyutu = blockSize. Bloklar yan yana ve üst üste tam hücre aralığında durur.
// Bir bloğa bakıp tıklarsan, baktığın yüzün yanına yeni blok gelir (üst yüz = üstüne, yan yüz = yanına).
// Zemine bakarsan blok zemine oturur; yakınında blok varsa onun yüksekliğine hizalanır.
// Dolu hücreye ya da karakterin içine blok konamaz; hayalet kırmızı olur.
//
// Hedefleme: İnşa modunda imleç serbest kalır, blok FARE İMLECİNİN gösterdiği yere gelir.
// Kamerayı döndürmek için SAĞ TUŞU basılı tut (Starter Assets kamerası o sırada serbest kalır).
//
// Not: Tek oyunculu sürümdür. Ağ sürümünde Place() isteği server'a gidecek ve server aynı kontrolü yapacak.
public class BuildingSystem : MonoBehaviour
{
    [Header("İnşa Ayarları")]
    public GameObject piecePrefab;
    [Tooltip("Bir bloğun kenar uzunluğu (metre). Starter Assets karakteri 1.8 m boyunda, 2 m duvar karakter boyundan yüksek olur.")]
    public float blockSize = 2f;
    public float maxBuildDistance = 30f;   // Kamera karakterin arkasında, bu yüzden geniş tutuldu
    public LayerMask groundLayer = ~0;     // Varsayılan: tüm katmanlar
    public float rotateStep = 90f;
    [Tooltip("Açıkken blok fare imlecinin gösterdiği yere gelir (kamera sağ tuşla döner). Kapalıyken ekranın ortasındaki nişangaha göre çalışır.")]
    public bool useCursorAiming = true;

    [Header("Maliyet (Inventory varsa)")]
    public string costItem = "Odun";
    public int cost = 1;                   // 0 yaparsan bedava

    [Header("Hayalet")]
    [Tooltip("Boş bırakılabilir. Doluysa URP/Lit, Surface Type: Transparent bir materyal ver; yarı saydam görünür.")]
    public Material ghostMaterial;
    [Tooltip("Alfa (A) ne kadar düşükse o kadar saydam. Ghost Material Transparent değilse alfa işe yaramaz.")]
    public Color validColor = new Color(0f, 1f, 0f, 0.35f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.35f);

    Camera cam;
    Inventory inventory;
    CharacterController body;
    Component thirdPersonController;   // Starter Assets karakter kontrolcüsü (varsa)
    FieldInfo cameraLockField;

    GameObject preview;
    readonly List<Material> ghostMats = new List<Material>();
    float scaleFactor = 1f;                // Prefab'ı blockSize'a getiren çarpan
    Vector3 centerOffset;                  // Pivot noktasından bloğun merkezine

    bool isBuilding;
    bool hasTarget;
    bool canPlace;
    Vector3 targetCenter;
    float yaw;

    // Yerleştirilmiş bloklar: kök nesne -> merkez konumu
    readonly Dictionary<Transform, Vector3> centerOf = new Dictionary<Transform, Vector3>();

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        body = GetComponent<CharacterController>();

        // Starter Assets paketine derleme bağımlılığı koymamak için kontrolcüyü isimle buluyoruz
        foreach (Component c in GetComponents<Component>())
        {
            if (c != null && c.GetType().Name == "ThirdPersonController")
            {
                thirdPersonController = c;
                cameraLockField = c.GetType().GetField("LockCameraPosition");
                break;
            }
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
            SetBuilding(!isBuilding);

        if (!isBuilding) return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
            yaw = (yaw + rotateStep) % 360f;

        if (useCursorAiming)
        {
            // İmleç serbest; kamera sadece sağ tuş basılıyken döner
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            bool rotating = Mouse.current != null && Mouse.current.rightButton.isPressed;
            SetCameraLocked(!rotating);
        }

        UpdateTarget();
        UpdatePreview();

        if (hasTarget && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Place();
    }

    void OnDisable()
    {
        SetBuilding(false);
    }

    void SetCameraLocked(bool locked)
    {
        if (thirdPersonController != null && cameraLockField != null)
            cameraLockField.SetValue(thirdPersonController, locked);
    }

    void SetBuilding(bool on)
    {
        isBuilding = on;

        if (on)
        {
            if (preview == null) CreatePreview();
        }
        else
        {
            DestroyPreview();

            // İmleci ve kamerayı eski haline getir
            if (useCursorAiming)
            {
                SetCameraLocked(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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
        preview.transform.rotation = Quaternion.identity;

        // Bloğun gerçek boyutunu ölçeklemeden ÖNCE ölç (önce collider'a, yoksa görüntüye bakar)
        Bounds b = GetBounds(preview);
        scaleFactor = blockSize / b.size.x;
        centerOffset = (b.center - preview.transform.position) * scaleFactor;
        preview.transform.localScale *= scaleFactor;

        // Hayalet ışınları engellemesin, fiziğe girmesin
        foreach (Collider c in preview.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (Rigidbody rb in preview.GetComponentsInChildren<Rigidbody>()) Destroy(rb);

        // Hayalet materyalleri: her renderer'a kendi kopyasını ver
        foreach (Renderer r in preview.GetComponentsInChildren<Renderer>())
        {
            Material m = ghostMaterial != null ? new Material(ghostMaterial) : r.material;
            r.material = m;
            ghostMats.Add(m);
        }
    }

    void DestroyPreview()
    {
        if (preview != null) Destroy(preview);
        preview = null;

        foreach (Material m in ghostMats)
            if (m != null && ghostMaterial != null) Destroy(m);
        ghostMats.Clear();
        hasTarget = false;
    }

    // Ekranın ortasından ışın atar, hedef hücrenin merkezini bulur
    void UpdateTarget()
    {
        hasTarget = false;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray;
        if (useCursorAiming && Mouse.current != null)
            ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());   // fare imleci
        else
            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));         // ekran ortası

        RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildDistance, groundLayer, QueryTriggerInteraction.Ignore);

        RaycastHit best = default;
        float bestDist = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;                              // kendi karakterimiz
            if (preview != null && h.transform.IsChildOf(preview.transform)) continue;   // hayalet
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                best = h;
            }
        }
        if (bestDist == float.MaxValue) return;

        Vector3 center;
        if (centerOf.TryGetValue(best.collider.transform.root, out Vector3 hitBlockCenter))
        {
            // Bir bloğa baktık: baktığımız yüzün yanına koy
            center = hitBlockCenter + NormalToStep(best.normal) * blockSize;
        }
        else
        {
            // Zemine (ya da başka bir şeye) baktık: X ve Z ızgaraya oturur, blok zemine konur
            center = new Vector3(
                Mathf.Round(best.point.x / blockSize) * blockSize,
                best.point.y + blockSize * 0.5f,
                Mathf.Round(best.point.z / blockSize) * blockSize);
            center = SnapHeightToNeighbors(center);
        }

        targetCenter = center;
        hasTarget = true;
        canPlace = !IsOccupied(center) && !PlayerInside(center) && CanAfford();
    }

    // Yakındaki bir blok varsa onun yüksekliğine hizala (zemin eğimli olsa da duvarlar düz kalsın)
    Vector3 SnapHeightToNeighbors(Vector3 c)
    {
        foreach (KeyValuePair<Transform, Vector3> kv in centerOf)
        {
            if (kv.Key == null) continue;
            Vector3 d = kv.Value - c;
            if (Mathf.Abs(d.x) < blockSize * 1.1f && Mathf.Abs(d.z) < blockSize * 1.1f && Mathf.Abs(d.y) < blockSize * 0.6f)
            {
                c.y = kv.Value.y;
                break;
            }
        }
        return c;
    }

    // Çarpma yüzünün normalini en yakın eksene çevirir (üst, alt, yan)
    static Vector3 NormalToStep(Vector3 n)
    {
        Vector3 a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
        if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(n.x), 0f, 0f);
        if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(n.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(n.z));
    }

    // Bu hücrede zaten bir blok var mı? (kutuyu biraz küçük tutuyoruz, komşu bloklar "dolu" saymasın)
    bool IsOccupied(Vector3 center)
    {
        Collider[] cols = Physics.OverlapBox(center, Vector3.one * (blockSize * 0.45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider c in cols)
            if (centerOf.ContainsKey(c.transform.root)) return true;
        return false;
    }

    // Karakteri blokla sıkıştırma
    bool PlayerInside(Vector3 center)
    {
        return body != null && body.bounds.Intersects(new Bounds(center, Vector3.one * (blockSize * 0.98f)));
    }

    bool CanAfford()
    {
        return inventory == null || cost <= 0 || inventory.Count(costItem) >= cost;
    }

    void UpdatePreview()
    {
        if (preview == null) return;

        preview.SetActive(hasTarget);
        if (!hasTarget) return;

        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        preview.transform.SetPositionAndRotation(targetCenter - rot * centerOffset, rot);

        Color c = canPlace ? validColor : invalidColor;
        foreach (Material m in ghostMats)
            if (m != null) m.color = c;
    }

    void Place()
    {
        if (!canPlace)
        {
            if (!CanAfford() && inventory != null) inventory.Message($"{cost} {costItem} gerekli");
            return;
        }

        if (inventory != null && cost > 0 && !inventory.Remove(costItem, cost))
        {
            inventory.Message($"{cost} {costItem} gerekli");
            return;
        }

        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        GameObject placed = Instantiate(piecePrefab, targetCenter - rot * centerOffset, rot);
        placed.transform.localScale *= scaleFactor;
        placed.name = "Placed_" + piecePrefab.name;

        // Duvar yerçekimiyle kaymasın / itilmesin
        foreach (Rigidbody rb in placed.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        centerOf[placed.transform] = targetCenter;
    }

    // Önce collider'lara, yoksa renderer'lara göre sınır kutusu
    static Bounds GetBounds(GameObject go)
    {
        Collider[] cols = go.GetComponentsInChildren<Collider>();
        if (cols.Length > 0)
        {
            Bounds cb = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) cb.Encapsulate(cols[i].bounds);
            return cb;
        }

        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds rb = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) rb.Encapsulate(rs[i].bounds);
        return rb;
    }

    void OnGUI()
    {
        if (!isBuilding) return;

        // Nişangah (sadece imleç hedeflemesi kapalıyken)
        if (!useCursorAiming)
            GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);

        string cameraHint = useCursorAiming ? "  |  Sağ tuş basılı: kamera" : "";
        GUI.Box(new Rect(Screen.width / 2f - 300, Screen.height - 70, 600, 36),
            $"İNŞA MODU  |  Sol tık: koy ({cost} {costItem})  |  R: çevir{cameraHint}  |  B: çık");
    }
}