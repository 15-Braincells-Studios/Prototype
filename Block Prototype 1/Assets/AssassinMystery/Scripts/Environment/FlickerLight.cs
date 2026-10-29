using UnityEngine;

namespace AssassinMystery
{
    /// <summary>Candle / fire flicker on a light's intensity.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] float amount = 0.25f;
        [SerializeField] float speed = 7f;

        Light lightSource;
        float baseIntensity;
        float seed;

        void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            lightSource.intensity = baseIntensity * (1f + n * 2f * amount);
        }
    }
}
