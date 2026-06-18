using TMPro;
using UnityEngine;

public class DialogueTester : MonoBehaviour
{
    public TMP_Text dialogueText;
    
    private OllamaManager ollama;

    private void Start()
    {
        ollama = gameObject.AddComponent<OllamaManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GenerateEvent("Players discover a locked door");
            Debug.Log("Generating dialogue for locked door event...");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GenerateEvent("A giant rock blocks the road");
            Debug.Log("Generating dialogue for rock blocking road event...");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GenerateEvent("The bridge is broken");
            Debug.Log("Generating dialogue for broken bridge event...");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            GenerateEvent("Players found a mysterious puzzle");
            Debug.Log("Generating dialogue for mysterious puzzle event...");
        }
    }

    public void GenerateEvent(string eventDescription)
    {
        string prompt =
        $@"Generate a short co-op game dialogue.

        P1: Impulsive and reckless.
        P2: Calm and strategic.

        Event: {eventDescription}

        p1:...
        p2:...

        Exactly 4 lines.";
        
        ollama.GenerateDialogue(
            prompt,
            OnDialogueGenerated);
    }

    private void OnDialogueGenerated(
        string dialogue)
    {
        UnityEngine.Debug.Log(
            "CALLBACK RECEIVED");

        UnityEngine.Debug.Log(
            dialogue);

        if (dialogueText == null)
        {
            UnityEngine.Debug.LogError(
                "TMP TEXT IS NULL");

            return;
        }

        dialogueText.text =
            dialogue;
    }
}
