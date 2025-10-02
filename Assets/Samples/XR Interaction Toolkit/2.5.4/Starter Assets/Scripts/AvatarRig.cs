using UnityEngine;

public class AvatarRig : MonoBehaviour
{
    [Header("Targets del XR Rig")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Huesos del avatar")]
    public Transform headBone;
    public Transform leftHandBone;
    public Transform rightHandBone;
    public Transform hips;

    void LateUpdate()
    {
        if (headTarget != null && headBone != null)
        {
            headBone.position = headTarget.position;
            headBone.rotation = headTarget.rotation;
        }

        if (leftHandTarget != null && leftHandBone != null)
        {
            leftHandBone.position = leftHandTarget.position;
            leftHandBone.rotation = leftHandTarget.rotation;
        }

        if (rightHandTarget != null && rightHandBone != null)
        {
            rightHandBone.position = rightHandTarget.position;
            rightHandBone.rotation = rightHandTarget.rotation;
        }

        Vector3 pos = headBone.position;
        pos.y = hips.position.y; // mantener altura del avatar
        hips.position = pos;
    }
}
