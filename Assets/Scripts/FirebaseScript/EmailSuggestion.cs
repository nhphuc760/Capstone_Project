using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailSuggestion : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField emailInput;
     
    public GameObject suggestionPanel;
    public Transform content;
    public Button emailItemPrefab;

    private List<string> emailHistory = new List<string>();

    private void Start()
    {
        LoadEmailHistory();

        if (suggestionPanel != null)
            suggestionPanel.SetActive(false);
    }

    //==================================================
    // Save Email
    //==================================================

    public void SaveEmail(string email)
    {
        email = email.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
            return;

        if (!emailHistory.Contains(email))
        {
            emailHistory.Add(email);

            PlayerPrefs.SetString(
                "EmailHistory",
                string.Join("|", emailHistory));

            PlayerPrefs.Save();
        }
    }

    //==================================================
    // Load Email
    //==================================================

    private void LoadEmailHistory()
    {
        emailHistory.Clear();

        string history = PlayerPrefs.GetString("EmailHistory", "");

        if (string.IsNullOrEmpty(history))
            return;

        emailHistory.AddRange(history.Split('|'));
    }

    //==================================================
    // Show Suggestions
    //==================================================

    public void ShowSuggestions()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        string keyword = emailInput.text.ToLower();

        int count = 0;

        foreach (string email in emailHistory)
        {
            if (!email.StartsWith(keyword))
                continue;

            Button item =
                Instantiate(emailItemPrefab, content);

            item.GetComponentInChildren<TextMeshProUGUI>().text = email;

            //Button btn = item.GetComponent<Button>();

            item.onClick.RemoveAllListeners();

            item.onClick.AddListener(() =>
            {
                //Debug.Log("Selected Email: " + email);
                emailInput.text = email;
                HideSuggestions();
                Debug.Log(emailInput.text);
            });

            count++;
        }

        suggestionPanel.SetActive(count > 0); // Show the panel only if there are suggestions
    }

    //==================================================
    // Hide Suggestions
    //==================================================

    public void HideSuggestions()
    {
        suggestionPanel.SetActive(false);
    }
}