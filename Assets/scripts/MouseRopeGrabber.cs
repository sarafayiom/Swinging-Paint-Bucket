using UnityEngine;
using UnityEngine.InputSystem;

public class MouseRopeGrabber : MonoBehaviour
{
    public Camera cam;
    public PBDRopePureManager rope;

    [Header("Grab Settings")]
    public float grabRadius = 0.3f;

    private bool isGrabbing = false;
    private int grabbedIndex = -1;
    private Vector3 previousMousePos;
    private Vector3 releaseVelocity;
    private Vector3 dragTarget;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            int index = GetClosestRopeParticle(mouseWorld);
            if (index != -1)
            {
                grabbedIndex = index;
                isGrabbing = true;
                dragTarget = mouseWorld;
                previousMousePos = mouseWorld;
            }
        }

        if (isGrabbing && grabbedIndex != -1)
        {
            dragTarget = mouseWorld;
            releaseVelocity = (mouseWorld - previousMousePos) / Time.deltaTime;
            previousMousePos = mouseWorld;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isGrabbing && grabbedIndex != -1)
            {
                rope.AddImpulse(grabbedIndex, releaseVelocity);
            }

            isGrabbing = false;
            grabbedIndex = -1;
            rope.dragIndex = -1;
        }
    }

    void FixedUpdate()
    {
        if (isGrabbing && grabbedIndex != -1)
        {
            Vector3 target = dragTarget;
            target = rope.GetReachableGrabTarget(grabbedIndex, target);

            rope.dragIndex = grabbedIndex;
            rope.dragTarget = target;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 ropePos = rope.transform.position;
        float planeZ = ropePos.z;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, planeZ));

        plane.Raycast(ray, out float dist);
        return ray.GetPoint(dist);
    }

    bool IsNearRope(Vector3 mousePos)
    {
        return GetClosestRopeParticle(mousePos) != -1;
    }
    int GetClosestRopeParticle(Vector3 mousePos)
    {
        float minDist = grabRadius;
        int index = -1;

        int startIndex = 1;
        int endIndex = rope.numberOfSegments - 1;

        if (rope.currentType == PBDRopePureManager.RopeType.MetalCable)
        {
            startIndex = Mathf.Max(1, rope.numberOfSegments - 1);
            endIndex = rope.numberOfSegments - 1;
        }

        for (int i = startIndex; i <= endIndex; i++)
        {
            Vector3 p = rope.GetParticlePosition(i);
            float d = Vector3.Distance(mousePos, p);

            if (d < minDist)
            {
                minDist = d;
                index = i;
            }
        }

        return index;
    }
}