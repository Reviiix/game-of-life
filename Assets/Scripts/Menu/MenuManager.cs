using pure_unity_methods.Abstraction;
using UnityEngine;

namespace Menu
{
    public class MenuManager : Singleton<MenuManager>
    {
        [SerializeField] private Canvas main;
        [SerializeField] private Canvas settings;
        [SerializeField] private Canvas information;
        [SerializeField] private Canvas invalidGame;

        public void EnableMain(bool state = true)
        {
            main.enabled = state;
        }
    
        public void EnableSettings(bool state = true)
        {
            settings.enabled = state;
        }
    
        public void EnableInformation(bool state = true)
        {
            information.enabled = state;
        }
    
        public void EnableInvalidGame(bool state = true)
        {
            invalidGame.enabled = state;
        }
    }
}
