using UnityEngine;

[ExecuteInEditMode]
public class EditorRopeDrag : MonoBehaviour
{
    private PureCustomBucket bucket;
    private PBDRopePureManager ropeManager;
    private Vector3 lastPosition;

    void OnEnable()
    {
        bucket = GetComponent<PureCustomBucket>();
        ropeManager = Object.FindAnyObjectByType<PBDRopePureManager>();
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (bucket == null || ropeManager == null) return;

#if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            if (transform.position != lastPosition && GUIUtility.hotControl != 0)
            {
                bucket.isBeingDragged = true;
             //   ropeManager.ForceRopeEndPosition(bucket.CurrentAnchorWorldPosition);
            }
        }

        if (GUIUtility.hotControl == 0 && bucket.isBeingDragged)
        {
            bucket.isBeingDragged = false;
        }
#endif
        lastPosition = transform.position;
    }
}