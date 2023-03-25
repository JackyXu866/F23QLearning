using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    Rigidbody rb;

    public float speed = 10f;

    public float angularSpeed = 200f;


    public float acceleration = 6f;

    [Range(0f, 1f)]
    public float deceleration = 0.2f;

  

    public bool heturistics = true;

    public BoxCollider grounder;
    bool grounded  = false;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (heturistics)
        {
            CarAccelerate(Input.GetAxis("Vertical"));
            CarTurning(Input.GetAxis("Horizontal"));
        }

    }


    public void CarAccelerate(float rate)
    {
        if (grounded)
        {
            rb.velocity = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward)) * rb.velocity.magnitude * transform.forward;
            rb.AddForce(acceleration * rate * transform.forward * rb.mass, ForceMode.Force);
            //rb.AddForceAtPosition(acceleration * rate * transform.forward * rb.mass, transform.TransformPoint(new Vector3(0, 0.3f, -1.2f)));

            if (rb.velocity.magnitude > speed)
            {
                rb.velocity = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward)) * speed * transform.forward;

            }
            else if (Mathf.Abs(rate) < Mathf.Epsilon)
            {
                //rb.velocity = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward)) * rb.velocity.magnitude * transform.forward;
                rb.AddForce(rb.velocity.magnitude / speed * acceleration * deceleration * (-Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward))) * transform.forward * rb.mass, ForceMode.Force);
                
            }

        }
    }

    public void CarTurning(float rate)
    {
        if (Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) > float.Epsilon && grounded)
        {
            rb.angularVelocity = transform.up * rate * angularSpeed;
            
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        grounded = true;
    }

    private void OnTriggerExit(Collider other)
    {
        grounded = false;
    }
}
