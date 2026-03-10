//using UnityEngine;
//using TMPro;

//public class LabelDebugHelper : MonoBehaviour
//{
//    void Start()
//    {
//        Debug.Log("=== LABEL DEBUG INFO ===");
//        Debug.Log($"Canvas Render Mode: {GetComponent<Canvas>()?.renderMode}");
//        Debug.Log($"Canvas Scale: {transform.lossyScale}");
//        Debug.Log($"Canvas Position: {transform.position}");
//        Debug.Log($"Parent: {transform.parent?.name}");

//        OrganLabelManager manager = GetComponent<OrganLabelManager>();
//        if (manager != null)
//        {
//            Debug.Log($"Label Prefab Assigned: {manager.labelPrefab != null}");
//            Debug.Log($"Line Length: {manager.lineLength}");
//            Debug.Log($"Label Distance: {manager.labelDistance}");
//        }
//    }

//    void Update()
//    {
//        // Draw gizmos for all child labels
//        foreach (Transform child in transform)
//        {
//            Debug.DrawLine(transform.position, child.position, Color.green);
//        }
//    }

//    void OnDrawGizmos()
//    {
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, 0.01f);

//        foreach (Transform child in transform)
//        {
//            Gizmos.color = Color.cyan;
//            Gizmos.DrawWireSphere(child.position, 0.005f);
//            Gizmos.DrawLine(transform.position, child.position);
//        }
//    }
//}