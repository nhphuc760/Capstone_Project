using Cinemachine;
using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    ICinemachineCamera camA;
    ICinemachineCamera camB;
    [SerializeField]
    CinemachineBrain Brain;
    private void Start()
    {
        camA = GetComponent<CinemachineVirtualCamera>();
        camB = GetComponent<CinemachineVirtualCamera>();
        float weight = 1f;
        float blendTime = 0f;
        Brain.SetCameraOverride(-1, camA, camB, weight, blendTime);
    }
}
