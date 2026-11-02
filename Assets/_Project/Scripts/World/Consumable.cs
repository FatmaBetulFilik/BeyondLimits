using UnityEngine;

// Su şişesi, bandaj, ilk yardım çantası gibi anında kullanılan eşyalar.
public class Consumable : MonoBehaviour, IInteractable
{
    public enum Kind { Yemek, Su, IlkYardim }

    [SerializeField] Kind kind = Kind.Su;
    [SerializeField] float amount = 40f;
    [SerializeField] string displayName = "Su şişesi";

    public string Prompt => $"Kullan: {displayName}";
    public bool CanInteract => true;

    public void Interact(GameObject interactor)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();
        if (stats == null) return;

        switch (kind)
        {
            case Kind.Yemek:     stats.Eat(amount);   break;
            case Kind.Su:        stats.Drink(amount); break;
            case Kind.IlkYardim: stats.Heal(amount);  break;
        }

        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null) inv.Message($"{displayName} kullanıldı (+{amount})");

        Destroy(gameObject);
    }
}