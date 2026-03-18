using UnityEngine;

namespace FleetGameContent.VFX
{
    public class FlameThrustController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem flameParticles;

        [Header("Thrust Control")]
        // Ползунок тяги от 0 (минимум) до 1 (максимум)
        [Range(0f, 1f)] 
        public float thrustLevel = 1f;

        [Header("Speed Settings")]
        [SerializeField] private float minSpeed = 3f;
        [SerializeField] private float maxSpeed = 10f;

        private void Update()
        {
            // Проверка на случай, если ты забыл перетащить систему частиц в инспекторе
            if (flameParticles == null) return;

            // В Unity параметры Particle System меняются через её внутренние модули
            var main = flameParticles.main;

            // Функция Lerp плавно вычисляет значение между 3 и 10 
            // на основе того, где сейчас находится ползунок (от 0 до 1)
            main.startSpeed = Mathf.Lerp(minSpeed, maxSpeed, thrustLevel);

            // Небольшой бонус для оптимизации:
            // Если тяга убрана в ноль, мы просто выключаем генерацию новых частиц
            var emission = flameParticles.emission;
            emission.enabled = thrustLevel > 0.01f;
        }
    }
}