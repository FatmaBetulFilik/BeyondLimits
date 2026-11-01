using Unity.Netcode;
using UnityEngine;

// Yerden alınıp kaybolan eşyalar (çakıl taşı, odun parçası, balta, meyve...).
// AĞ: Biri alınca herkeste kaybolur (taken NetworkVariable). Nesnede NetworkObject bileşeni olmalı.
public class Pickup : NetworkBehaviour, IInteractable
{
    [SerializeField] string itemName = "Taş";
    [SerializeField] int amount = 1;

    readonly NetworkVariable<bool> taken = new NetworkVariable<bool>(false);

    public string Prompt => $"Al: {itemName}";
    public bool CanInteract => IsSpawned && !taken.Value;

    public override void OnNetworkSpawn()
    {
        taken.OnValueChanged += OnTakenChanged;
        InteractableUtil.SetVisible(gameObject, !taken.Value);
    }

    public override void OnNetworkDespawn()
    {
        taken.OnValueChanged -= OnTakenChanged;
    }

    void OnTakenChanged(bool previous, bool current)
    {
        InteractableUtil.SetVisible(gameObject, !current);
    }

    // Sadece server çağırır
    public void Interact(GameObject interactor)
    {
        if (!IsServer || taken.Value) return;

        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null) inv.Add(itemName, amount);
        taken.Value = true;
    }
}
