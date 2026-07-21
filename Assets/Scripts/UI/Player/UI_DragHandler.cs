using TriInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UI_DragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{   
    [Required]
    [SerializeField] Transform characterTransform;
    [SerializeField] float rotationSpeed = 0.5f;
    [SerializeField] float smoothTime = .1f;
    float _targetRotationY;
    float _currentRotationVelocity;
    bool _isDraging;

    private void Start()
    {
        RotationLookToCamera();        
    }

    private void Update()
    {
        if (_isDraging)
        {
            _targetRotationY -= Mouse.current.delta.value.x * rotationSpeed;
        }
        float currentRotationY = characterTransform.eulerAngles.y;
        float nextRotationY = Mathf.SmoothDampAngle(currentRotationY, _targetRotationY, ref _currentRotationVelocity, smoothTime);
        characterTransform.rotation = Quaternion.Euler(0f, nextRotationY, 0f);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (characterTransform != null)
        {
            _isDraging = true;
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp");
        if (characterTransform != null)
        {
            _isDraging = false;
            ResetRotation();
        }
    }
    public void ResetRotation()
    {
        _targetRotationY = 0f;
    }


    void RotationLookToCamera()
    {        

        var cam = Camera.main;
        if (cam == null)
            return;
        float distanceThisToModel = Vector3.Distance(characterTransform.position, transform.position);
        Vector3 directionToCam = (cam.transform.position - transform.position).normalized;
        transform.position = characterTransform.position + directionToCam * distanceThisToModel;
       
        Vector3 RefDir = - directionToCam;
        RefDir.y = 0f;


        if (RefDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(RefDir);
        }
        _targetRotationY = characterTransform.eulerAngles.y;
    }
}
