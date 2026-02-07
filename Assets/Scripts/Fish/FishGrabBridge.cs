using UnityEngine;


[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class FishGrabBridge : MonoBehaviour
{
    FishAI fish;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        fish = GetComponent<FishAI>();
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        grab.selectEntered.AddListener(_ => fish.isGrabbed = true);
        grab.selectExited.AddListener(_ => fish.isGrabbed = false);
    }
}
