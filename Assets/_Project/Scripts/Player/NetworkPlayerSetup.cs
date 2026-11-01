using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Ağ oyuncusunun (PlayerArmature) prefab'ının köküne ekle.
// Her bilgisayarda oyuncunun bir kopyası var; yalnızca SAHİBİNİN kopyası klavye/fare ile oynanır.
// Diğer kopyalar sadece NetworkTransform'dan gelen konumu gösterir.
public class NetworkPlayerSetup : NetworkBehaviour
{
    [Tooltip("Sadece sahibinin bilgisayarında açık kalacak bileşenler (ThirdPersonController, StarterAssetsInputs, PlayerInput...)")]
    [SerializeField] Behaviour[] ownerOnly;

    [Tooltip("Kameranın takip edeceği nokta (PlayerArmature altındaki PlayerCameraRoot)")]
    [SerializeField] Transform cameraTarget;

    [Header("Doğma noktası")]
    [SerializeField] Vector3 spawnCenter = Vector3.zero;   // X ve Z kullanılır; yükseklik araziye ışın atılarak bulunur
    [SerializeField] float spawnSpacing = 3f;              // Oyuncular üst üste doğmasın

    public override void OnNetworkSpawn()
    {
        bool mine = IsOwner;

        foreach (Behaviour b in ownerOnly)
            if (b != null) b.enabled = mine;

        // CharacterController başkasının kopyasında konum aktarımıyla çakışmasın
        TryGetComponent(out CharacterController cc);
        if (cc != null) cc.enabled = false;

        if (!mine) return;

        MoveToSpawn();
        if (cc != null) cc.enabled = true;

        // Sahnedeki takip kamerasını kendi oyuncumuza bağla
        CinemachineCamera follow = FindFirstObjectByType<CinemachineCamera>();
        if (follow != null && cameraTarget != null) follow.Follow = cameraTarget;
    }

    void MoveToSpawn()
    {
        Vector3 p = spawnCenter + Vector3.right * (OwnerClientId * spawnSpacing);

        if (Physics.Raycast(new Vector3(p.x, 1000f, p.z), Vector3.down, out RaycastHit hit, 2000f))
            p = hit.point + Vector3.up * 1f;

        if (TryGetComponent(out NetworkTransform nt))
            nt.Teleport(p, transform.rotation, transform.localScale);
        else
            transform.position = p;
    }
}
