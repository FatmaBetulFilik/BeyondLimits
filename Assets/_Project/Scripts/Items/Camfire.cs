using UnityEngine;

// Bonfire modeline ekle. Odunla yakılır, alev ve ışık açılır, yakındaki oyuncunun canını doldurur.
public class Campfire : MonoBehaviour, IInteractable
{
    [SerializeField] int woodCost = 3;
    [SerializeField] float burnTime = 60f;
    [SerializeField] Light fireLight;              // Point Light
    [SerializeField] GameObject fireVisual;        // Alev efekti (Particle System)
    [SerializeField] float lightIntensity = 6f;    // Işık gücü
    [SerializeField] float healRadius = 4f;
    [SerializeField] float healPerSecond = 3f;

    float burnLeft;
    PlayerStats player;

    bool Lit => burnLeft > 0;

    public string Prompt => $"Ateşi yak ({woodCost} Odun)";
    public bool CanInteract => !Lit;

    void Start()
    {
        player = FindFirstObjectByType<PlayerStats>();
        SetFire(false);
    }

    public void Interact(GameObject interactor)
    {
        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv == null) return;

        if (!inv.Remove("Odun", woodCost))
        {
            inv.Message($"Yeterli odun yok! {woodCost} Odun gerekli.");
            return;
        }

        burnLeft = burnTime;
        SetFire(true);
        inv.Message("Ateş yandı!");
    }

    void Update()
    {
        if (!Lit) return;

        burnLeft -= Time.deltaTime;

        if (fireLight != null)
            fireLight.intensity = lightIntensity * (0.7f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.6f);

        if (player != null && Vector3.Distance(player.transform.position, transform.position) < healRadius)
            player.Heal(healPerSecond * Time.deltaTime);

        if (burnLeft <= 0) SetFire(false);
    }

    void SetFire(bool on)
    {
        if (fireLight != null) fireLight.enabled = on;
        if (fireVisual != null) fireVisual.SetActive(on);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}