using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomScrollView
{
    public class CustomGridScroller : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("Ссылки")]
        public RectTransform content;
        public RectTransform topMarker;
        public RectTransform bottomMarker;
        
        [Header("Настройки скролла")]
        public ScrollAxis axis = ScrollAxis.Vertical;
        public float scrollSensitivity = 1.0f; 
        [Range(1f, 20f)] public float decelerationRate = 10f;
        
        [Tooltip("Время за которое скролл долетает до цели (автоперемотка)")]
        public float smoothDampTime = 0.2f;

        [Header("Сглаживание пальца (Амортизатор)")]
        [Tooltip("Насколько быстро список догоняет палец. 25-30 — съедает рывки, но не ощущается как лаг.")]
        public float dragSmoothing = 25f;

        public enum ScrollAxis { Vertical, Horizontal }

        private Canvas _parentCanvas;
        private RectTransform _viewportRect;

        // Переменные для физики
        private bool _isDragging;
        private float _velocity;
        
        // Переменные для плавной промотки
        private bool _isAnimating;
        private float _targetAnimPosition;
        private float _animVelocity; 

        // НОВОЕ: Переменная для "призрачной" позиции пальца
        private Vector3 _targetDragPos;

        private void Start()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            _viewportRect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            // 1. АВТОПЕРЕМОТКА (SmoothDamp)
            if (_isAnimating)
            {
                Vector3 currentPos = content.localPosition;
                
                if (axis == ScrollAxis.Vertical)
                {
                    currentPos.y = Mathf.SmoothDamp(currentPos.y, _targetAnimPosition, ref _animVelocity, smoothDampTime, Mathf.Infinity, Time.unscaledDeltaTime);
                    if (Mathf.Abs(currentPos.y - _targetAnimPosition) < 0.5f) { currentPos.y = _targetAnimPosition; _isAnimating = false; }
                }
                else
                {
                    currentPos.x = Mathf.SmoothDamp(currentPos.x, _targetAnimPosition, ref _animVelocity, smoothDampTime, Mathf.Infinity, Time.unscaledDeltaTime);
                    if (Mathf.Abs(currentPos.x - _targetAnimPosition) < 0.5f) { currentPos.x = _targetAnimPosition; _isAnimating = false; }
                }
                
                content.localPosition = currentPos;
                ClampContent();
                return; 
            }

            // 2. НОВОЕ: СГЛАЖИВАНИЕ ДВИЖЕНИЯ ПАЛЬЦА (Амортизатор)
            if (_isDragging)
            {
                // Список плавно "догоняет" виртуальную позицию пальца
                content.localPosition = Vector3.Lerp(content.localPosition, _targetDragPos, Time.unscaledDeltaTime * dragSmoothing);
                
                if (ClampContent()) 
                {
                    // Если уперлись в стену, не даем невидимой цели "улетать" за экран
                    _targetDragPos = content.localPosition; 
                }
                return; 
            }

            // 3. ИНЕРЦИЯ ПОСЛЕ СВАЙПА
            if (!_isDragging && Mathf.Abs(_velocity) > 0.1f)
            {
                Vector3 currentPos = content.localPosition;
                float movement = _velocity * Time.unscaledDeltaTime;

                if (axis == ScrollAxis.Vertical) currentPos.y += movement;
                else currentPos.x += movement;

                content.localPosition = currentPos;
                
                if (ClampContent()) 
                {
                    _velocity = 0;
                }
                else 
                {
                    _velocity = Mathf.Lerp(_velocity, 0, Time.unscaledDeltaTime * decelerationRate);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _isAnimating = false;
            _velocity = 0;
            _animVelocity = 0; 
            
            // Фиксируем стартовую точку
            _targetDragPos = content.localPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_parentCanvas == null) return;

            Vector2 delta = (eventData.delta / _parentCanvas.scaleFactor) * scrollSensitivity;
            float deltaMovement = 0;

            // ВАЖНО: Мы больше не двигаем content напрямую. Мы двигаем только цель (_targetDragPos)
            if (axis == ScrollAxis.Vertical)
            {
                _targetDragPos.y += delta.y;
                deltaMovement = delta.y;
            }
            else
            {
                _targetDragPos.x += delta.x;
                deltaMovement = delta.x;
            }

            // Математику расчета скорости для будущей инерции оставляем как есть
            float instantVelocity = deltaMovement / Time.unscaledDeltaTime;
            if (_velocity == 0) _velocity = instantVelocity;
            else _velocity = Mathf.Lerp(_velocity, instantVelocity, 0.4f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        public void MoveDirectlyToPosition(float position)
        {
            position = Mathf.Clamp01(position);
            _isAnimating = true;
            _velocity = 0;
            _animVelocity = 0; 

            if (axis == ScrollAxis.Vertical)
            {
                _targetAnimPosition = Mathf.Lerp(GetMinClamp(), GetMaxClamp(), position);
            }
            else
            {
                _targetAnimPosition = Mathf.Lerp(GetMinClamp(), GetMaxClamp(), position);
            }
        }

        private bool ClampContent()
        {
            Vector3 clampedPos = content.localPosition;
            bool hitBoundary = false;

            if (axis == ScrollAxis.Vertical)
            {
                float minClamp = GetMinClamp();
                float maxClamp = GetMaxClamp();
                
                if (clampedPos.y < minClamp || clampedPos.y > maxClamp) hitBoundary = true;
                clampedPos.y = Mathf.Clamp(clampedPos.y, minClamp, maxClamp);
            }
            else
            {
                float minClamp = GetMinClamp();
                float maxClamp = GetMaxClamp();
                
                if (clampedPos.x < minClamp || clampedPos.x > maxClamp) hitBoundary = true;
                clampedPos.x = Mathf.Clamp(clampedPos.x, minClamp, maxClamp);
            }

            content.localPosition = clampedPos;
            return hitBoundary;
        }

        private float GetMinClamp() => Mathf.Min(GetTargetA(), GetTargetB());
        private float GetMaxClamp() => Mathf.Max(GetTargetA(), GetTargetB());

        private float GetTargetA()
        {
            if (axis == ScrollAxis.Vertical)
            {
                float viewportTopOffset = (1f - _viewportRect.pivot.y) * _viewportRect.rect.height;
                return -content.InverseTransformPoint(topMarker.position).y + viewportTopOffset;
            }
            else
            {
                float viewportLeftOffset = -_viewportRect.pivot.x * _viewportRect.rect.width;
                return -content.InverseTransformPoint(topMarker.position).x + viewportLeftOffset;
            }
        }

        private float GetTargetB()
        {
            if (axis == ScrollAxis.Vertical)
            {
                float viewportBottomOffset = -_viewportRect.pivot.y * _viewportRect.rect.height;
                return -content.InverseTransformPoint(bottomMarker.position).y + viewportBottomOffset;
            }
            else
            {
                float viewportRightOffset = (1f - _viewportRect.pivot.x) * _viewportRect.rect.width;
                return -content.InverseTransformPoint(bottomMarker.position).x + viewportRightOffset;
            }
        }
    }
}