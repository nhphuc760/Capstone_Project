using UnityEngine;

public class SyncPhysicsObject : MonoBehaviour
{
    Rigidbody rigidbody3D;
    ConfigurableJoint joint;
    [SerializeField]
    Rigidbody animatedRigidbody;
    [SerializeField]
    bool syncAnimation = false;
    Quaternion startLocalRotation;

    private void Awake()
    {
        rigidbody3D = GetComponent<Rigidbody>();
        joint = GetComponent<ConfigurableJoint>();
        startLocalRotation = transform.localRotation;
    }
    public void UpdateJointFromAnimation() 
    {
        if (!syncAnimation)
            return;
        ConfigurableJointExtensions.SetTargetRotationLocal(joint, animatedRigidbody.transform.localRotation, startLocalRotation);
    }

}
