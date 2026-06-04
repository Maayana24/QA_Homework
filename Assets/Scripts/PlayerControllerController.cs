using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerController : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] float speed;
    [SerializeField] CapsuleCollider coll;
    Vector2 movement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.y);
    }

    private void OnMove(InputValue value)
    {
        movement = value.Get<Vector2>().normalized * speed;
    }
    private void OnInteract(InputValue value)
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, coll.height/2);
        foreach (Collider overlappingColl in overlaps)
        {
            if(overlappingColl.tag == "Torch")
            {
                overlappingColl.GetComponent<Torch>().FlameOn();
            }
        }
    }
}
