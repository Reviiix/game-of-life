using GridSystem;
using pure_unity_methods.Abstraction;
using pure_unity_methods.StateManagement;
using UnityEngine;

public class ProjectInitializer : MonoBehaviour, IInitializer
{
    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        StartCoroutine(GridManager.Instance.Initialise(() =>
        {
            SequentialStateManager.Instance.Initialise();
        }));
    }
}
