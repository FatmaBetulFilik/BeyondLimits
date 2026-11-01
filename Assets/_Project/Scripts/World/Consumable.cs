using Unity.Netcode;
using UnityEngine;

// Su şişesi, bandaj, ilk yardım çantası gibi anında kullanılan eşyalar.
// AĞ: Kullanılınca herkeste kaybolur. Nesnede NetworkObject bileşeni olmalı.
public class Consumable : NetworkBehaviour, IInteractable
{
    public enum Kind { Yemek, Su, IlkYardim }

    [SerializeField] Kind kind = Kind.Su;
    [SerializeField] float amount = 40f;
    [SerializeField] string displayName = "Su şişesi";

    readonly NetworkVariable<bool> used = new NetworkVariable<bool>(false);

    public string Prompt => $"Kullan: {displayName}";
    public bool CanInteract => IsSpawned && !used.Value;

    public override void OnNetworkSpawn()
    {
        used.OnValueChanged += OnUsedChanged;
        InteractableUtil.SetVisible(gameObject, !used.Value);
    }

    public override void OnNetworkDespawn()
    {
        used.OnValueChanged -= OnUsedChanged;
    }

    void OnUsedChanged(bool previous, bool current)
    {
        InteractableUtil.SetVisible(gameObject, !current);
    }

    // Sadece server çağırır
    public void Interact(GameObject interactor)
    {
        if (!IsServer || used.Value) return;

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

        used.Value = true;
    }
}
