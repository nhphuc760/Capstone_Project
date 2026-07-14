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

    #region Save Login
    public void SaveLogin(string email, string password)
    {
        email = email.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
            return;

        bool updated = false;

        // check if the email already exists in the history
        for (int i = 0; i < emailHistory.Count; i++)
        {
            string[] info = emailHistory[i].Split(';');

            if (info.Length < 2)
                continue;

            if (info[0] == email)
            {
                // update the password for the existing email
                emailHistory[i] = email + ";" + password;
                updated = true;
                break;
            }
        }

        // update the email history if the email is new
        if (!updated)
        {
            emailHistory.Add(email + ";" + password);
        }

        PlayerPrefs.SetString("EmailHistory", string.Join("|", emailHistory));
        PlayerPrefs.Save();
    }
    #endregion

    #region Load Email
    private void LoadEmailHistory()
    {
        emailHistory.Clear();

        string history = PlayerPrefs.GetString("EmailHistory", "");

        if (string.IsNullOrEmpty(history))
            return;

        emailHistory.AddRange(history.Split('|'));
    }
    #endregion

    #region Show Suggestions
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
        Debug.Log("Suggestions count: " + count);
    }
    #endregion

    public void HideSuggestions()
    {
        suggestionPanel.SetActive(false);
    }
}