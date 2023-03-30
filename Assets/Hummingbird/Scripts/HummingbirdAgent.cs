using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// a hummingbird machine learning agent
/// </summary>
public class HummingbirdAgent : Agent
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

    //init the agent
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        //if not training, stop timer. run forever
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    //reset the agent when episode begins
    public override void OnEpisodeBegin()
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
    /// called when action receieved
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        //no action on frozen
        if (frozen)
        {
            return;
        }
        ActionSegment<float> vectorAction = actions.ContinuousActions;
        //actions.ContinuousActions
        //new movement
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        //addforce
        rigidbody.AddForce(move * moveForce);

        //Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        //Calculate pitch and yaw rotation
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        //Calculate smooth rotation changes
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        //Calculate new pitch and yaw based on smoothed values
        //Clamp pitch to avoid flipping upside down
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        //Apply the new roation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observation from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor </param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //Observe the agents local rotation(4 observations)
        sensor.AddObservation(transform.localRotation.normalized);
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        //Get a vector from the beak tip to the nearest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beaktip.position;

        //Observe a normalized vector pointing to the nearest flower(3 observations)
        sensor.AddObservation(toFlower.normalized);

        //Obsere a dot product that indicates whether the beak tip is in front of the flower
        //(+1 means that the beak tip is directly in front of the flower, -1 means directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        //Observe a dot product that indicates whether the beak is pointing toward the flower
        //(+1 means that the beak is pointing directly at the flower, -1 means directly away)
        sensor.AddObservation(Vector3.Dot(beaktip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        //Observe the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.areaDiameter);

        //10 total observations
    }

    /// <summary>
    /// When behavior type is set to "Heuristic Only" on the agent's behavior Parameters,
    /// this function will be called. Its return values will be fed into <cref="OnActionReceived(float[])"/> insted of using the neural network
    /// </summary>
    /// <param name="buffer">zAn output action array</param>
    public override void Heuristic(in ActionBuffers buffer)
    {
        //Create placeholders for all movement/turning
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;
        // Convert keyboard inputs to movement and turning
        //All values should be between -1 and +1
        //Forward/Backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        //Left/Right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        //Up down
        if (Input.GetKey(KeyCode.E)) up = -transform.up;
        else if (Input.GetKey(KeyCode.Q)) up = transform.up;

        //Pitch up/down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        //Turn left/right
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        //Combine the movement and normalize
        Vector3 combined = (forward + left + up).normalized;

        var actionsOut = buffer.ContinuousActions;

        //Add the 3 movement values, pitch and yaw to the actionsOut array
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
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

    /// <summary>
    /// update nearest flower to agent
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in  flowerArea.flowers)
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

                if (!nearestFlower.hasNectar ||distanceToFlower < distanceToCurrent)
                {
                    nearestFlower = flower;
                }
            }
        }
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
            attemptRemaining --;

            if (inFrontOfFlower)
            {
                //random flower
                Flower flower = flowerArea.flowers[Random.Range(0,flowerArea.flowers.Count)];

                //position 10 - 20 cm in front of flower
                float distanceFromFlower = Random.Range(.1f, .2f);
                potentialPosition = flower.transform.position + flower.FlowerUpVector * distanceFromFlower;

                //point beak at flower (head is center of transform
                Vector3 toflower = flower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toflower,Vector3.up);

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
    /// Called every frame
    /// </summary>
    private void Update()
    {
        //Draw a line from the beak tip to the nearest flower
        if (nearestFlower != null)
            Debug.DrawLine(beaktip.position, nearestFlower.FlowerCenterPosition, Color.green);
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
}
