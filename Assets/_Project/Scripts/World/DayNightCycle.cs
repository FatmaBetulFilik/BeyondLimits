using Unity.Netcode;
using UnityEngine;

// Directional Light'a ekle. Güneşi döndürür, gece-gündüz döngüsü yapar.
// AĞ: Zaman server saatinden okunur, bu yüzden herkes aynı anda gündüz/gece görür (bu bileşene NetworkObject gerekmez).
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] float dayLengthSeconds = 120f;          // Bir tam gün kaç saniye
    [SerializeField, Range(0f, 1f)] float startTime = 0.3f;  // 0.25 sabah, 0.5 öğle, 0.75 akşam
    [SerializeField] float maxIntensity = 1.2f;

    Light sun;

    void Start()
    {
        sun = GetComponent<Light>();
    }

    void Update()
    {
        // Ağa bağlıysa server saati, değilse yerel saat
        bool online = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        double seconds = online ? NetworkManager.Singleton.ServerTime.Time : Time.timeAsDouble;

        float time01 = (float)((startTime + seconds / dayLengthSeconds) % 1.0);

        float angle = time01 * 360f - 90f;   // 0.25'te ufukta, 0.5'te tepede
        transform.rotation = Quaternion.Euler(angle, 170f, 0f);

        if (sun != null)
            sun.intensity = Mathf.Max(0.05f, Mathf.Sin(angle * Mathf.Deg2Rad)) * maxIntensity;
    }
}
