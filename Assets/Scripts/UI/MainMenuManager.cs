using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
   public async void PlayOnline()
    {
        await SceneController.Instance.NewTransitionPlan()
                                      .Load(new ParameterScene { Name = SceneDatabase.BOOTSTRAPONLINE})
                                      .UnLoad(new ParameterScene { Name = SceneDatabase.MAINMENU})
                                      .WithFadeIn()
                                      .Perform();
    }
}
