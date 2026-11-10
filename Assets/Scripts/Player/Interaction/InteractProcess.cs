using UnityEngine;
using UnityEngine.UI;

public class InteractProcess : MonoBehaviour, IProcessor
{
    [SerializeField] Image processBar;
    [SerializeField] float lastDurationShow = 0.1f;
    float timer = 0f;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {       
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);   
    }

    private void LateUpdate()
    {

        if(timer > 0)
        {
            timer -= Time.deltaTime;    
        }

        if (timer <= 0)
        {
            gameObject.SetActive(false);
        }
    }


    public void UpdateProcess(float value)
    {
        timer = lastDurationShow;
        gameObject.SetActive(true);
        if(processBar)
            processBar.fillAmount = value;
    }



}
