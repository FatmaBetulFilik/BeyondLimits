using UnityEngine;

public enum WaterType { Ocean, Mud, Pond }

public class WaterZone : MonoBehaviour
{
    public WaterType type = WaterType.Ocean;
    [Range(0.1f, 1f)] public float speedMultiplier = 1f;
    public bool canSwim = true;
    public AudioClip enterSound;
}
