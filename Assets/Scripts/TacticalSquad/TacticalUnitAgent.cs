using UnityEngine;

namespace Sipoga.Tactics
{
    public sealed class TacticalUnitAgent : MonoBehaviour
    {
        private TacticalSquadDirector _director;
        private TacticalUnitPlan _plan;
        private Renderer _bodyRenderer;
        private Renderer _headRenderer;
        private Renderer _directionRenderer;
        private Renderer _selectionRenderer;
        private TextMesh _label;
        private Transform _characterVisualRoot;
        private Color _displayColor;
        private Vector3 _targetWorldPosition;
        private string _targetPositionId = string.Empty;
        private bool _isMoving;
        private bool _isManual;
        private bool _isDown;
        private bool _isSelected;
        private float _lastManualInputTime;
        private int _currentFloor;

        [SerializeField]
        private float moveSpeed = 4.7f;

        public string UnitId
        {
            get { return _plan != null ? _plan.UnitId : string.Empty; }
        }

        public TacticalUnitPlan Plan
        {
            get { return _plan; }
        }

        public string TargetPositionId
        {
            get { return _targetPositionId; }
        }

        public bool IsMoving
        {
            get { return _isMoving; }
        }

        public bool IsManual
        {
            get { return _isManual; }
        }

        public bool IsDown
        {
            get { return _isDown; }
        }

        public int CurrentFloor
        {
            get { return _currentFloor; }
        }

        public void Initialize(
            TacticalSquadDirector director,
            TacticalUnitPlan plan,
            TacticalPositionDefinition initialPosition)
        {
            _director = director;
            _plan = plan;
            _displayColor = plan.DisplayColor;
            gameObject.name = plan.Callsign + " Tactical Operator";
            BuildVisuals();
            Warp(initialPosition);
            UpdateVisualState();
        }

        public void Warp(TacticalPositionDefinition position)
        {
            if (position == null)
            {
                return;
            }

            transform.position = position.WorldPosition;
            _currentFloor = position.Floor;
            _targetWorldPosition = position.WorldPosition;
            _targetPositionId = position.Id;
            _isMoving = false;
            _isManual = false;
        }

        public void OrderTo(TacticalPositionDefinition position)
        {
            if (position == null || _isDown)
            {
                return;
            }

            _targetWorldPosition = position.WorldPosition;
            _targetPositionId = position.Id;
            _currentFloor = position.Floor;
            _isMoving = Vector3.Distance(transform.position, _targetWorldPosition) > 0.08f;
            _isManual = false;

            if (!_isMoving)
            {
                transform.position = _targetWorldPosition;
                _director.NotifyUnitArrived(UnitId, _targetPositionId);
            }
        }

        public void ManualMove(Vector3 worldDirection, float speed)
        {
            if (_isDown || worldDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            if (!_isManual)
            {
                _isManual = true;
                _isMoving = false;
                _targetPositionId = string.Empty;
                _director.NotifyUnitLeftPosition(UnitId);
            }

            _lastManualInputTime = Time.unscaledTime;
            Vector3 flatDirection = new Vector3(worldDirection.x, 0f, worldDirection.z).normalized;
            Vector3 next = transform.position + flatDirection * speed * Time.unscaledDeltaTime;
            next.x = Mathf.Clamp(next.x, -10.0f, 10.0f);
            next.z = Mathf.Clamp(next.z, -6.0f, 7.6f);
            next.y = transform.position.y;
            transform.position = next;

            if (flatDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(flatDirection, Vector3.up),
                    14f * Time.unscaledDeltaTime);
            }
        }

        public void SetDown(bool down)
        {
            _isDown = down;
            if (down)
            {
                _isMoving = false;
                _isManual = false;
            }

            UpdateVisualState();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisualState();
        }

        public string GetStatusLabel()
        {
            if (_isDown)
            {
                return "DOWN";
            }

            if (_isMoving || _isManual)
            {
                return "MOVING";
            }

            return "HOLDING";
        }

        private void Update()
        {
            if (_isDown)
            {
                UpdateLabelTransform();
                return;
            }

            if (_isMoving)
            {
                Vector3 remaining = _targetWorldPosition - transform.position;
                float distance = remaining.magnitude;
                if (distance <= 0.06f)
                {
                    transform.position = _targetWorldPosition;
                    _isMoving = false;
                    _director.NotifyUnitArrived(UnitId, _targetPositionId);
                }
                else
                {
                    Vector3 direction = remaining / distance;
                    float step = Mathf.Min(moveSpeed * Time.deltaTime, distance);
                    transform.position += direction * step;
                    Vector3 flat = new Vector3(direction.x, 0f, direction.z);
                    if (flat.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(flat.normalized, Vector3.up),
                            10f * Time.deltaTime);
                    }
                }
            }

            if (_isManual && Time.unscaledTime - _lastManualInputTime > 0.18f)
            {
                _isManual = false;
                _director.NotifyManualMovementEnded(this);
            }

            UpdateLabelTransform();
        }

        private void BuildVisuals()
        {
            GameObject visualRootObject = new GameObject("Character Visual");
            visualRootObject.transform.SetParent(transform, false);
            _characterVisualRoot = visualRootObject.transform;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(_characterVisualRoot, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0f);
            body.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null)
            {
                Destroy(bodyCollider);
            }

            _bodyRenderer = body.GetComponent<Renderer>();

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(_characterVisualRoot, false);
            head.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            head.transform.localScale = Vector3.one * 0.34f;
            Collider headCollider = head.GetComponent<Collider>();
            if (headCollider != null)
            {
                Destroy(headCollider);
            }

            _headRenderer = head.GetComponent<Renderer>();

            GameObject direction = TacticalPrototypeVisual.CreateBlock(
                "Facing marker",
                transform.position,
                new Vector3(0.10f, 0.10f, 0.65f),
                _displayColor,
                _characterVisualRoot,
                false);
            direction.transform.localPosition = new Vector3(0f, 0.22f, 0.55f);
            direction.transform.localRotation = Quaternion.identity;
            _directionRenderer = direction.GetComponent<Renderer>();

            GameObject selection = TacticalPrototypeVisual.CreateCylinder(
                "Selection ring",
                transform.position,
                new Vector3(0.68f, 0.025f, 0.68f),
                Color.white,
                transform,
                false);
            selection.transform.localPosition = new Vector3(0f, -0.92f, 0f);
            _selectionRenderer = selection.GetComponent<Renderer>();

            _label = TacticalPrototypeVisual.CreateWorldLabel(
                _plan.Callsign,
                transform.position + Vector3.up * 1.55f,
                0.055f,
                Color.white,
                transform);
            _label.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        }

        private void UpdateVisualState()
        {
            if (_bodyRenderer == null)
            {
                return;
            }

            Color activeColor = _isDown
                ? Color.Lerp(_displayColor, new Color(0.10f, 0.10f, 0.11f), 0.78f)
                : _displayColor;
            _bodyRenderer.sharedMaterial = TacticalPrototypeVisual.GetMaterial(activeColor, true);
            _headRenderer.sharedMaterial = TacticalPrototypeVisual.GetMaterial(activeColor * 0.88f, true);
            _directionRenderer.sharedMaterial = TacticalPrototypeVisual.GetMaterial(activeColor, true);

            Color ringColor;
            if (_isDown)
            {
                ringColor = new Color(0.75f, 0.10f, 0.12f);
            }
            else if (_isSelected)
            {
                ringColor = Color.white;
            }
            else
            {
                ringColor = new Color(_displayColor.r, _displayColor.g, _displayColor.b, 0.55f);
            }

            _selectionRenderer.sharedMaterial = TacticalPrototypeVisual.GetMaterial(ringColor, true);
            _selectionRenderer.gameObject.SetActive(_isSelected || _isDown);
            if (_characterVisualRoot != null)
            {
                _characterVisualRoot.localRotation = _isDown
                    ? Quaternion.Euler(0f, 0f, 78f)
                    : Quaternion.identity;
            }

            if (_label != null)
            {
                _label.color = _isDown ? new Color(1f, 0.25f, 0.28f) : Color.white;
                _label.text = _isDown ? _plan.Callsign + "  DOWN" : _plan.Callsign;
            }
        }

        private void UpdateLabelTransform()
        {
            if (_label == null)
            {
                return;
            }

            // The label is parented to the unit and already follows it. Keep the
            // vertical offset stable even while the body is rotated into a down pose.
            _label.transform.position = transform.position + Vector3.up * 1.55f;
        }
    }
}
