using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void NewPatch()
    {
        SceneManager.LoadScene("MainTest");
    }

    public void LoadPatch()
    {
        SceneManager.LoadScene("MainTest");
    }


    public void ExitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}