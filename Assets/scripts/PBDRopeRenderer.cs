using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PBDRopePureManager : MonoBehaviour
{
    public enum RopeType { NormalRope, MetalCable }

    [Header("Physics Settings")]
    public RopeType currentType = RopeType.NormalRope;
    public int numberOfSegments = 30;
    public float totalRopeLength = 5f;

    public int constraintIterations = 50;
    public float gravityForce = -9.81f;

    [Range(0.9f, 1f)] public float dampingFactor = 0.999f;

    [Header("Rendering Settings (Normal Rope)")]
    public float ropeRadius = 0.03f;
    public int radialSegments = 12;
    public Material ropeMaterial;

    [Header("Metal Chain Settings")]
    public Mesh chainLinkMesh;
    public Material chainMaterial;
    public Vector3 linkScale = Vector3.one;
    public Vector3 linkRotationOffset = new Vector3(90f, 0f, 0f);
    public Vector3 meshPivotOffset = Vector3.zero;

    [Header("Metal Physics Adjustments (Don't affect Normal Rope)")]
    public int metalIterationsMultiplier = 2;
    public float metalGravityMultiplier = 1.5f;

    [Header("Connected Custom Bucket")]
    public PureCustomBucket connectedBucket;
    public int dragIndex = -1;
    public Vector3 dragTarget;
  

    private class Particle
    {
        public Vector3 position;
        public Vector3 oldPosition;
//عطينا قيمة وسطية للموقع مشان الفرق الزمني بين تنفيذ الفيزيا والعرض على الشاشة
        public Vector3 interpolatedPosition;
        public float inverseMass;
//بالكونستراكتير عم نجهز الجزيئات للحبل وناخد مقلوب كتلتها
        public Particle(Vector3 pos, float mass)
        {
            position = oldPosition = interpolatedPosition = pos;
            inverseMass = mass > 0 ? 1f / mass : 0f;
        }
    }

    private List<Particle> ropeParticles = new List<Particle>();
    private List<GameObject> chainLinkGameObjects = new List<GameObject>();
    private Mesh ropeMesh;
    private Vector3[] meshVertices;
    private int[] meshTriangles;
    private Vector2[] meshUVs;
    private MeshFilter ropeMeshFilter;
    private MeshRenderer ropeMeshRenderer;

    private float currentStiffness = 1.0f;
    private float currentBendingStiffness;

    private Vector3 lastSideDirection = Vector3.right;
    private Vector3 lastUpDirection = Vector3.up;
    private float ropeDebugTimer;

    public Vector3 GetParticlePosition(int index) => ropeParticles[index].interpolatedPosition;
    public void ForceParticlePosition(int index, Vector3 target)
    {
        ropeParticles[index].position = target;
        ropeParticles[index].oldPosition = target;
    }
    //هي الدالة هي اول دالة بتشتغل 
    void Start()
    {
        ropeMeshRenderer = GetComponent<MeshRenderer>();
        SetupParticles();
        SetupMesh();
        SetupChainLinks();

        for (int i = 0; i < ropeParticles.Count; i++)
        {
            ropeParticles[i].interpolatedPosition = ropeParticles[i].position;
        }
        GenerateMeshSmooth();
    }
    //هون عم نجهز جزيئات الحبل ونوزعلها الكتل والمواقع
    void SetupParticles()
    {
        ropeParticles.Clear();
        float segmentLength = totalRopeLength / numberOfSegments;
        float finalMass = (connectedBucket != null) ? connectedBucket.TotalMass : 1.0f;

        Vector3 ropeDirection = Vector3.down;

        if (connectedBucket != null)
        {
            ropeDirection = Quaternion.AngleAxis(
                connectedBucket.initialLaunchAngle,
                connectedBucket.launchDirection.normalized
            ) * Vector3.down;
        }

        for (int i = 0; i <= numberOfSegments; i++)
        {
            Vector3 pos = transform.position + ropeDirection * (i * segmentLength);
            float mass = (i == 0) ? 0f : (i == numberOfSegments ? finalMass : 0.2f);
            ropeParticles.Add(new Particle(pos, mass));
        }

        if (connectedBucket != null && Mathf.Abs(connectedBucket.initialVelocity) > 0.01f)
        {
            Vector3 tangent = Vector3.Cross(ropeDirection, connectedBucket.launchDirection.normalized).normalized;
            foreach (var particle in ropeParticles)
            {
                particle.oldPosition = particle.position - tangent * connectedBucket.initialVelocity * Time.fixedDeltaTime;
            }
        }
    }
//دالة بترسملي الشكل الهندسي للحبل (الاسطوانة) يلي هي عبارة عن مربعات وكل مربع مثلثين
    void SetupMesh()
    {
        ropeMeshFilter = GetComponent<MeshFilter>();
        ropeMeshRenderer.material = ropeMaterial;
        ropeMesh = new Mesh();
        int vertexCount = (numberOfSegments + 1) * radialSegments;
        meshVertices = new Vector3[vertexCount];
        meshUVs = new Vector2[vertexCount];
        meshTriangles = new int[numberOfSegments * radialSegments * 6];
        int triangleIndex = 0;

        for (int i = 0; i < numberOfSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int nextJ = (j + 1) % radialSegments;
                int v1 = i * radialSegments + j;
                int v2 = i * radialSegments + nextJ;
                int v3 = (i + 1) * radialSegments + j;
                int v4 = (i + 1) * radialSegments + nextJ;
                meshTriangles[triangleIndex++] = v1; meshTriangles[triangleIndex++] = v3; meshTriangles[triangleIndex++] = v2;
                meshTriangles[triangleIndex++] = v2; meshTriangles[triangleIndex++] = v3; meshTriangles[triangleIndex++] = v4;
            }
        }
        ropeMesh.vertices = meshVertices; ropeMesh.triangles = meshTriangles; ropeMesh.uv = meshUVs;
        ropeMeshFilter.mesh = ropeMesh;
    }
    // هي الدالة بتركبلي الميش (السلسلة) للحبل المعدني
    void SetupChainLinks()
    {
        foreach (var chainlink in chainLinkGameObjects) if (chainlink) Destroy(chainlink);
        chainLinkGameObjects.Clear();

        for (int i = 0; i < numberOfSegments; i++)
        {
            GameObject chainlink = new GameObject($"ChainLink_{i}");
            chainlink.transform.SetParent(this.transform);

            MeshFilter mf = chainlink.AddComponent<MeshFilter>();
            MeshRenderer mr = chainlink.AddComponent<MeshRenderer>();

            mf.mesh = chainLinkMesh;
            mr.material = chainMaterial != null ? chainMaterial : ropeMaterial;

            chainlink.SetActive(false);
            chainLinkGameObjects.Add(chainlink);
        }
    }
    //هون كل الفيزيا الخاصة بالحبل
    void FixedUpdate()
    {
        int activeIterations = constraintIterations;
        float activeGravity = gravityForce;

        if (currentType == RopeType.NormalRope)
        {
            //هاد معامل الصلابة للحبل فيكن تغيروه انا حاليا حطيته 1 اذا بدكن يتمطط شوي حطوه مثلا 0.7
            currentStiffness = 1.0f;
            //وهاد معامل الصلابة للانحناء يعني قديش ممكن الحبل ينثني
            currentBendingStiffness = 0.01f;
        }
        else
        {
            currentStiffness = 1.0f;
            currentBendingStiffness = 0.7f;
            activeIterations = constraintIterations * metalIterationsMultiplier;
            activeGravity = gravityForce * metalGravityMultiplier;
        }

        ropeParticles[0].position = transform.position; ropeParticles[0].oldPosition = transform.position;
        float deltaTime = Time.fixedDeltaTime;

        if (connectedBucket != null && ropeParticles.Count > 0)
        {
            ropeRadius = 0.03f * connectedBucket.bucketScale;
            ropeParticles[numberOfSegments].inverseMass = 1f / connectedBucket.TotalMass;
            if (connectedBucket.isBeingDragged)
            {
                Vector3 target = connectedBucket.CurrentAnchorWorldPosition;
                if (currentType == RopeType.MetalCable)
                {
                    Vector3 root = ropeParticles[0].position;
                    Vector3 bucketDir = target - root;
                    if (bucketDir.sqrMagnitude < 0.0001f) bucketDir = Vector3.down;
                    target = root + bucketDir.normalized * totalRopeLength;
                }

                Vector3 delta = target - ropeParticles[numberOfSegments].position;
                ropeParticles[numberOfSegments].position += delta * 0.85f;
                ropeParticles[numberOfSegments].oldPosition = ropeParticles[numberOfSegments].position;
            }
        }
//تطبيق معادلة فيرلييه
        for (int i = 1; i < ropeParticles.Count; i++)
        {
            if (i == numberOfSegments && connectedBucket != null && connectedBucket.isBeingDragged) continue;
            Particle particle = ropeParticles[i];

            Vector3 velocity = (particle.position - particle.oldPosition) * dampingFactor;
            particle.oldPosition = particle.position;
            particle.position += velocity + Vector3.up * activeGravity * deltaTime * deltaTime;
        }

        float restLength = totalRopeLength / numberOfSegments;
//اثناء امساك الحبل بالماوس صفرنا الكتل مؤقتا حتى ما تأثر عليها القوى 
        float originalInverseMass = -1f;
        if (dragIndex >= 0 && dragIndex < ropeParticles.Count)
        {
            originalInverseMass = ropeParticles[dragIndex].inverseMass;
            ropeParticles[dragIndex].inverseMass = 0f;
        }
//هاد القسم المسؤول عن التكرارات لمنع تشوه الحبل او ابتعاد الجزيئات عن بعضها اكتر او اقل من مسافة البعد بين كل مقطع بالحبل
        for (int iter = 0; iter < activeIterations; iter++)
        {
            SolveMouseGrabConstraint();

            for (int i = 0; i < ropeParticles.Count - 1; i++)
                SolveDistance(ropeParticles[i], ropeParticles[i + 1], restLength);

            for (int i = ropeParticles.Count - 2; i >= 0; i--)
                SolveDistance(ropeParticles[i], ropeParticles[i + 1], restLength);

            for (int i = 0; i < ropeParticles.Count - 2; i++)
                SolveBendingImproved(ropeParticles[i], ropeParticles[i + 1], ropeParticles[i + 2]);
        }
        //رجعنا الكتل بعد افلات الحبل
        if (originalInverseMass >= 0f)
        {
            ropeParticles[dragIndex].inverseMass = originalInverseMass;
        }

        Vector3 ropeRoot = ropeParticles[0].position;
        Vector3 endToRoot = ropeParticles[numberOfSegments].position - ropeRoot;
        float currentDistance = endToRoot.magnitude;

        if (currentDistance > totalRopeLength + 0.001f)
        {
            Vector3 clampedEndPosition = ropeRoot + endToRoot.normalized * totalRopeLength;
            Vector3 currentVelocity = ropeParticles[numberOfSegments].position - ropeParticles[numberOfSegments].oldPosition;
            ropeParticles[numberOfSegments].position = clampedEndPosition;
            Vector3 ropeDirection = endToRoot.normalized;
            Vector3 normalVelocity = Vector3.Project(currentVelocity, ropeDirection);
            Vector3 tangentialVelocity = currentVelocity - normalVelocity;
            ropeParticles[numberOfSegments].oldPosition = clampedEndPosition - tangentialVelocity;

            if (connectedBucket != null && connectedBucket.isBeingDragged)
            {
                if (connectedBucket.handleAnchor != null)
                {
                    Vector3 handleOffset = connectedBucket.transform.position - connectedBucket.handleAnchor.position;
                    connectedBucket.transform.position = clampedEndPosition + handleOffset;
                }
                else { connectedBucket.transform.position = clampedEndPosition; }
            }
        }
    }
    //هي الدالة عم نجهز فيها للعرض منه وضع القيم الوسيطة مشان الفرق بين الفيزياء و العرض 
    //وكمان عم نجهز نوع الحبل ليصير اكتيف عند اختياره
    void LateUpdate()
    {
        float interpolationFactor = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        for (int i = 0; i < ropeParticles.Count; i++)
        {
            ropeParticles[i].interpolatedPosition = Vector3.Lerp(ropeParticles[i].oldPosition, ropeParticles[i].position, interpolationFactor);
        }

        if (connectedBucket != null && ropeParticles.Count > 0 && !connectedBucket.isBeingDragged)
        {
            Vector3 bucketTargetPos = ropeParticles[numberOfSegments].interpolatedPosition;
            Vector3 lastSegmentDir = (ropeParticles[numberOfSegments].interpolatedPosition - ropeParticles[numberOfSegments - 1].interpolatedPosition).normalized;
            connectedBucket.UpdateBucketPhysics(bucketTargetPos, lastSegmentDir);
        }

        if (currentType == RopeType.NormalRope)
        {
            ropeMeshRenderer.enabled = true;
            SetChainLinksActive(false);
            GenerateMeshSmooth();
        }
        else if (currentType == RopeType.MetalCable)
        {
            ropeMeshRenderer.enabled = false;
            SetChainLinksActive(true);
            UpdateChainVisuals();
        }
    }

    void SetChainLinksActive(bool isActive)
    {
        if (chainLinkGameObjects == null) return;
        foreach (var link in chainLinkGameObjects)
        {
            if (link != null && link.activeSelf != isActive)
                link.SetActive(isActive);
        }
    }
    //هي تجهيز عرض حبل السلسلة
    void UpdateChainVisuals()
    {
        if (chainLinkMesh == null || chainLinkGameObjects.Count == 0 || ropeParticles.Count < 2) return;

        Vector3 firstForward = (ropeParticles[1].interpolatedPosition - ropeParticles[0].interpolatedPosition).normalized;
        Vector3 lastChainUp = (Mathf.Abs(firstForward.y) > 0.99f) ? Vector3.right : Vector3.up;
        Vector3 lastChainSide = Vector3.Cross(firstForward, lastChainUp).normalized;
        lastChainUp = Vector3.Cross(lastChainSide, firstForward).normalized;

        float meshOriginalLength = chainLinkMesh.bounds.size.y;

        for (int i = 0; i < numberOfSegments; i++)
        {
            if (i >= chainLinkGameObjects.Count) break;
            GameObject link = chainLinkGameObjects[i];

            Vector3 p1 = ropeParticles[i].interpolatedPosition;
            Vector3 p2 = ropeParticles[i + 1].interpolatedPosition;

            Vector3 direction = (p2 - p1).normalized;
            if (direction.sqrMagnitude == 0) direction = Vector3.down;

            Vector3 side = Vector3.Cross(lastChainUp, direction).normalized;
            if (side.sqrMagnitude == 0) side = lastChainSide;
            Vector3 up = Vector3.Cross(direction, side).normalized;
            lastChainSide = side;
            lastChainUp = up;

            Vector3 basePosition = (p1 + p2) * 0.5f;
            float alternateTwist = (i % 2 == 0) ? 0f : 90f;

            Quaternion lookRot = Quaternion.LookRotation(direction, up);
            Quaternion twistRot = Quaternion.AngleAxis(alternateTwist, direction);

            link.transform.rotation = twistRot * lookRot * Quaternion.Euler(linkRotationOffset);
            link.transform.position = basePosition + link.transform.TransformDirection(meshPivotOffset);

            float currentSegmentLength = Vector3.Distance(p1, p2);
            Vector3 dynamicScale = linkScale;

            if (meshOriginalLength > 0.0001f)
            {
                dynamicScale.z = currentSegmentLength / meshOriginalLength;
            }

            link.transform.localScale = dynamicScale;
        }
    }
    //هي تجهيز شكل الحبل العادي
    void GenerateMeshSmooth()
    {
        float twistAngle = (connectedBucket != null) ? connectedBucket.GetCurrentTwist() : 0f;
        float angleStep = (360f / radialSegments) * Mathf.Deg2Rad;
        if (ropeParticles.Count < 2) return;

        Vector3 firstForward = (ropeParticles[1].interpolatedPosition - ropeParticles[0].interpolatedPosition).normalized;
        lastUpDirection = (Mathf.Abs(firstForward.y) > 0.99f) ? Vector3.right : Vector3.up;
        lastSideDirection = Vector3.Cross(firstForward, lastUpDirection).normalized;
        lastUpDirection = Vector3.Cross(lastSideDirection, firstForward).normalized;

        for (int i = 0; i <= numberOfSegments; i++)
        {
            Vector3 forward = (i < numberOfSegments) ? (ropeParticles[i + 1].interpolatedPosition - ropeParticles[i].interpolatedPosition).normalized : (ropeParticles[i].interpolatedPosition - ropeParticles[i - 1].interpolatedPosition).normalized;
            if (forward.sqrMagnitude == 0) forward = Vector3.down;
            Vector3 side = Vector3.Cross(lastUpDirection, forward).normalized;
            if (side.sqrMagnitude == 0) side = lastSideDirection;
            Vector3 up = Vector3.Cross(forward, side).normalized;
            lastSideDirection = side; lastUpDirection = up;

            for (int j = 0; j < radialSegments; j++)
            {
                float twist = twistAngle * ((float)i / numberOfSegments);
                float angle = j * angleStep + twist;
                Vector3 offset = (Mathf.Cos(angle) * side + Mathf.Sin(angle) * up) * ropeRadius;
                meshVertices[i * radialSegments + j] = (ropeParticles[i].interpolatedPosition + offset) - transform.position;
                meshUVs[i * radialSegments + j] = new Vector2((float)j / radialSegments, (float)i * 2f);
            }
        }
        ropeMesh.vertices = meshVertices;
        ropeMesh.uv = meshUVs;
        ropeMesh.RecalculateNormals();
        ropeMesh.RecalculateBounds();
    }
    //هي تصحيح مواضع جزيئات الحبل
    void SolveDistance(Particle particleA, Particle particleB, float rest)
    {
        Vector3 delta = particleA.position - particleB.position;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return;
        float difference = (distance - rest) / (distance * (particleA.inverseMass + particleB.inverseMass));
        if (particleA.inverseMass > 0) particleA.position -= delta * difference * particleA.inverseMass * currentStiffness;
        if (particleB.inverseMass > 0) particleB.position += delta * difference * particleB.inverseMass * currentStiffness;
    }
    //تصحيح انثناء الحبل 
    void SolveBendingImproved(Particle particleA, Particle particleB, Particle particleC)
    {
        Vector3 center = (particleA.position + particleC.position) * 0.5f;
        Vector3 direction = center - particleB.position;
        float totalMass = particleA.inverseMass + 2f * particleB.inverseMass + particleC.inverseMass;
        if (totalMass <= 0) return;
        Vector3 correction = direction * currentBendingStiffness;
        if (particleA.inverseMass > 0) particleA.position -= correction * (particleA.inverseMass / totalMass);
        if (particleB.inverseMass > 0) particleB.position += correction * (2f * particleB.inverseMass / totalMass);
        if (particleC.inverseMass > 0) particleC.position -= correction * (particleC.inverseMass / totalMass);
    }

    public void ForceRopeEndPosition(Vector3 targetPosition)
    {
        ropeParticles[numberOfSegments].position = targetPosition;
        ropeParticles[numberOfSegments].oldPosition = targetPosition;
    }

    void SolveMouseGrabConstraint()
    {
        if (dragIndex < 0 || dragIndex >= ropeParticles.Count) return;
        ropeParticles[dragIndex].position = dragTarget;
    }

    public Vector3 GetReachableGrabTarget(int particleIndex, Vector3 desiredTarget)
    {
        Vector3 root = ropeParticles[0].position;

        if (currentType == RopeType.NormalRope)
        {
            float maxDistance = (totalRopeLength / numberOfSegments) * particleIndex * 0.98f;
            Vector3 direction = desiredTarget - root;
            if (direction.magnitude > maxDistance)
                return root + direction.normalized * maxDistance;
            return desiredTarget;
        }
        else
        {
            float maxDistance = (totalRopeLength / numberOfSegments) * particleIndex;
            Vector3 direction = desiredTarget - root;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.down;

            return root + direction.normalized * maxDistance;
        }
    }

    public void AddImpulse(int index, Vector3 velocity)
    {
        if (index <= 0 || index >= ropeParticles.Count) return;
        ropeParticles[index].oldPosition = ropeParticles[index].position - velocity * Time.fixedDeltaTime * 0.3f;
    }
}