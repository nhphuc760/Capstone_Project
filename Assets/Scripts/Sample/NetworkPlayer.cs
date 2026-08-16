using UnityEngine;
using Fusion;
using Cinemachine;
using Fusion.Addons.Physics;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{

    [SerializeField]
    NetworkRigidbody3D networkRigidbody;
    [SerializeField]
    Rigidbody rigidbody3D;
    [SerializeField]
    ConfigurableJoint mainJoint;
    [SerializeField] float maxSpeed = 3;
    bool isGrounded = false;
    CinemachineVirtualCamera cinemachineVirtual;

    [SerializeField]
    Animator animator;

    RaycastHit[] hits = new RaycastHit[10];

    SyncPhysicsObject[] syncPhysicObjects;
    NetworkInputData dataInput = new NetworkInputData();
    public static NetworkPlayer Local { get; private set; }

    private void Awake()
    {
        syncPhysicObjects = GetComponentsInChildren<SyncPhysicsObject>();
        //rigidbody3D.MoveRotation();
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 localVelocityVsForwards = Vector3.zero;
        float localForwardVelocity = 0;
        if (Object.HasStateAuthority)
        {
            isGrounded = false;
            int numberHits = Physics.SphereCastNonAlloc(rigidbody3D.position, .1f, -transform.up, hits, maxDistance: .1f);
            for (int i = 0; i < numberHits; i++)
            {
                if (hits[i].transform.root == transform)
                    continue;
                isGrounded = true;
                break;
            }
            if (!isGrounded)
            {
                rigidbody3D.AddForce(Vector3.down * 10f);
            }
            localVelocityVsForwards = transform.forward * Vector3.Dot(transform.forward, rigidbody3D.linearVelocity);
            localForwardVelocity = localVelocityVsForwards.magnitude;
        }

        if (GetInput(out NetworkInputData inputNetwork))
        {
            float inputMagnitude = inputNetwork.moveInput.magnitude;

            if (inputMagnitude != 0)
            {
                Vector3 dir = new Vector3(inputNetwork.moveInput.x, 0f, -inputNetwork.moveInput.y);
                Quaternion desiredDirection = Quaternion.LookRotation(dir, transform.up);
                mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, desiredDirection, Runner.DeltaTime * 300);

                if (localForwardVelocity < maxSpeed)
                {
                    rigidbody3D.AddForce(transform.forward * inputMagnitude * 30);
                }

            }
            if (isGrounded && inputNetwork.isJumpressed)
            {
                rigidbody3D.AddForce(Vector3.up * 5, ForceMode.Impulse);
            }
        }
        if (Object.HasStateAuthority)
        {
            animator.SetFloat("moveSpeed", localForwardVelocity * .4f);
            for (int i = 0; i < syncPhysicObjects.Length; i++)
            {
                syncPhysicObjects[i].UpdateJointFromAnimation();
            }
            if (transform.position.y < -10f)
            {
                networkRigidbody.Teleport(Vector3.zero, Quaternion.identity);
            }
        }       
    }


    public NetworkInputData GetInput()
    {
        dataInput.moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        dataInput.isJumpressed = Input.GetKey(KeyCode.Space);
        return dataInput;
    }

    public override void Spawned()
    {

        var interpolated = new NetworkBehaviourBufferInterpolator(this);
        if (Object.HasInputAuthority)
        {
            Local = this;
            cinemachineVirtual = FindAnyObjectByType<CinemachineVirtualCamera>();
            cinemachineVirtual.m_Follow = transform;
            cinemachineVirtual.m_LookAt = transform;
            transform.name = $"{Object.Id}";
        }
    }
    public void PlayerLeft(PlayerRef player)
    {
        throw new System.NotImplementedException();
    }
}
