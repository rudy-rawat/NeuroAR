using UnityEngine;
using System.Collections.Generic;

public class OrganRegistry : MonoBehaviour
{
    public static OrganRegistry Instance;

    public List<OrganVariant> OrganVariants = new List<OrganVariant>();
    private Dictionary<string, OrganVariant> organMap = new Dictionary<string, OrganVariant>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            foreach (var organ in OrganVariants)
            {
                if (!organMap.ContainsKey(organ.organName.ToLower()))
                {
                    organMap.Add(organ.organName.ToLower(), organ);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
        Debug.Log("OrganRegistry initialized with " + OrganVariants.Count + " variants.");
    }

    public GameObject GetBasic(string organName)
    {
        organName = organName.ToLower();

        if (organMap.ContainsKey(organName))
        {
            Debug.Log("Basic Loaded successfully of :- " + organName);
            return organMap[organName].basicPrefab;
        }
        Debug.LogWarning("Basic prefab is not found of : " + organName);
        return null;
    }

    public GameObject GetDetailed(string organName)
    {
        organName = organName.ToLower();

        if (organMap.ContainsKey(organName))
        {
            Debug.Log("Detailed Loaded successfully of :- " + organName);
            return organMap[organName].detailedPrefab;
        }
        Debug.LogWarning("Detailed prefab is not found of : " + organName);
        return null;
    }

    // New method to get the entire OrganVariant
    public OrganVariant GetOrganVariant(string organName)
    {
        organName = organName.ToLower();

        if (organMap.ContainsKey(organName))
        {
            return organMap[organName];
        }
        Debug.LogWarning("OrganVariant not found for: " + organName);
        return null;
    }
}