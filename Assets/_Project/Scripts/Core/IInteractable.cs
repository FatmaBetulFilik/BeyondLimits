using UnityEngine;

// Oyuncunun E tuşuyla etkileşebileceği her şey bu arayüzü uygular
// (sandık, ağaç, taş, çalı, yerdeki eşya...).
public interface IInteractable
{
    string Prompt { get; }          // Ekranda görünecek yazı: "Sandığı aç", "Kes (3)"
    bool CanInteract { get; }       // Şu an etkileşime açık mı?
    void Interact(GameObject interactor);
}