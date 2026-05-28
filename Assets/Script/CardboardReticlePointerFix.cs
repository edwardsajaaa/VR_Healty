using UnityEngine;

public class CardboardReticlePointerFix : MonoBehaviour
{
    private CardboardReticlePointer reticlePointer;

    void Awake()
    {
        reticlePointer = GetComponent<CardboardReticlePointer>();
        
        if (reticlePointer != null)
        {
            if (reticlePointer.ReticleInteractionLayerMask == 0)
                reticlePointer.ReticleInteractionLayerMask = 1 << 0;
        }
    }
}
