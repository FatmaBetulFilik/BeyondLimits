using UnityEngine;

// Yerden alınıp kaybolan eşyalar (çakıl taşı, odun parçası, meyve...).
public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] string itemName = "Taş";
    [SerializeField] int amount = 1;

    public string Prompt => $"Al: {itemName}";
    public bool CanInteract => true;

    public void Interact(GameObject interactor)
    {
        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null) inv.Add(itemName, amount);
        Destroy(gameObject);
    }
}