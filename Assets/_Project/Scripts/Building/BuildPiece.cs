using Unity.Netcode;
using UnityEngine;

// İnşa edilebilen parçanın prefab'ının köküne ekle (NetworkObject ile birlikte).
// Rigidbody varsa kinematik yapar: duvar yerçekimiyle kaymasın ve herkeste aynı yerde dursun.
public class BuildPiece : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }
}
