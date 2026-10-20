using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("İnşa Ayarları")]
    public GameObject piecePrefab;       // Yerleştirilecek tahta prefabı
    public float maxBuildDistance = 100f; // Farenin ulaşabileceği maksimum mesafe (Kamera yüksekse artırılmalı)
    public LayerMask groundLayer;        // Hangi katmanlara inşa edilebilir

    private Camera mainCamera;
    private GameObject currentPreview;   // Fareyle gezen hayalet obje
    private bool isBuilding = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 'B' tuşuna basınca inşa moduna gir/çık
        if (Input.GetKeyDown(KeyCode.B))
        {
            isBuilding = !isBuilding;
            
            if (!isBuilding && currentPreview != null)
            {
                Destroy(currentPreview); // Mod kapalıysa hayaleti yok et
            }
        }

        if (isBuilding)
        {
            HandleBuilding();
        }
    }

    void HandleBuilding()
    {
        // Eğer hayalet objemiz yoksa oluştur
        if (currentPreview == null)
        {
            currentPreview = Instantiate(piecePrefab);
            
            // Fiziğini kapat ki havada asılı kalsın (düşmesin)
            Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 
            
            // Rengini yarı şeffaf yeşil yap
            Renderer rend = currentPreview.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0, 1, 0, 0.5f);
            }
        }

        // Fareden 3D dünyaya bir ışın (Raycast) gönder
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
        RaycastHit hit;

        // Eğer ışın zemine (groundLayer) çarparsa
        if (Physics.Raycast(ray, out hit, 1000f) && hit.collider.gameObject.tag != "BuildPiece") 
        {
            // Hayaleti ışının çarptığı noktaya taşı (Y ekseninde 0.5 yukarı koy ki zemine gömülmesin)
            Vector3 placePosition = new Vector3(hit.point.x, hit.point.y + 0.5f, hit.point.z);
            currentPreview.transform.position = placePosition;

            // Sol tık ile yerleştir
            if (Input.GetMouseButtonDown(0))
            {
                PlacePiece(placePosition);
            }
        }
    }

    void PlacePiece(Vector3 position)
    {
        // Gerçek parçayı oluştur
        GameObject placedObj = Instantiate(piecePrefab, position, Quaternion.identity);
        
        // Objenin adını değiştir ki Hierarchy'de görebilelim
        placedObj.name = "Placed_Wood_Piece";
        
        // Hayaletin rengini sıfırla (Gerçek obje kahverengi olsun)
        Renderer rend = placedObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.white; 
        }

        Debug.Log("Tahta başarıyla yerleştirildi!");
    }
}