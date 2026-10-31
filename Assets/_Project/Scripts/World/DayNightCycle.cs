using UnityEngine;

// Directional Light'a ekle. Güneşi döndürür, gece-gündüz döngüsü yapar.
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] float dayLengthSeconds = 120f;          // Bir tam gün kaç saniye
    [SerializeField, Range(0f, 1f)] float startTime = 0.3f;  // 0.25 sabah, 0.5 öğle, 0.75 akşam
    [SerializeField] float maxIntensity = 1.2f;

    float time01;
    Light sun;

    void Start()
    {
        time01 = startTime;
        sun = GetComponent<Light>();
    }

    void Update()
    {
        time01 = (time01 + Time.deltaTime / dayLengthSeconds) % 1f;

        float angle = time01 * 360f - 90f;   // 0.25'te ufukta, 0.5'te tepede
        transform.rotation = Quaternion.Euler(angle, 170f, 0f);

        if (sun != null)
            sun.intensity = Mathf.Max(0.05f, Mathf.Sin(angle * Mathf.Deg2Rad)) * maxIntensity;
    }
}