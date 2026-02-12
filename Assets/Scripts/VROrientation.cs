using UnityEngine;
using System.Collections;
using System;

public class VROrientation : MonoBehaviour
{
    public Transform xrOrigin;
    public Transform xrCamera;
    public Transform spawnPoint;
    public Transform lookTarget;

    void Start()
    {
        StartCoroutine(AlignAfterTracking());
    }

    IEnumerator AlignAfterTracking()
    {
        yield return null; 
        yield return null;

        RepositionPlayer();
    }

    public void RepositionPlayer()
    {
        Vector3 cameraLocalOffset = xrCamera.localPosition;
        cameraLocalOffset.y = 0;

        xrOrigin.position = spawnPoint.position - xrOrigin.TransformVector(cameraLocalOffset);

        Vector3 direction = lookTarget.position - xrCamera.position;
        direction.y = 0;

        xrOrigin.rotation = Quaternion.LookRotation(direction);
        Console.WriteLine("Player repositioned to: " + xrOrigin.position + " with rotation: " + xrOrigin.rotation);
    }
}
