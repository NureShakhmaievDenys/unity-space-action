using System.Collections.Generic;
using UnityEngine;

namespace FleetGameContent.Scripts
{
    public class ActionCameraMover : MonoBehaviour
    {
        [Header("UI Ignore Settings")]
        [Tooltip("Поместите сюда RectTransform джойстика, ползунка и других кнопок")]
        [SerializeField] private RectTransform[] ignoredUIElements;

        [Header("Camera Settings")]
        [SerializeField] private float speedrot = 0.05f;
        [SerializeField] private bool inversionY, inversionX;
        [SerializeField] private float followSpeed = 2f; // Скорость задержки камеры
    
        [SerializeField] private float zoomSpeed = 20f; // Скорость изменения угла обзора
        [SerializeField] private float minFOV = 10f, maxFOV = 80f; // Ограничения угла обзора

        private float _rotx, _roty;
        private float _targetYRotation; // Целевая ротация для оси Y самолета
        private Transform _ship;
        private Camera _actionCamera;

        // Список для хранения только "свободных" от UI касаний
        private readonly List<Touch> _validTouches = new List<Touch>();
        
        // === НОВОЕ: Черный список для пальцев, которые начали движение на UI ===
        private readonly HashSet<int> _ignoredFingerIds = new HashSet<int>();

        private void Start()
        {
            _ship = transform.parent;
            _targetYRotation = _ship.eulerAngles.y; // Запоминаем начальный угол
            _actionCamera = transform.GetChild(0).gameObject.GetComponent<Camera>();
        }

        private void Update()
        {
            FilterTouches(); // Сначала отсеиваем UI-касания
            UpdateAngles();
            UpdateFieldOfView();
        }

        private void FilterTouches()
        {
            _validTouches.Clear(); 
            
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                
                // 1. Когда палец только касается экрана, проверяем, попал ли он в UI
                if (t.phase == TouchPhase.Began)
                {
                    if (IsTouchOnIgnoredUI(t.position))
                    {
                        // Запоминаем ID этого пальца, чтобы игнорировать его дальше
                        _ignoredFingerIds.Add(t.fingerId);
                    }
                }
                // 2. Когда палец отрывается от экрана, забываем его ID
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _ignoredFingerIds.Remove(t.fingerId);
                }

                // 3. Если ID пальца НЕТ в черном списке, добавляем его в рабочие касания камеры
                if (!_ignoredFingerIds.Contains(t.fingerId))
                {
                    _validTouches.Add(t);
                }
            }
        }

        private bool IsTouchOnIgnoredUI(Vector2 touchPosition)
        {
            if (ignoredUIElements == null || ignoredUIElements.Length == 0) return false;

            foreach (var uiRect in ignoredUIElements)
            {
                if (uiRect != null && RectTransformUtility.RectangleContainsScreenPoint(uiRect, touchPosition, null))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateAngles()
        {
            // Используем только валидные касания
            if (_validTouches.Count > 0)
            {
                Touch t = _validTouches[0];

                if (t.phase == TouchPhase.Moved)
                {
                    _roty += (inversionY ? t.deltaPosition.x : -t.deltaPosition.x) * speedrot;
                    _rotx += (inversionX ? -t.deltaPosition.y : t.deltaPosition.y) * speedrot;
                }
            }

            // Корректируем угол для плавного следования без резких скачков
            float yDelta = Mathf.DeltaAngle(_targetYRotation, _ship.eulerAngles.y);
            _targetYRotation += yDelta * Time.deltaTime * followSpeed;

            // Формируем итоговую ориентацию
            Quaternion aircraftRotation = Quaternion.Euler(0, _targetYRotation, 0);
            Quaternion cameraRotation = Quaternion.Euler(_rotx, _roty, 0);

            transform.rotation = aircraftRotation * cameraRotation;
        }

        private void UpdateFieldOfView()
        {
            // Управление колесиком мыши для ПК
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _actionCamera.fieldOfView -= scroll * zoomSpeed;
            }
        
            // Нажатие Ctrl уменьшает FOV, Shift увеличивает.
            if (Input.GetKey(KeyCode.LeftControl)) 
                _actionCamera.fieldOfView -= zoomSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) 
                _actionCamera.fieldOfView += zoomSpeed;

            // Управление жестом "щипок" на мобильных устройствах
            // Зум срабатывает, только если у нас есть минимум 2 касания, НЕ попавших на джойстик/ползунок
            if (_validTouches.Count >= 2)
            {
                Touch touch0 = _validTouches[0];
                Touch touch1 = _validTouches[1];

                float prevDistance = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
                float currentDistance = (touch0.position - touch1.position).magnitude;

                float deltaPinch = prevDistance - currentDistance;
                _actionCamera.fieldOfView += deltaPinch * zoomSpeed * 0.04f * Time.deltaTime;
            }

            // Ограничение угла обзора
            _actionCamera.fieldOfView = Mathf.Clamp(_actionCamera.fieldOfView, minFOV, maxFOV);
        }
    }
}