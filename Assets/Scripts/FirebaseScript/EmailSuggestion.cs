using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailSuggestion : MonoBehaviour
{
    [Header("Login")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Suggestion Panel")]
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

    public void SaveLogin(string email, string password)
    {
        email = email.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
            return;

        string key = email + ";" + password;

        bool exists = false;

        foreach (string item in emailHistory)
        {
            if (item.StartsWith(email + ";"))
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            emailHistory.Add(key);

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

        foreach (string data in emailHistory)
        {
            string[] info = data.Split(';');

            if (info.Length < 2)
                continue;

            string email = info[0];
            string password = info[1];

            if (!email.StartsWith(keyword))
                continue;

            Button item = Instantiate(emailItemPrefab, content);

            item.GetComponentInChildren<TextMeshProUGUI>().text = email;

            item.onClick.RemoveAllListeners();

            item.onClick.AddListener(() =>
            {
                emailInput.text = email;
                passwordInput.text = password;

                HideSuggestions();
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