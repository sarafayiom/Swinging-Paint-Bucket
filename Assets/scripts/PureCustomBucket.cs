using UnityEngine;

public class PureCustomBucket : MonoBehaviour
{
    [Header("Bucket Visual & Physical Scaling")]
    [Range(1.0f, 2.0f)] public float bucketScale = 1.0f;
    private float lastScale = -1f;

    [Header("Fluid Dynamics")]
    public float maxFluidCapacity = 10.0f;

    [Header("Rope Reference for Clamping")]
    public PBDRopePureManager ropeManager;

    [Header("Bucket Mass & Radius")]
    public float baseMass = 3.0f;
    public float bucketRadius = 0.3f;

    [Header("Launch Settings")]
    [Range(-90f, 90f)] public float initialLaunchAngle = 30f;
    public float initialVelocity = 0f;
    public Vector3 launchDirection = Vector3.forward;

    [Header("Swing Statistics")]
    public int swingCount = 0;
    public float swingThreshold = 0.1f;
    private float previousSide = 0f;

    [Header("Dynamic Anchor Point")]
    public Transform handleAnchor;

    [Header("Model Axis Correction")]
    public Vector3 rotationOffset = new Vector3(0, 0, 0);

    [Header("Interaction State")]
    public bool isBeingDragged = false;

    [Header("Physics Torsion")]
    public float k_torsion = 15.0f;
    [Range(0f, 10f)] public float torsionalDamping = 2.0f; 

    private float additionalFluidMass = 0.0f;
    private float currentTorsionAngle = 0.0f;
    private float torsionalVelocity = 0.0f;
  
    public float TotalMass => baseMass + additionalFluidMass;
    public Vector3 CurrentAnchorWorldPosition => (handleAnchor != null) ? handleAnchor.position : transform.position;
 
    private void OnValidate()
    {
        ApplyScale();
    }

    private void Start()
    {
        ApplyScale();
    }

    private void Update()
    {
        if (!Mathf.Approximately(lastScale, bucketScale))
        {
            ApplyScale();
            lastScale = bucketScale;
        }
    }
    //تغير حجم الدلو
    private void ApplyScale()
    {
        transform.localScale = Vector3.one * bucketScale;
        bucketRadius = 0.3f * bucketScale;
        baseMass = 3.0f * Mathf.Pow(bucketScale, 3f);
        maxFluidCapacity = 10.0f * Mathf.Pow(bucketScale, 3f);
    }
    // هي فيزيا الدلو يلي هي الفتل والدوران للدلو
    public void UpdateBucketPhysics(Vector3 targetAnchorPosition, Vector3 lastSegmentDir)
    {
        if (isBeingDragged) return;

        if (handleAnchor == null)
        {
            transform.position = targetAnchorPosition;
            return;
        }

        float currentSide = Vector3.Dot(lastSegmentDir, Vector3.right);
        if (Mathf.Abs(currentSide) > swingThreshold && Mathf.Abs(previousSide) > swingThreshold)
        {
            if (Mathf.Sign(currentSide) != Mathf.Sign(previousSide))
            {
                swingCount++;
            }
        }
        previousSide = currentSide;
        //قانون هوك للفتل
        float torsionalForce = -currentTorsionAngle * k_torsion;
        torsionalVelocity += torsionalForce * Time.deltaTime;

        torsionalVelocity *= Mathf.Clamp01(1.0f - torsionalDamping * Time.deltaTime);
        currentTorsionAngle += torsionalVelocity * Time.deltaTime;

        Quaternion swingRotation = Quaternion.FromToRotation(Vector3.down, lastSegmentDir);
        Quaternion torsionRotation = Quaternion.AngleAxis(currentTorsionAngle, Vector3.up);
        Quaternion targetRotation = swingRotation * torsionRotation * Quaternion.Euler(rotationOffset);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

        Vector3 worldOffset = transform.rotation * handleAnchor.localPosition;
        transform.position = targetAnchorPosition - worldOffset;

    }

    public void UpdateFluidMass(float mass) => additionalFluidMass = mass;
    public float GetCurrentTwist() => currentTorsionAngle;
}