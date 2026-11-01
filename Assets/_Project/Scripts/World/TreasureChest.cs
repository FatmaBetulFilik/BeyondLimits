using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Hazine sandığı. Bir kez açılır, ganimeti envantere ekler.
// Modelin kapağı ayrı bir parçaysa onu "Lid" alanına sürükle; açılırken döner.
// AĞ: opened NetworkVariable'dır; biri açınca herkeste kapak açılır, sonradan katılan açık görür.
public class TreasureChest : NetworkBehaviour, IInteractable
{
    [SerializeField] Transform lid;
    [SerializeField] Vector3 openRotation = new Vector3(-110f, 0f, 0f);
    [SerializeField] string lootName = "Altın";
    [SerializeField] int lootAmount = 10;

    readonly NetworkVariable<bool> opened = new NetworkVariable<bool>(false);
    Quaternion lidClosed;
    Quaternion lidOpen;

    public string Prompt => "Sandığı aç";
    public bool CanInteract => IsSpawned && !opened.Value;

    void Awake()
    {
        if (lid == null) return;
        lidClosed = lid.localRotation;
        lidOpen = lidClosed * Quaternion.Euler(openRotation);
    }

    public override void OnNetworkSpawn()
    {
        opened.OnValueChanged += OnOpenedChanged;
        if (opened.Value && lid != null) lid.localRotation = lidOpen;   // sonradan katılan için
    }

    public override void OnNetworkDespawn()
    {
        opened.OnValueChanged -= OnOpenedChanged;
    }

    void OnOpenedChanged(bool previous, bool current)
    {
        if (current && lid != null) StartCoroutine(OpenLid());
    }

    // Sadece server çağırır
    public void Interact(GameObject interactor)
    {
        if (!IsServer || opened.Value) return;
        opened.Value = true;

        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null) inv.Add(lootName, lootAmount);
    }

    IEnumerator OpenLid()
    {
        for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
        {
            lid.localRotation = Quaternion.Slerp(lidClosed, lidOpen, t);
            yield return null;
        }
        lid.localRotation = lidOpen;
    }
}
