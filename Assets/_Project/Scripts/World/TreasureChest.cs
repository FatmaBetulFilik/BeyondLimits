using System.Collections;
using UnityEngine;

// Hazine sandığı. Bir kez açılır, ganimeti envantere ekler.
// Modelin kapağı ayrı bir parçaysa onu "Lid" alanına sürükle; açılırken döner.
public class TreasureChest : MonoBehaviour, IInteractable
{
    [SerializeField] Transform lid;
    [SerializeField] Vector3 openRotation = new Vector3(-110f, 0f, 0f);
    [SerializeField] string lootName = "Altın";
    [SerializeField] int lootAmount = 10;

    bool opened;

    public string Prompt => "Sandığı aç";
    public bool CanInteract => !opened;

    public void Interact(GameObject interactor)
    {
        if (opened) return;
        opened = true;

        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null) inv.Add(lootName, lootAmount);

        if (lid != null) StartCoroutine(OpenLid());
    }

    IEnumerator OpenLid()
    {
        Quaternion start = lid.localRotation;
        Quaternion end = start * Quaternion.Euler(openRotation);
        for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
        {
            lid.localRotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
        lid.localRotation = end;
    }
}