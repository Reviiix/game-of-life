using pure_unity_methods.Abstraction;
using UnityEngine;

public class MenuManager : Singleton<MenuManager>
{
    [SerializeField] private Canvas main;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject information;
    [SerializeField] private GameObject invalidGame;

    public void EnableMain(bool state = true)
    {
        main.enabled = state;
    }
    
    public void EnableSettings(bool state = true)
    {
        settings.SetActive(state);
    }
    
    public void EnableInformation(bool state = true)
    {
        information.SetActive(state);
    }
    
    public void EnableInvalidGame(bool state = true)
    {
        invalidGame.SetActive(state);
    }
}
