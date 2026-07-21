using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SFB;

public class PatchBrowserUI : MonoBehaviour
{
    [Header("Botones")]
    public Button newPatchButton;
    public Button loadPatchButton;

    [Header("Escena principal")]
    public string mainSceneName = "MainTest";

    private static readonly ExtensionFilter[] patchFilter =
    {
        new ExtensionFilter("Modular Patch", "patch.json"),
        new ExtensionFilter("Todos los archivos", "*")
    };

    private void Start()
    {
        newPatchButton.onClick.AddListener(OnNewPatch);
        loadPatchButton.onClick.AddListener(OnLoadPatch);
    }

    private void OnNewPatch()
    {
        PatchLoader.PatchToLoad = null;
        SceneManager.LoadScene(mainSceneName);
    }

    private void OnLoadPatch()
    {
        StandaloneFileBrowser.OpenFilePanelAsync(
            title:       "Abrir Patch",
            directory:   "",
            extensions:  patchFilter,
            multiselect: false,
            cb: paths =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

                PatchLoader.PatchToLoad = paths[0];
                SceneManager.LoadScene(mainSceneName);
            });
    }
}