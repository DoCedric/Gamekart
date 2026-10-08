using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

[Serializable]
public class kartSelectorData
{
    public GameObject kartPrefab;
    public Sprite kartIcon;
}

public class selector : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;

    [SerializeField] private List<kartSelectorData> kartData;
    private int currentIndex = 0;
    private Transform currentKartInstance;

    [SerializeField] private float timer = 4f;
    private float currentTimer = 0f;
    private Boolean timing = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnKart(currentIndex);
    }

    // Update is called once per frame
    void Update()
    {
        currentTimer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.RightArrow) || currentTimer >= timer)
        {
            currentIndex = (currentIndex + 1) % kartData.Count;
            ReplaceKart(currentIndex);
            currentTimer = 0f;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentIndex = (currentIndex - 1 + kartData.Count) % kartData.Count;
            ReplaceKart(currentIndex);
        }

        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if(timing)
            {
                timing = false;
            }
            else
            {
                timing = true;
                currentTimer = 0f;
            }
        }
    }

    void ReplaceKart(int index)
    {
        if (currentKartInstance != null)
        {
            Destroy(currentKartInstance.gameObject);
        }
        SpawnKart(index);
        UpdateText();
    }

    void SpawnKart(int index)
    {
        if (kartData.Count == 0 || kartData[index].kartPrefab == null)
            return;

        GameObject newKart = Instantiate(kartData[index].kartPrefab, transform);
        currentKartInstance = newKart.transform;
        UpdateText();
    }

    public void UpdateText() { 
        if (textField == null) { 
            Debug.LogWarning("TextMeshProUGUI reference is missing."); 
            return; 
        } 
        
        if (currentKartInstance.gameObject == null) { 
            textField.text = "No Prefab Assigned"; 
            return; 
        }

        String name = currentKartInstance.gameObject.name;

        name = CleanName(name);

        textField.text = name; 
    }

    string CleanName(string raw)
    {
        // Remove the trailing "_Kart(Clone)"
        raw = raw.Replace("_Kart(Clone)", "");
        raw = raw.Replace("(Clone)", "");
        raw = raw.Replace("Prefab", "");
        // Split on underscores
        string[] parts = raw.Split('_');

        // Take everything after the second underscore
        // Example: 2IGP22_D_Lescroart_Xander → ["2IGP22","D","Lescroart","Xander"]
        string[] nameParts = parts.Skip(2).ToArray();

        // Join with spaces
        return string.Join(" ", nameParts);
    }

}
