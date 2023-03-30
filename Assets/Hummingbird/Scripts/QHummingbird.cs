using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QHummingbird : MonoBehaviour
{

    [Tooltip("force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("speed to pitch up and down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around up axis")]
    public float yawSpeed = 100f;

    [Tooltip("transform at the tip of the beak")]
    public Transform beaktip;

    [Tooltip("agent's camera")]
    public Camera agentCamera;

    [Tooltip("whether it is in training mode or playing mode")]
    public bool trainingMode;



    //rigidbody of agent
    new private Rigidbody rigidbody;

    //the flower the agent was in
    private FlowerArea flowerArea;

    //nearest flower to agent
    private Flower nearestFlower;

    //allow for smoother pitch changes
    private float smoothPitchChange = 0f;

    //allow for smoother yaw changes
    private float smoothYawChange = 0f;

    //maximum angle the bird can pitch up and down
    private float maxPitchAngle = 80f;


    //maximum of distance from the beak to accept nectar collision
    private float beakTipRadius = 0.008f;

    //wether the agent is frozen (intentionally not flying)
    private bool frozen = false;

    //amount of nectar the agent has obtained this episode
    public float nectarObtained { get; private set; }


    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        //if not training, stop timer. run forever

    }


    /// <summary>
    /// Prevent the agent from moving and taking actions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// Resume agent movement and actions
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }


    public void Restart()
    {
        if (trainingMode)
        {
            //resets flower only when one agent in area
            flowerArea.ResetFlowers();
        }

        //reset nectar obtained
        nectarObtained = 0f;

        //zero out velocities
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;


        //default to respawn in front of flower
        bool inFrontOfFlower = true;

        if (trainingMode)
        {
            //50% of chance to spawn in front of flower
            inFrontOfFlower = Random.value > 0.5f;
        }

        //move agent to a new random pos
        MoveToSafeRandomPosition(inFrontOfFlower);

        //recalculate the nearest flower
        UpdateNearestFlower();

    }


    /// <summary>
    /// move the agent to a safe random pos (i.e. not collide with anything
    /// if in frint of flower, point beak to flower
    /// </summary>
    /// <param name="inFrontOfFlower"> to check if to choose a spot in front of flower</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptRemaining = 100; //no infinite loop
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        //loop till found
        while (!safePositionFound && attemptRemaining > 0)
        {
            attemptRemaining--;

            if (inFrontOfFlower)
            {
                //random flower
                Flower flower = flowerArea.flowers[Random.Range(0, flowerArea.flowers.Count)];

                //position 10 - 20 cm in front of flower
                float distanceFromFlower = Random.Range(.1f, .2f);
                potentialPosition = flower.transform.position + flower.FlowerUpVector * distanceFromFlower;

                //point beak at flower (head is center of transform
                Vector3 toflower = flower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toflower, Vector3.up);

            }
            else
            {
                //random height above ground
                float height = Random.Range(1.2f, 2.5f);

                //pick random radius from center
                float radius = Random.Range(2f, 7f);

                //random rotation around Y axis
                Quaternion directtion = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);

                //combine
                potentialPosition = flowerArea.transform.position + Vector3.up * height + directtion * Vector3.forward * radius;

                //random pitch and yaw
                float pitch = Random.Range(-60f, 60f);
                float yaw = Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            //check if collides
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);


            //safe if no collider found
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "cannot find safe position!");

        //set
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }


    /// <summary>
    /// update nearest flower to agent
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.flowers)
        {
            if (nearestFlower == null && flower.hasNectar)
            {
                nearestFlower = flower;
            }
            else if (flower.hasNectar)
            {
                //distance comparison
                float distanceToFlower = Vector3.Distance(flower.transform.position, beaktip.position);
                float distanceToCurrent = Vector3.Distance(nearestFlower.transform.position, beaktip.position);

                if (!nearestFlower.hasNectar || distanceToFlower < distanceToCurrent)
                {
                    nearestFlower = flower;
                }
            }
        }
    }


    /// <summary>
    /// Called when the agent's collider enter a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Handles when the agents collider enters of stays in a trigger collider
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        //Check if agent is colliding with nectar
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beaktip.position);

            //Check if the closest collision point is close to the beak tip
            //Note : a collision with anything but the beak tip should not count
            if (Vector3.Distance(beaktip.position, closestPointToBeakTip) < beakTipRadius)
            {
                //Look up the flower for this nectar collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                //Attempt to take .01 nectar
                //Note : this is per fixed timestep, meaning it happens 50 times a sec
                float nectarReceived = flower.Feed(.01f);

                //Keep track of nectar obtained
                nectarObtained += nectarReceived;
                if (trainingMode)
                {
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }
                //If flower is empty update the nearest flower
                if (!flower.hasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="collision">The colision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            //Collided with the area boundary, give a negative reward
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Called every .02 seconds
    /// </summary>
    private void FixedUpdate()
    {
        //Avoids scenario where nearest flower nectar is stolen by opponent and not updated
        if ((nearestFlower != null && !nearestFlower.hasNectar) || nearestFlower == null)
        {
            UpdateNearestFlower();
        }
    }

    private void Update()
    {
        //Draw a line from the beak tip to the nearest flower
        if (nearestFlower != null)
            Debug.DrawLine(beaktip.position, nearestFlower.FlowerCenterPosition, Color.green);
    }


}
