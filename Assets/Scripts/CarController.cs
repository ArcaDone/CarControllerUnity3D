using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{

    [Header("Inputs")]

    // If isPlayer is false inputs are ignored
    [SerializeField] bool isPlayer = true;
    public bool IsPlayer { get { return isPlayer; } set { isPlayer = value; } }

    // Input names to read using GetAxis
    [SerializeField] string throttleInput = "Throttle";
    [SerializeField] string brakeInput = "Brake";
    [SerializeField] string turnInput = "Horizontal";
    [SerializeField] string jumpInput = "Jump";
    [SerializeField] string boostInput = "Boost";
    [SerializeField] float MaxSpeed = 70f;
    [SerializeField] float _stabilizerMotorTorque = 10f;
    [SerializeField] float attractionSpeed = 5f;
    [SerializeField] Transform RayCastTransf;

    [Header("Wheels")]
    [SerializeField] WheelCollider[] driveWheel;
    public WheelCollider[] DriveWheel { get { return driveWheel; } }
    [SerializeField] WheelCollider[] turnWheel;

    public WheelCollider[] TurnWheel { get { return turnWheel; } }

    // This code checks if the car is grounded only when needed and the data is old enough
    bool isGrounded = false;
    int lastGroundCheck = 0;
    public bool IsGrounded
    {
        get
        {
            if (lastGroundCheck == Time.frameCount)
                return isGrounded;

            lastGroundCheck = Time.frameCount;
            isGrounded = true;
            foreach (WheelCollider wheel in wheels)
            {
                if (!wheel.gameObject.activeSelf || !wheel.isGrounded)
                    isGrounded = false;
            }
            return isGrounded;
        }
    }

    [Header("Behaviour")]
    /*
     *  Motor torque represent the torque sent to the wheels by the motor with x: speed in km/h and y: torque
     *  The curve should start at x=0 and y>0 and should end with x>topspeed and y<0
     *  The higher the torque the faster it accelerate
     *  the longer the curve the faster it gets
     */
    [SerializeField] AnimationCurve motorTorque = new AnimationCurve(new Keyframe(0, 200), new Keyframe(50, 300), new Keyframe(200, 0));

    // Differential gearing ratio
    [Range(2, 16)]
    [SerializeField] float diffGearing = 4.0f;
    public float DiffGearing { get { return diffGearing; } set { diffGearing = value; } }

    // Basicaly how hard it brakes
    [SerializeField] float brakeForce = 1500.0f;
    public float BrakeForce { get { return brakeForce; } set { brakeForce = value; } }

    // Max steering hangle, usualy higher for drift car
    [Range(0f, 50.0f)]
    [SerializeField] float steerAngle = 30.0f;
    public float SteerAngle { get { return steerAngle; } set { steerAngle = Mathf.Clamp(value, 0.0f, 50.0f); } }

    // The value used in the steering Lerp, 1 is instant (Strong power steering), and 0 is not turning at all
    [Range(0.001f, 1.0f)]
    [SerializeField] float steerSpeed = 0.2f;
    public float SteerSpeed { get { return steerSpeed; } set { steerSpeed = Mathf.Clamp(value, 0.001f, 1.0f); } }

    // How hight do you want to jump?
    [Range(1f, 2.5f)]
    [SerializeField] float jumpVel = 1.3f;
    public float JumpVel { get { return jumpVel; } set { jumpVel = Mathf.Clamp(value, 1.0f, 2.5f); } }

    // How hard do you want to drift?
    [Range(0.0f, 2f)]
    [SerializeField] float driftIntensity = 1f;
    public float DriftIntensity { get { return driftIntensity; } set { driftIntensity = Mathf.Clamp(value, 0.0f, 2.0f); } }

    public float minSpeedToDrift = 50;
    // Reset Values
    Vector3 spawnPosition;
    Quaternion spawnRotation;

    /*
     *  The center of mass is set at the start and changes the car behavior A LOT
     *  I recomment having it between the center of the wheels and the bottom of the car's body
     *  Move it a bit to the from or bottom according to where the engine is
     */
    [SerializeField] Transform centerOfMass;

    // Force aplied downwards on the car, proportional to the car speed
    [Range(0.5f, 10f)]
    [SerializeField] float downforce = 1.0f;
    public float Downforce { get { return downforce; } set { downforce = Mathf.Clamp(value, 0, 5); } }

    // When IsPlayer is false you can use this to control the steering
    float steering;
    public float Steering { get { return steering; } set { steering = Mathf.Clamp(value, -1f, 1f); } }

    // When IsPlayer is false you can use this to control the throttle
    float throttle;
    public float Throttle { get { return throttle; } set { throttle = Mathf.Clamp(value, -1f, 1f); } }

    // Like your own car handbrake, if it's true the car will not move
    [SerializeField] bool handbrake;
    public bool Handbrake { get { return handbrake; } set { handbrake = value; } }

    // Use this to disable drifting
    public bool allowDrift = false;
    bool drift;
    public bool Drift { get { return drift; } set { drift = value; } }

    // Use this to read the current car speed (you'll need this to make a speedometer)
    [SerializeField] public float speed = 0.0f;
    public float Speed { get { return speed; } }

    [Header("Particles")]
    // Exhaust fumes
    [SerializeField] ParticleSystem[] gasParticles;

    [Header("Boost")]
    // Disable boost
    [HideInInspector] public bool allowBoost = true;

    // Maximum boost available
    [SerializeField] float maxBoost = 10f;
    public float MaxBoost { get { return maxBoost; } set { maxBoost = value; } }

    // Current boost available
    [SerializeField] float boost = 10f;
    public float Boost { get { return boost; } set { boost = Mathf.Clamp(value, 0f, maxBoost); } }

    [Range(0f, 1f)]
    [SerializeField] float boostRegen = 0.2f;
    public float BoostRegen { get { return boostRegen; } set { boostRegen = Mathf.Clamp01(value); } }

    [SerializeField] float boostForce = 5000;
    public float BoostForce { get { return boostForce; } set { boostForce = value; } }

    // Use this to boost when IsPlayer is set to false
    public bool boosting = false;

    public bool consumeBoost = false;
    // Use this to jump when IsPlayer is set to false
    public bool jumping = false;

    // Boost particles and sound
    [SerializeField] ParticleSystem[] boostParticles;
    [SerializeField] AudioClip boostClip;
    [SerializeField] AudioSource boostSource;

    //puffonground
    [SerializeField] ParticleSystem smokePuffJump;
    private bool instantiatedSMokePuff = false;

    // Private variables set at the start
    Rigidbody _rb;
    WheelCollider[] wheels;

    bool inReset = false;
    Vector3 spawnP;
    Quaternion spawnR;



    // Init rigidbody, center of mass, wheels and more
    void Start()
    {
        if (boostClip != null)
        {
            boostSource.clip = boostClip;
        }

        boost = maxBoost;

        _rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (_rb != null && centerOfMass != null)
        {
            _rb.centerOfMass = centerOfMass.localPosition;
        }

        wheels = GetComponentsInChildren<WheelCollider>();

        // Set the motor torque to a non null value because 0 means the wheels won't turn no matter what
        foreach (WheelCollider wheel in wheels)
        {
            wheel.motorTorque = 0.0001f;
        }
        spawnP = transform.position;
        spawnR = transform.rotation;
    }

    // Visual feedbacks and boost regen
    void Update()
    {

        PoofEffect();
        foreach (ParticleSystem gasParticle in gasParticles)
        {
            gasParticle.Play();
            ParticleSystem.EmissionModule em = gasParticle.emission;
            em.rateOverTime = handbrake ? 0 : Mathf.Lerp(em.rateOverTime.constant, Mathf.Clamp(150.0f * throttle, 30.0f, 100.0f), 0.1f);
        }

        if (isPlayer && allowBoost)
        {
            boost += Time.deltaTime * boostRegen;
            if (boost > maxBoost) { boost = maxBoost; }
        }

        // Reset to spawn if out of bounds
        if (transform.position.y < -10)
        {
            transform.position = spawnP;
            transform.rotation = spawnR;
            inReset = true;
        }
    }

    // Update everything
    void FixedUpdate()
    {
        // Mesure current speed
        speed = transform.InverseTransformDirection(_rb.velocity).z * 3.6f;

        // Get all the inputs!
        if (isPlayer)
        {
            // Accelerate & brake
            if (throttleInput != "" && throttleInput != null)
            {
                throttle = GetInput(throttleInput) - GetInput(brakeInput);
            }
            // Boost
            boosting = (GetInput(boostInput) > 0.5f);

            // Turn
            steering = GetInput(turnInput) * steerAngle;
            

            // Dirft disabled, I'm using mine 11 Marzo
            /*drift = GetInput(driftInput) > 0 && _rb.velocity.sqrMagnitude > 30;*/
            drift = speed > minSpeedToDrift;

            // Jump
            jumping = GetInput(jumpInput) != 0;

        }

        // Direction
        foreach (WheelCollider wheel in turnWheel)
        {
            //wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, steering, steerSpeed);
            //versione di prova
            wheel.steerAngle = steering;
        }

        foreach (WheelCollider wheel in wheels)
        {
            wheel.brakeTorque = 0;
        }

        // Handbrake
        if (handbrake)
        {
            foreach (WheelCollider wheel in wheels)
            {
                // Don't zero out this value or the wheel completly lock up
                wheel.motorTorque = 0.0001f;
                wheel.brakeTorque = brakeForce;
            }
        }
        else if (Mathf.Abs(speed) < 4 || Mathf.Sign(speed) == Mathf.Sign(throttle))
        {
            foreach (WheelCollider wheel in driveWheel)
            {
                //Limit max speed
                if (speed <= MaxSpeed)
                {
                    wheel.motorTorque = throttle * motorTorque.Evaluate(speed) * diffGearing / driveWheel.Length;
                }
                else
                {
                    wheel.motorTorque = 0f;
                }
            }

        }
        else
        {
            foreach (WheelCollider wheel in wheels)
            {
                wheel.brakeTorque = Mathf.Abs(throttle) * brakeForce;
            }
        }

        // Jump
        if (jumping && isPlayer)
        {
            if (!IsGrounded)
                return;

            _rb.velocity += transform.up * jumpVel;
        }

        // Boost
        if ( (boosting || consumeBoost) && allowBoost && boost > 0.1f)
        {
            _rb.AddForce(transform.forward * boostForce);

            boost -= Time.fixedDeltaTime;
            if (boost < 0f) { boost = 0f; }

            if (boostParticles.Length > 0 && !boostParticles[0].isPlaying)
            {
                foreach (ParticleSystem boostParticle in boostParticles)
                {
                    boostParticle.Play();
                    boostParticle.gameObject.SetActive(true);
                }
            }

            if (boostSource != null && !boostSource.isPlaying)
            {
                boostSource.Play();
            }
        }
        else
        {
            if (boostParticles.Length > 0 && boostParticles[0].isPlaying)
            {
                foreach (ParticleSystem boostParticle in boostParticles)
                {
                    boostParticle.Stop();
                    boostParticle.gameObject.SetActive(false);
                }
            }

            if (boostSource != null && boostSource.isPlaying)
            {
                boostSource.Stop();
            }
        }

        // Drift
        if (drift && allowDrift)
        {
            Vector3 driftForce = -transform.right;
            driftForce.y = 0.0f;
            driftForce.Normalize();

            if (steering != 0)
                driftForce *= _rb.mass * speed / 7f * throttle * steering / steerAngle;
            Vector3 driftTorque = transform.up * 0.1f * steering / steerAngle;


            _rb.AddForce(driftForce * driftIntensity, ForceMode.Force);
            _rb.AddTorque(driftTorque * driftIntensity, ForceMode.Acceleration); //era ForceMode.VelocityChange
        }

        // Reset will turn false after the respawn is successful
        inReset = inReset || Input.GetKeyDown(KeyCode.R);

        if (inReset)
        {  // Reset
            float y = transform.eulerAngles.y;
            transform.eulerAngles = new Vector3(0, y, 0);
            _rb.velocity = new Vector3(0, -1f, 0);
            transform.position += Vector3.up * 2;
            inReset = false;
        }
        // Downforce
        _rb.AddForce(-transform.up * speed * downforce);

        AttractorCheck();

    }

    void PoofEffect()
    {
        if (instantiatedSMokePuff && !IsGrounded)
        {
            instantiatedSMokePuff = false;
        }

        if (IsGrounded && !instantiatedSMokePuff)
        {
            if (smokePuffJump != null)
            {
                Instantiate(smokePuffJump, gameObject.transform.position, gameObject.transform.rotation);
            }
            instantiatedSMokePuff = true;
        }

    }

    void Stabilizer(Vector3 surface)
    {
        Rigidbody ms_Rigidbody = gameObject.GetComponent<Rigidbody>();
        Vector3 axisFromRotate = Vector3.Cross(transform.up, surface);
        Vector3 torqueForce = axisFromRotate.normalized * axisFromRotate.magnitude * _stabilizerMotorTorque;
        torqueForce.x = torqueForce.x * 0.4f;
        torqueForce -= ms_Rigidbody.angularVelocity;

        ms_Rigidbody.AddTorque((torqueForce * 2 / 3 ) * ms_Rigidbody.mass * 0.02f, ForceMode.Impulse);
    }



    // Reposition the car to the start position
    public void ResetPos()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    public void toogleHandbrake(bool h)
    {
        handbrake = h;
    }

    // Use this method if you want to use your own input manager
    private float GetInput(string input)
    {
        return Input.GetAxis(input);
    }

    void AttractorCheck()
    {
        int layerMask = 1 << 8;

        layerMask = ~layerMask;
        RaycastHit hit;
        if (Physics.Raycast(RayCastTransf.position, transform.TransformDirection(Vector3.down), out hit, 5, layerMask) && hit.transform.CompareTag("AttractionLayer"))
        {
            Debug.DrawRay(RayCastTransf.position, transform.TransformDirection(Vector3.down) * hit.distance, Color.yellow);
            _rb.useGravity = false;
            ApplyStabilizer(hit.normal);
            _rb.AddRelativeForce(Vector3.down * 10000 * attractionSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Debug.DrawRay(RayCastTransf.position, transform.TransformDirection(Vector3.down) * 5, Color.white);
            _rb.useGravity = true;
            ApplyStabilizer(Vector3.up);
        }
    }

    void ApplyStabilizer(Vector3 vectorToStabilize)
    {

        Stabilizer(vectorToStabilize);
    }
}