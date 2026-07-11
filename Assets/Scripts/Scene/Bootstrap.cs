using Firebase.Database;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] FirstTimeSetup firstTimeSetup;
    DatabaseReference @ref;
    private async void Start()
    {
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}");
        var data = await @ref.GetValueAsync();
        if (data.Exists)
        {
            await NetworkDataManager.Instance.Initialize(data);
            await SceneController.Instance.NewTransitionPlan()
                                   .Load(new ParameterScene { Name = SceneDatabase.LOBBY }, true)
                                   .UnLoad(new ParameterScene { Name = SceneDatabase.MAINMENU })
                                   .WithFadeOut()
                                   .Perform();
        }
        else
        {
            firstTimeSetup.gameObject.SetActive(true);
            await SceneController.Instance.loadingOverlay.FadeOutBlack(.5f);
        }
    }
}
