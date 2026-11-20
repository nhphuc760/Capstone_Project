using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyUIManager : MonoBehaviour
{
    private async void Start()
    {
        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeOut()
                                          .Perform();
        }
    }
}