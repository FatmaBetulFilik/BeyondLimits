using UnityEngine;

// Oyuncunun E tuşuyla etkileşebileceği her şey bu arayüzü uygular
// (sandık, ağaç, taş, çalı, yerdeki eşya...).
//
// AĞ NOTU: Interact() SADECE SERVER'DA çağrılır (PlayerInteractor isteği server'a gönderir).
// Prompt ve CanInteract her bilgisayarda okunur, bu yüzden NetworkVariable'dan beslenmelidir.
public interface IInteractable
{
    string Prompt { get; }          // Ekranda görünecek yazı: "Sandığı aç", "Kes (3)"
    bool CanInteract { get; }       // Şu an etkileşime açık mı?
    void Interact(GameObject interactor);
}

// Ağda "yok olan" nesneleri gizlemek için ortak yardımcı.
// Nesneyi Destroy etmek yerine gizliyoruz: sonradan katılan oyuncu da durumu NetworkVariable'dan okuyup aynı şekilde görür.
public static class InteractableUtil
{
    public static void SetVisible(GameObject go, bool visible)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) r.enabled = visible;
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) c.enabled = visible;
    }
}
