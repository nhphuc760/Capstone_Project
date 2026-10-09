using UnityEngine;

public class Wall : StructureBase
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Operation()
    {

    }

    public override void UpgradeLogic()
    {
     
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Wall collision with: " + collision.collider.name);
    }

}
