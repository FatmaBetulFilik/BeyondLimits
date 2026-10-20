using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("İnşa Ayarları")]
    public GameObject piecePrefab;       
    public float maxBuildDistance = 100f; 
    public LayerMask groundLayer;        

    private Camera mainCamera;
    private GameObject currentPreview;   
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
                Destroy(currentPreview); 
            }
        }

        if (isBuilding)
        {
            HandleBuilding();
        }
    }

    void HandleBuilding()
    {
        // 1. Hayalet Objeyi Oluşturma
        if (currentPreview == null)
        {
            currentPreview = Instantiate(piecePrefab);
            
            // Fiziğini kapat (havada asılı kalsın, düşmesin)
            Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 
            
            // Rengini yarı şeffaf yeşil yap
            Renderer rend = currentPreview.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0, 1, 0, 0.5f);
            }
        }

        // 2. Fareyi Takip Etme (Işın / Raycast)
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Sadece zemin katmanına veya objelere çarparsa
        if (Physics.Raycast(ray, out hit, maxBuildDistance, groundLayer))
        {
            Vector3 placePosition = new Vector3(hit.point.x, hit.point.y + 0.5f, hit.point.z);
            currentPreview.transform.position = placePosition;

            // 3. Sol Tık ile Yerleştirme
            if (Input.GetMouseButtonDown(0))
            {
                PlacePiece(placePosition);
            }
        }
    }

    void PlacePiece(Vector3 position)
    {
        // A. Gerçek parçayı oluştur
        GameObject placedObj = Instantiate(piecePrefab, position, Quaternion.identity);
        placedObj.name = "Placed_Wood_Piece";
        
        // B. Gerçek objenin rengini normale döndür
        Renderer rend = placedObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.white; 
        }

        // C. ÇOK ÖNEMLİ: Hayalet objeyi yok et (Böylece yerleşen tahta görünür)
        Destroy(currentPreview); 
        currentPreview = null; // Yeni bir tahta koymak için sıfırla

        Debug.Log("Tahta başarıyla yerleştirildi!");
    }
}