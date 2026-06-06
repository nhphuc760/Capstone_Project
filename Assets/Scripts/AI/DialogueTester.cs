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
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GenerateEvent("A giant rock blocks the road");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GenerateEvent("The bridge is broken");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            GenerateEvent("Players found a mysterious puzzle");
        }
    }

    public void GenerateEvent(string eventDescription)
    {
        string prompt =
        $@"Generate a short co-op game dialogue.

        P1: Impulsive.
        P2: Calm.

        Event: {eventDescription}

        Exactly 4 lines.";
        
        ollama.GenerateDialogue(
            prompt,
            OnDialogueGenerated);
    }

    private void OnDialogueGenerated(string dialogue)
    {
        Debug.Log(dialogue);

        dialogueText.text = dialogue;
    }
}