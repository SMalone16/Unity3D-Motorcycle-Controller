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
    [SerializeField] Transform[] SteeringPiecesTransforms;
    [SerializeField] float movePower;
    [SerializeField] float brakePower;
    [SerializeField] float inNeutralBrakePower;
    [SerializeField] float maxSteerRotateAngle;
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
        frontWheelCollider.motorTorque = verticalInput * movePower;
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
            frontWheelCollider.brakeTorque = inNeutralBrakePower;
            isMovingSoundPlaying = false;
        }
        else
        {
            SetMotorcycleInNeutralSound();
            frontWheelCollider.brakeTorque = 0;
            isMovingSoundPlaying = false;
        }
    }

    void HorizontalMove()
    {
        if (!CanRide())
            return;

        float horizontalInput = Input.GetAxis("Horizontal");
        currentSteerRotateAngle = maxSteerRotateAngle * horizontalInput;
        frontWheelCollider.steerAngle = currentSteerRotateAngle;
        //
        if (horizontalInput > 0.3f)
            FollowBikerArmsToSteeringWhenTurnRight();
        else if (horizontalInput < -0.3f)
            FollowBikerArmsToSteeringWhenTurnLeft();
        else
            FollowBikerArmsToSteeringWhenNoTurn();
    }

    ///////////////////////
    // Tilting To Motorcycle
    void TiltingToMotorcycle()
    {
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
        frontWheelCollider.brakeTorque = currentBrakePower;
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
            motorcycleRigidbody.velocity = Vector3.zero;
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

        frontWheelCollider.motorTorque = 0f;
        frontWheelCollider.brakeTorque = 0f;
        frontWheelCollider.steerAngle = 0f;
    }

}
