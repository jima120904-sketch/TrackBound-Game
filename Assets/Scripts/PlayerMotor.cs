using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMotor : MonoBehaviour
{
    private CharacterController _controller;
    private Camera _camera;
    private TrainDrive _train;
    private float _pitch;
    private float _verticalSpeed;
    private float _coal = 6;
    private string _message = string.Empty;
    private float _messageUntil;

    public Camera ViewCamera => _camera;
    public float CoalChunks => _coal;
    public string Message => Time.time < _messageUntil ? _message : string.Empty;
    public string TargetHint { get; private set; }

    public void Initialize(TrainDrive train)
    {
        _train = train;
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _controller.height = 1.8f;
        _controller.radius = 0.34f;
        _controller.center = new Vector3(0, 0.9f, 0);
        _controller.stepOffset = 0.28f;
        _controller.slopeLimit = 48f;

        var cameraObject = new GameObject("First Person Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 1.62f, 0);
        _camera = cameraObject.AddComponent<Camera>();
        _camera.fieldOfView = 76f;
        _camera.nearClipPlane = 0.05f;
        cameraObject.AddComponent<AudioListener>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (_controller == null || _camera == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * 2.3f, Space.World);
            _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * 2.1f, -82f, 82f);
            _camera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1);
        var speed = Input.GetKey(KeyCode.LeftShift) ? 5.4f : 3.1f;
        var movement = (transform.right * input.x + transform.forward * input.y) * speed;

        if (_controller.isGrounded && _verticalSpeed < 0)
        {
            _verticalSpeed = -1.5f;
        }
        if (_controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            _verticalSpeed = 5.0f;
        }
        _verticalSpeed -= 15f * Time.deltaTime;
        movement.y = _verticalSpeed;
        _controller.Move(movement * Time.deltaTime);

        UpdateInteractionTarget();
        if (Input.GetKeyDown(KeyCode.E) && _camera != null && Physics.Raycast(_camera.transform.position, _camera.transform.forward, out var hit, 3.2f))
        {
            var interactable = hit.collider.GetComponentInParent<TrainInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
            }

            var coal = hit.collider.GetComponentInParent<CoalPickup>();
            if (coal != null)
            {
                _coal += coal.Chunks;
                SetMessage($"Picked up {coal.Chunks:0} coal chunks");
                Destroy(coal.gameObject);
            }
        }
    }

    public bool TryUseCoal()
    {
        if (_coal < 1)
        {
            return false;
        }

        _coal -= 1;
        return true;
    }

    public void SetMessage(string message)
    {
        _message = message;
        _messageUntil = Time.time + 2.5f;
    }

    private void UpdateInteractionTarget()
    {
        TargetHint = string.Empty;
        if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out var hit, 3.2f))
        {
            var interactable = hit.collider.GetComponentInParent<TrainInteractable>();
            if (interactable != null)
            {
                TargetHint = interactable.Hint;
                return;
            }

            if (hit.collider.GetComponentInParent<CoalPickup>() != null)
            {
                TargetHint = "E  Pick up coal";
            }
        }
    }
}

public sealed class CoalPickup : MonoBehaviour
{
    public float Chunks { get; set; } = 1;
}