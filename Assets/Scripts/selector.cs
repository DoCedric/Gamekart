using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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

    [Header("Flow")]
    [SerializeField] private SceneReference trackScene;
    private bool confirmed = false;

    [Header("Input")]
    [SerializeField] InputActionReference nextAction;
    [SerializeField] InputActionReference previousAction;
    [SerializeField] InputActionReference toggleTimerAction;
    [SerializeField] InputActionReference confirmAction;

    void OnEnable()
    {
        nextAction.action.Enable();
        previousAction.action.Enable();
        toggleTimerAction.action.Enable();
        confirmAction.action.Enable();
    }

    void OnDisable()
    {
        nextAction.action.Disable();
        previousAction.action.Disable();
        toggleTimerAction.action.Disable();
        confirmAction.action.Disable();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnKart(currentIndex);
    }

    // Update is called once per frame
    void Update()
    {
        if (confirmed) return;

        if (confirmAction.action.WasPressedThisFrame())
        {
            ConfirmSelection();
            return;
        }

        if (timing) currentTimer += Time.deltaTime;

        if (nextAction.action.WasPressedThisFrame() || currentTimer >= timer)
        {
            currentIndex = (currentIndex + 1) % kartData.Count;
            ReplaceKart(currentIndex);
            currentTimer = 0f;
        }
        else if (previousAction.action.WasPressedThisFrame())
        {
            currentIndex = (currentIndex - 1 + kartData.Count) % kartData.Count;
            ReplaceKart(currentIndex);
        }

        else if (toggleTimerAction.action.WasPressedThisFrame())
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

    void ConfirmSelection()
    {
        if (kartData.Count == 0 || kartData[currentIndex].kartPrefab == null)
            return;

        if (string.IsNullOrEmpty(trackScene.ScenePath))
        {
            Debug.LogWarning("selector: no track scene assigned.", this);
            return;
        }

        confirmed = true;
        KartSelection.SelectedKartPrefab = kartData[currentIndex].kartPrefab;
        SceneManager.LoadScene(trackScene.ScenePath);
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
        KartDrivingState.SetDrivingEnabled(newKart, false);
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
