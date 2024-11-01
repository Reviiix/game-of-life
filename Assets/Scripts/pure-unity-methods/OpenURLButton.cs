using Menu.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace pure_unity_methods
{
    [RequireComponent(typeof(GameButton))]
    public class OpenURLButton : MonoBehaviour
    {
        [SerializeField] private string url;
        private Button gameButton;

        private void Awake()
        {
            AssignButtonEvents();
        }
    
        private void AssignButtonEvents()
        {
            GetComponent<Button>().onClick.AddListener(OpenURLButtonPressed);
        }

        private void OpenURLButtonPressed()
        {
            Application.OpenURL(url);
        }
    }
}
