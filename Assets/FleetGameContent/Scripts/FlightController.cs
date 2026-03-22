using FleetGameContent.VFX;
using UnityEngine;
using UnityEngine.UI;

namespace FleetGameContent.Scripts
{
    public class FlightController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Joystick yokeInput;
        [SerializeField] private Slider thrustInput;

        [Header("Anim sets")] 
        [SerializeField] private float maxEngineModuleXAngle; 
        [SerializeField] private float minEngineModuleXAngle; 
        [SerializeField] private float engineRotationSpeed = 10f; 

        [Header("Refs")]
        [SerializeField] private Transform engineModule;
        [SerializeField] private FlameThrustController[] leftEngines;
        [SerializeField] private FlameThrustController[] rightEngines;

        [Header("Audio Settings (Cinematic)")] 
        [SerializeField] private AudioSource engineAudio;
        [SerializeField] private float minPitch = 0.6f;   // Низкий утробный гул простоя
        [SerializeField] private float maxPitch = 1.5f;   // Высокий рев на форсаже
        [SerializeField] private float minVolume = 0.05f; // Почти не слышно, когда нет инпута (можно поставить 0)
        [SerializeField] private float maxVolume = 1.0f;
        [SerializeField] private float audioSmoothSpeed = 4f; // Насколько плавно звук разгоняется и остывает
        [SerializeField] private float rotationAudioFactor = 0.5f; // Чувствительность звука к маневрам

        [Header("Physics Settings")] 
        [SerializeField] private float thrustPower = 500f; 
        [SerializeField] private float pitchTorque = 500f;  
        [SerializeField] private float yawTorque = 500f;    

        [Header("Damping Settings (Anti-Iron)")] 
        [SerializeField] private float linearDamping = 3f;   
        [SerializeField] private float angularDamping = 5f;  

        private Rigidbody _rigidbody;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;
            _rigidbody.useGravity = false; 

            // Убеждаемся, что аудиосорс включен и зациклен при старте
            if (engineAudio != null)
            {
                engineAudio.loop = true;
                engineAudio.pitch = minPitch;
                engineAudio.volume = minVolume;
                if (!engineAudio.isPlaying) engineAudio.Play();
            }
        }

        private void Update()
        {
            AnimateFlames();
            UpdateAudio(); // === Вызываем кинематографичный звук каждый кадр ===
        }

        private void FixedUpdate()
        {
            ApplyInputToARigidbody();
            StabilizeRotation(); 
        }

        private void ApplyInputToARigidbody()
        {
            if (_rigidbody == null) return;

            // 1. ГАШЕНИЕ ИМПУЛЬСА
            _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, Vector3.zero, linearDamping * Time.fixedDeltaTime);
            _rigidbody.angularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, Vector3.zero, angularDamping * Time.fixedDeltaTime);

            // 2. ТЯГА
            Vector3 forwardForce = Vector3.forward * (thrustInput.value * thrustPower);
            _rigidbody.AddRelativeForce(forwardForce * Time.fixedDeltaTime, ForceMode.Force);

            // 3. ВРАЩЕНИЕ
            float pitch = yokeInput.Vertical * pitchTorque; 
            float yaw = yokeInput.Horizontal * yawTorque;

            Vector3 rotationTorque = new Vector3(pitch, yaw, 0f);
            _rigidbody.AddRelativeTorque(rotationTorque * Time.fixedDeltaTime, ForceMode.Force);
        }

        private void StabilizeRotation()
        {
            Vector3 currentEuler = transform.eulerAngles;

            float pitchAngle = currentEuler.x;
            if (pitchAngle > 180f) pitchAngle -= 360f;

            pitchAngle = Mathf.Clamp(pitchAngle, -80f, 80f);

            transform.eulerAngles = new Vector3(pitchAngle, currentEuler.y, 0f);

            Vector3 localAngularVelocity = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            localAngularVelocity.z = 0f;
            _rigidbody.angularVelocity = transform.TransformDirection(localAngularVelocity);
        }

        private void AnimateFlames()
        {
            float horizontalInfluenceToThrustAnimation = 0.5f;
            float leftFlamesIntensity = thrustInput.value - (yokeInput.Horizontal < 0 ? -yokeInput.Horizontal : 0) * horizontalInfluenceToThrustAnimation;
            float rightFlamesIntensity = thrustInput.value - (yokeInput.Horizontal > 0 ? yokeInput.Horizontal : 0) * horizontalInfluenceToThrustAnimation;

            foreach (FlameThrustController leftEngine in leftEngines)
                leftEngine.thrustLevel = leftFlamesIntensity;
            
            foreach (FlameThrustController rightEngine in rightEngines)
                rightEngine.thrustLevel = rightFlamesIntensity;
            
            if (engineModule != null)
            {
                float invertedVertical = -yokeInput.Vertical;
                float normalizedInput = (invertedVertical + 1f) / 2f;
                
                float targetAngleX = Mathf.Lerp(minEngineModuleXAngle, maxEngineModuleXAngle, normalizedInput);
                
                Vector3 currentEuler = engineModule.localEulerAngles;
                Quaternion targetRotation = Quaternion.Euler(targetAngleX, currentEuler.y, currentEuler.z);
                
                engineModule.localRotation = Quaternion.Lerp(engineModule.localRotation, targetRotation, engineRotationSpeed * Time.deltaTime);
            }
        }

        // === НОВЫЙ МЕТОД: Управление звуком ===
        private void UpdateAudio()
        {
            if (engineAudio == null || _rigidbody == null) return;

            // 1. Учитываем тягу маршевых двигателей
            float thrustIntensity = thrustInput.value;

            // 2. Учитываем работу маневровых двигателей (берем физическую скорость вращения)
            // Мы берем magnitude, поэтому звук реагирует на вращение по любой оси
            float rotationIntensity = Mathf.Clamp01(_rigidbody.angularVelocity.magnitude * rotationAudioFactor);

            // 3. Общая интенсивность — это максимум между тягой и маневрами.
            // (Если стоим на месте и резко крутим стик, двигатели все равно должны реветь)
            float combinedIntensity = Mathf.Clamp01(Mathf.Max(thrustIntensity, rotationIntensity));

            // 4. Считаем, какими должны быть питч и громкость в этот момент
            float targetPitch = Mathf.Lerp(minPitch, maxPitch, combinedIntensity);
            float targetVolume = Mathf.Lerp(minVolume, maxVolume, combinedIntensity);

            // 5. Плавный переход (Lerp) к целевым значениям.
            // Именно это убирает механическую резкость и дает голливудский "тяжелый" звук турбин.
            engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, targetPitch, Time.deltaTime * audioSmoothSpeed);
            engineAudio.volume = Mathf.Lerp(engineAudio.volume, targetVolume, Time.deltaTime * audioSmoothSpeed);
        }
    }
}