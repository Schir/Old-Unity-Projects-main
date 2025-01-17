﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOrbiter : MonoBehaviour
{
    [SerializeField]
    Transform focus = default;

    [SerializeField, Range(0f, 20f)]
    float distance= 5f;

    [SerializeField, Min(0f)]
    float responsiveness = 0f;
    Vector3 focusPoint, previousFocusPoint;
    Vector2 orbitAngles = new Vector2(45f, 0f);
    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;
    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;
    [SerializeField, Min(0f)]
    float alignDelay = 5f;
    float lastManualRotationTime;
    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;
    Camera regularCamera;
    [SerializeField]
    LayerMask obstructionMask = -1;
    // Start is called before the first frame update
    void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        UpdateFocusPoint();
        Quaternion lookRotation;
        
        
        if(Input.GetMouseButton(0) || distance == 0f)
        {
            if(ManualRotation() || AutomaticRotation())
            {
                ConstrainAngles();
                lookRotation = Quaternion.Euler(orbitAngles);
            }
            else
            {
                lookRotation = transform.localRotation;
            }
        }
            else
            {
                lookRotation = transform.localRotation;
            }

            Vector3 lookDirection = lookRotation * Vector3.forward;
            float lookDistance;
            if(Physics.BoxCast(focusPoint, CameraHalfExtends, -lookDirection, out RaycastHit hit, lookRotation, distance - regularCamera.nearClipPlane, obstructionMask))
            {
                lookDistance = hit.distance + regularCamera.nearClipPlane;
            }
            else
            {
                lookDistance = distance; 
            }
            Vector3 lookPosition = focusPoint - lookDirection * lookDistance;
            transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        focusPoint = focus.position;
        if(responsiveness>0f)
        {
            float distanceSqr = (focusPoint - previousFocusPoint).sqrMagnitude;
            focusPoint = Vector3.Lerp(previousFocusPoint,focusPoint, distanceSqr * responsiveness * Time.unscaledDeltaTime);
        }
    }
    bool ManualRotation()
    {
        Vector2 input = new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));
        const float e = 0.001f;
        if(input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }
    void OnValidate()
    {
        if(maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }
    void ConstrainAngles()
    {
        orbitAngles.x = 
            Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);
        if(orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        }
        else if(orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }
    bool AutomaticRotation()
    {
        if(Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }
        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z
        );
        float movementDeltaSqr = movement.sqrMagnitude;
        if(movementDeltaSqr < 0.000001f)
        {
            return false;
        }
        float headingAngle = GetAngle(movement/Mathf.Sqrt(movementDeltaSqr));
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if(deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if(180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }
    static float GetAngle (Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle: angle;
    }
    Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }
}
