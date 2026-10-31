using UnityEngine;
using UnityEngine.InputSystem;

// PlayerArmature'a ekle. Can, açlık, susuzluk barlarını yönetir ve gösterir.
public class PlayerStats : MonoBehaviour
{
    public float maxValue = 100f;
    public float health = 100f;
    public float hunger = 100f;
    public float thirst = 100f;

    [SerializeField] float hungerPerSecond = 0.5f;   // Açlık azalma hızı
    [SerializeField] float thirstPerSecond = 0.8f;   // Susuzluk azalma hızı
    [SerializeField] float starveDamagePerSecond = 2f;

    public bool IsDead => health <= 0;

    void Update()
    {
        if (IsDead)
        {
            // Ölünce R ile yeniden başla
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                health = hunger = thirst = maxValue;
            return;
        }

        hunger = Mathf.Max(0, hunger - hungerPerSecond * Time.deltaTime);
        thirst = Mathf.Max(0, thirst - thirstPerSecond * Time.deltaTime);

        if (hunger <= 0 || thirst <= 0)
            Damage(starveDamagePerSecond * Time.deltaTime);
    }

    public void Damage(float amount) => health = Mathf.Max(0, health - amount);
    public void Heal(float amount)   => health = Mathf.Min(maxValue, health + amount);
    public void Eat(float amount)    => hunger = Mathf.Min(maxValue, hunger + amount);
    public void Drink(float amount)  => thirst = Mathf.Min(maxValue, thirst + amount);

    void OnGUI()
    {
        DrawBar(0, "Can", health, Color.red);
        DrawBar(1, "Açlık", hunger, new Color(1f, 0.6f, 0f));
        DrawBar(2, "Susuzluk", thirst, new Color(0.2f, 0.6f, 1f));

        if (IsDead)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120), "ÖLDÜN\n<size=24>R ile yeniden başla</size>", style);
        }
    }

    void DrawBar(int row, string label, float value, Color color)
    {
        float w = 220, h = 20;
        float x = Screen.width - w - 20;
        float y = 20 + row * (h + 8);

        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, w * (value / maxValue), h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 6, y + 1, w, h), $"{label}: {Mathf.CeilToInt(value)}");
    }
}