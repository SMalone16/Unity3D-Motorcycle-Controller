using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MotorcycleController : MonoBehaviour
{
    [SerializeField] GameManager gameManager;

    ///////////////////////
    Rigidbody motorcycleRigidbody;

    ///////////////////////
    [Header("MOTORCYCLE VALUES")]
    [SerializeField] WheelCollider frontWheelCollider;
    [SerializeField] WheelCollider rearWheelCollider;
    [SerializeField] Transform[] SteeringPiecesTransforms;
    [SerializeField] float movePower;
    [SerializeField] float brakePower;
    [SerializeField] float inNeutralBrakePower;
    [SerializeField] float maxSteerRotateAngle;
    [SerializeField] Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
    [SerializeField] float forwardFrictionStiffness = 1.8f;
    [SerializeField] float sidewaysFrictionStiffness = 2.1f;
    [SerializeField] float suspensionSpring = 35000f;
    [SerializeField] float suspensionDamper = 4500f;
    [SerializeField] float minSteerMultiplier = 0.35f;
    [SerializeField] float lowSpeedSteerReductionThreshold = 6f;
    [SerializeField] float fullSteerSpeedThreshold = 24f;
    [SerializeField] float slopeSteerReductionStartAngle = 10f;
    [SerializeField] float slopeSteerReductionMaxAngle = 35f;
    [SerializeField] float riderLeanTorque = 170f;
    //
    float currentSteerRotateAngle;
    bool isBraking;
    float currentBrakePower;

    ///////////////////////
    [Header("SPEED TEXT")]
    [SerializeField] TMP_Text speedText;
    int speed = 0;

    ///////////////////////
    [Header("TILTING")]
    [SerializeField] float tiltingSpeed;
    float speedDetector;
    float slideValue;
    bool canMoveToFront = true;
    bool isStalled;
    bool isSpinout;

    ///////////////////////
    [Header("BIKER")]
    [SerializeField] GameObject bikerParentGameObject;
    [SerializeField] float followArmSpeed;
    [SerializeField] Transform rightArmTransform;
    [SerializeField] Transform leftArmTransform;

    ///////////////////////
    bool isMovingSoundPlaying = false;
    bool isInNeutralSoundPlaying = false;
    bool isbrakingSoundPlaying = false;

    Vector3 startPosition;
    Quaternion startRotation;
    Vector3 bikerStartPosition;
    Quaternion bikerStartRotation;
    Quaternion rightArmStartLocalRotation;
    Quaternion leftArmStartLocalRotation;

    void Start()
    {
        motorcycleRigidbody = GetComponent<Rigidbody>();
        ResolveWheelColliders();
        motorcycleRigidbody.centerOfMass = centerOfMassOffset;
        ApplyWheelSetup(frontWheelCollider);
        ApplyWheelSetup(rearWheelCollider);

        startPosition = transform.position;
        startRotation = transform.rotation;
        bikerStartPosition = bikerParentGameObject.transform.position;
        bikerStartRotation = bikerParentGameObject.transform.rotation;
        rightArmStartLocalRotation = rightArmTransform.localRotation;
        leftArmStartLocalRotation = leftArmTransform.localRotation;
        //
        BikerRigidbodiesKinematicSituation(true);
        BikerCollidersEnabledSituation(false);
    }

    void Update()
    {
        DisplayMotorcycleSpeedText();
    }

    void FixedUpdate()
    {
        VerticalMove();
        HorizontalMove();
        ApplyRiderLeanControl();
        CheckPutBrake();
        FollowAllSteeringPieceToWheelRotation();
        TiltingToMotorcycle();
    }

    ///////////////////////
    //Movement
    void VerticalMove()
    {
        if (!CanRide())
            return;

        float verticalInput = Input.GetAxis("Vertical");
        WheelCollider driveWheelCollider = rearWheelCollider != null ? rearWheelCollider : frontWheelCollider;
        if (driveWheelCollider != null)
            driveWheelCollider.motorTorque = verticalInput * movePower;
        //
        if(verticalInput > 0 && canMoveToFront)
        {
            SetMotorcycleMovingSound();
            MakeSlideOnMotorcycle();
            isInNeutralSoundPlaying = false;
        }
        else if (verticalInput == 0)
        {
            SetMotorcycleInNeutralSound();
            ApplyBrakeTorqueToAllWheels(inNeutralBrakePower);
            isMovingSoundPlaying = false;
        }
        else
        {
            SetMotorcycleInNeutralSound();
            ApplyBrakeTorqueToAllWheels(0f);
            isMovingSoundPlaying = false;
        }
    }

    void HorizontalMove()
    {
        if (!CanRide())
            return;

        if (frontWheelCollider == null)
            return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float steerMultiplier = CalculateSteerMultiplier();
        currentSteerRotateAngle = maxSteerRotateAngle * steerMultiplier * horizontalInput;
        frontWheelCollider.steerAngle = currentSteerRotateAngle;
        //
        if (horizontalInput > 0.3f)
            FollowBikerArmsToSteeringWhenTurnRight();
        else if (horizontalInput < -0.3f)
            FollowBikerArmsToSteeringWhenTurnLeft();
        else
            FollowBikerArmsToSteeringWhenNoTurn();
    }

    void ApplyRiderLeanControl()
    {
        if (!CanRide())
            return;

        float leanInput = Input.GetAxis("Vertical");
        if (Mathf.Abs(leanInput) < 0.01f)
            return;

        motorcycleRigidbody.AddTorque(transform.right * leanInput * riderLeanTorque, ForceMode.Acceleration);
    }

    ///////////////////////
    // Tilting To Motorcycle
    void TiltingToMotorcycle()
    {
        if (frontWheelCollider == null)
            return;

        float zPosition = frontWheelCollider.steerAngle;
        zPosition = Mathf.Clamp(zPosition, -25, 25);
        //
        Quaternion newMotorcycleRotation = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, -zPosition));
        transform.rotation = Quaternion.Lerp(transform.rotation, newMotorcycleRotation, tiltingSpeed * Time.fixedDeltaTime);
    }

    ///////////////////////
    // Control Put On The Brake
    void CheckPutBrake()
    {
        if (!CanRide())
            return;

        isBraking = Input.GetKey(KeyCode.Space);
        currentBrakePower = isBraking ? brakePower : 0f;
        ApplyBrakeTorqueToAllWheels(currentBrakePower);
        if (isBraking)
            SetMotorcycleBrakingSound();
        else
            isbrakingSoundPlaying = false;
    }

    ///////////////////////
    // Slide
    void MakeSlideOnMotorcycle()
    {
        speedDetector = motorcycleRigidbody.velocity.sqrMagnitude;
        //
        slideValue = Vector3.Dot(motorcycleRigidbody.velocity.normalized, transform.forward);
        if (slideValue > 0 && slideValue < 0.7f && speedDetector>20f)
        {
            if (isStalled || isSpinout)
                motorcycleRigidbody.velocity = Vector3.zero;

            ApplyBrakeTorqueToAllWheels(brakePower * 0.6f);
            Vector3 forwardVelocity = Vector3.Project(motorcycleRigidbody.velocity, transform.forward);
            motorcycleRigidbody.velocity = Vector3.Lerp(motorcycleRigidbody.velocity, forwardVelocity, 0.2f);
            SetMotorcycleBrakingSound();
            canMoveToFront = false;
            speedDetector = 0;
            StartCoroutine(ResetCanMoveToFrontBoolean());
        }
    }

    IEnumerator ResetCanMoveToFrontBoolean()
    {
        yield return new WaitForSeconds(2f);
        canMoveToFront = true;
    }

    ///////////////////////
    // Follow Components To Each Other
    void FollowAllSteeringPieceToWheelRotation()
    {
        if (frontWheelCollider == null)
            return;

        foreach (Transform piece in SteeringPiecesTransforms)
        {
            piece.localEulerAngles = new Vector3(piece.localEulerAngles.x, frontWheelCollider.steerAngle, piece.localEulerAngles.z);
        }
    }

    void FollowBikerArmsToSteeringWhenTurnRight()
    {
        Quaternion newRightArmRotation = Quaternion.Euler(new Vector3(138f, 91f, 60f));
        rightArmTransform.localRotation = Quaternion.Lerp(rightArmTransform.localRotation, newRightArmRotation, followArmSpeed * Time.fixedDeltaTime);
        //
        Quaternion newLeftArmRotation = Quaternion.Euler(new Vector3(90f, -51f, 0f));
        leftArmTransform.localRotation = Quaternion.Lerp(leftArmTransform.localRotation, newLeftArmRotation, followArmSpeed * Time.fixedDeltaTime);
    }

    void FollowBikerArmsToSteeringWhenTurnLeft()
    {
        Quaternion newRightArmRotation = Quaternion.Euler(new Vector3(78f, 91f, 60f));
        rightArmTransform.localRotation = Quaternion.Lerp(rightArmTransform.localRotation, newRightArmRotation, followArmSpeed * Time.fixedDeltaTime);
        //
        Quaternion newLeftArmRotation = Quaternion.Euler(new Vector3(106f, -51f, 0f));
        leftArmTransform.localRotation = Quaternion.Lerp(leftArmTransform.localRotation, newLeftArmRotation, followArmSpeed * Time.fixedDeltaTime);
    }

    void FollowBikerArmsToSteeringWhenNoTurn()
    {
        Quaternion newRightArmRotation = Quaternion.Euler(new Vector3(103f, 91f, 60f));
        rightArmTransform.localRotation = Quaternion.Lerp(rightArmTransform.localRotation, newRightArmRotation, followArmSpeed * Time.fixedDeltaTime);
        //
        Quaternion newLeftArmRotation = Quaternion.Euler(new Vector3(96f, -51f, 0f));
        leftArmTransform.localRotation = Quaternion.Lerp(leftArmTransform.localRotation, newLeftArmRotation, followArmSpeed * Time.fixedDeltaTime);
    }

    ///////////////////////
    // Display Motorcycle Speed
    void DisplayMotorcycleSpeedText()
    {
        speed = Mathf.RoundToInt(motorcycleRigidbody.velocity.magnitude * 3.6f);
        speedText.text = speed.ToString();
    }

    ///////////////////////
    // Sounds
    void SetMotorcycleMovingSound()
    {
        if (!isMovingSoundPlaying)
        {
            AudioManager.Instance.StopMotorcycleEngineSound();
            AudioManager.Instance.PlayMotorcycleSpeedUpSound();
            isMovingSoundPlaying = true;
        }
    }

    void SetMotorcycleInNeutralSound()
    {
        if (!isInNeutralSoundPlaying)
        {
            AudioManager.Instance.StopMotorcycleSpeedUpSound();
            AudioManager.Instance.PlayMotorcycleEngineSound();
            isInNeutralSoundPlaying = true;
        }
    }

    void SetMotorcycleBrakingSound()
    {
        if (!isbrakingSoundPlaying)
        {
            AudioManager.Instance.StopMotorcycleEngineSound();
            AudioManager.Instance.StopMotorcycleSpeedUpSound();
            AudioManager.Instance.PlayMotorcycleBrakingSound();
            isbrakingSoundPlaying = true;
            isMovingSoundPlaying = false;
        }
    }
    ///////////////////////
    // Ragdoll
    void BikerRigidbodiesKinematicSituation(bool isRigidbodiesKinematicActive)
    {
        Rigidbody[] bikerRigidbodies;
        bikerRigidbodies = bikerParentGameObject.GetComponentsInChildren<Rigidbody>();
        Array.ForEach(bikerRigidbodies, rb => rb.isKinematic = isRigidbodiesKinematicActive);
    }

    void ResetBikerRigidbodiesVelocity()
    {
        Rigidbody[] bikerRigidbodies;
        bikerRigidbodies = bikerParentGameObject.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in bikerRigidbodies)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ApplyRiderEjectionForce()
    {
        Rigidbody[] bikerRigidbodies;
        bikerRigidbodies = bikerParentGameObject.GetComponentsInChildren<Rigidbody>();
        if (bikerRigidbodies.Length > 0)
            bikerRigidbodies[0].AddForce(transform.forward, ForceMode.Impulse);
    }

    void BikerCollidersEnabledSituation(bool isCollidersActive)
    {
        Collider[] bikerColliders;
        bikerColliders = bikerParentGameObject.GetComponentsInChildren<Collider>();
        Array.ForEach(bikerColliders, coll => coll.enabled = isCollidersActive);
    }

    ///////////////////////
    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.CompareTag("Obstacle"))
        {
            isSpinout = true;
            gameManager?.OnRiderEjected();
        }
    }

    bool CanRide()
    {
        return gameManager != null && gameManager.CurrentState == GameState.Riding;
    }

    public void HandleRiderEjected()
    {
        BikerRigidbodiesKinematicSituation(false);
        BikerCollidersEnabledSituation(true);
        ApplyRiderEjectionForce();
    }

    public void ResetForNewRun()
    {
        StopAllCoroutines();
        canMoveToFront = true;
        isStalled = false;
        isSpinout = false;
        speedDetector = 0f;
        slideValue = 0f;
        speed = 0;
        isMovingSoundPlaying = false;
        isInNeutralSoundPlaying = false;
        isbrakingSoundPlaying = false;
        isBraking = false;
        currentBrakePower = 0f;
        currentSteerRotateAngle = 0f;

        transform.position = startPosition;
        transform.rotation = startRotation;
        motorcycleRigidbody.velocity = Vector3.zero;
        motorcycleRigidbody.angularVelocity = Vector3.zero;

        bikerParentGameObject.transform.position = bikerStartPosition;
        bikerParentGameObject.transform.rotation = bikerStartRotation;
        rightArmTransform.localRotation = rightArmStartLocalRotation;
        leftArmTransform.localRotation = leftArmStartLocalRotation;

        ResetBikerRigidbodiesVelocity();
        BikerRigidbodiesKinematicSituation(true);
        BikerCollidersEnabledSituation(false);

        if (frontWheelCollider != null)
        {
            frontWheelCollider.brakeTorque = 0f;
            frontWheelCollider.steerAngle = 0f;
        }

        if (rearWheelCollider != null)
        {
            rearWheelCollider.motorTorque = 0f;
            rearWheelCollider.brakeTorque = 0f;
        }
    }

    void ResolveWheelColliders()
    {
        if (frontWheelCollider != null && rearWheelCollider != null)
            return;

        WheelCollider[] wheelColliders = GetComponentsInChildren<WheelCollider>();
        if (wheelColliders.Length == 0)
            return;

        WheelCollider frontCandidate = null;
        WheelCollider rearCandidate = null;
        float maxZ = float.MinValue;
        float minZ = float.MaxValue;

        foreach (WheelCollider wheel in wheelColliders)
        {
            float localZ = transform.InverseTransformPoint(wheel.transform.position).z;
            if (localZ > maxZ)
            {
                maxZ = localZ;
                frontCandidate = wheel;
            }

            if (localZ < minZ)
            {
                minZ = localZ;
                rearCandidate = wheel;
            }
        }

        if (frontWheelCollider == null)
            frontWheelCollider = frontCandidate;

        if (rearWheelCollider == null)
            rearWheelCollider = rearCandidate != frontWheelCollider ? rearCandidate : null;
    }

    void ApplyWheelSetup(WheelCollider wheelCollider)
    {
        if (wheelCollider == null)
            return;

        WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
        forwardFriction.stiffness = forwardFrictionStiffness;
        wheelCollider.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
        sidewaysFriction.stiffness = sidewaysFrictionStiffness;
        wheelCollider.sidewaysFriction = sidewaysFriction;

        JointSpring suspension = wheelCollider.suspensionSpring;
        suspension.spring = suspensionSpring;
        suspension.damper = suspensionDamper;
        wheelCollider.suspensionSpring = suspension;
    }

    void ApplyBrakeTorqueToAllWheels(float brakeTorque)
    {
        if (frontWheelCollider != null)
            frontWheelCollider.brakeTorque = brakeTorque;

        if (rearWheelCollider != null)
            rearWheelCollider.brakeTorque = brakeTorque;
    }

    float CalculateSteerMultiplier()
    {
        float speedKmh = motorcycleRigidbody.velocity.magnitude * 3.6f;
        float speedMultiplier = Mathf.InverseLerp(lowSpeedSteerReductionThreshold, fullSteerSpeedThreshold, speedKmh);

        float slopeAngle = 0f;
        if (frontWheelCollider != null && frontWheelCollider.GetGroundHit(out WheelHit hit))
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

        float slopeMultiplier = 1f - Mathf.InverseLerp(slopeSteerReductionStartAngle, slopeSteerReductionMaxAngle, slopeAngle);
        float steerMultiplier = speedMultiplier * slopeMultiplier;

        return Mathf.Clamp(steerMultiplier, minSteerMultiplier, 1f);
    }

}
