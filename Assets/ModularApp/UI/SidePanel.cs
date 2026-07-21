using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using SFB;

public class SidePanel : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform moduleListContainer;
    public GameObject addButtonPrefab;
    public PortUI masterOutPortUI;

    [Header("Master Out")]
    public Button playPauseButton;

    [Header("Patch")]
    public Button saveButton;
    public Button menuButton;

    private static readonly (string groupName, string[] modules)[] moduleGroups =
    {
        ("Audio Generation", new[] {"Oscillator"}),
        ("Processing and FX", new[] {"Amplifier", "Filter", "Distorsion", "Delay", "Reverb", "RingMod"}),
        ("Modulation", new[] {"LFO", "Envelope", "SampleHold"}),
        ("Control and CV Generation", new[] {"StepSequencer", "Pitch Quantizer", "MIDI to CV"}),
        ("Utilities", new[] {"Mixer", "Attenuverter", "Clock", "Oscilloscope"}),
    };

    private static readonly Color headerBgColor = new Color(0.09f, 0.09f, 0.09f, 1f);
    private static readonly Color buttonBgColor = new Color(0.165f, 0.165f, 0.165f, 1f);

    private static readonly ExtensionFilter[] patchFilter =
    {
        new ExtensionFilter("Modular Patch", "patch.json")
    };

    private void Start()
    {
        foreach (var (groupName, modules) in moduleGroups)
            CreateModuleGroup(groupName, modules);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(moduleListContainer);

        InitializeMasterOutPort();

        if (saveButton) saveButton.onClick.AddListener(OnSavePatch);
        if (menuButton) menuButton.onClick.AddListener(OnGoToMenu);
    }

    private void CreateModuleGroup(string groupName, string[] modules)
    {
        GameObject header = CreateHeaderButton(groupName);
        GameObject content = CreateGroupContent(groupName);

        foreach (string moduleType in modules)
            CreateAddButton(moduleType, content.transform);

        TextMeshProUGUI headerTMP = header.GetComponentInChildren<TextMeshProUGUI>();
        bool isExpanded = true;
 
        header.GetComponent<Button>().onClick.AddListener(() =>
        {
            isExpanded = !isExpanded;
            content.SetActive(isExpanded);

            string arrow = isExpanded ? "- " : "+ ";
            headerTMP.text = arrow + groupName;

            LayoutRebuilder.ForceRebuildLayoutImmediate(moduleListContainer);
        });
 
        headerTMP.text = "- " + groupName;
    }

    private GameObject CreateHeaderButton(string groupName)
    {
        GameObject header = Instantiate(addButtonPrefab, moduleListContainer);
        header.name = "GroupHeader_" + groupName;

        Image bg = header.GetComponent<Image>();
        if (bg) bg.color = headerBgColor;
 
        TextMeshProUGUI tmp = header.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp)
        {
            tmp.text = groupName;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = new Vector4(6f, 0f, 0f, 0f);
        }
 
        return header;
    }

    private GameObject CreateGroupContent(string groupName)
    {
        GameObject content = new GameObject("GroupContent_" + groupName);
        content.transform.SetParent(moduleListContainer, false);
 
        RectTransform rt = content.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = Vector2.zero;
 
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.spacing                = 2f;
        vlg.padding                = new RectOffset(4, 4, 2, 2);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
 
        return content;
    }

    private const float MASTER_OUT_PORT_SIZE = 26f;

    private void InitializeMasterOutPort()
    {
        Debug.Log($"[SidePanel] Synthesizer.Instance: {Synthesizer.Instance}");
        
        var masterOut = Synthesizer.Instance.GetMasterOut();
        Debug.Log($"[SidePanel] masterOut: {masterOut}");
        
        Port port = masterOut.GetPort("audio_in");
        Debug.Log($"[SidePanel] port: {port}");
        
        Debug.Log($"[SidePanel] masterOutPortUI: {masterOutPortUI}");
        
        masterOutPortUI.Initialize(port, null);

        if (masterOutPortUI.portCircle != null)
        {
            masterOutPortUI.portCircle.rectTransform.sizeDelta =
                new Vector2(MASTER_OUT_PORT_SIZE, MASTER_OUT_PORT_SIZE);
        }


        if (playPauseButton != null)
        {
            UpdatePlayPauseLabel();
            playPauseButton.onClick.AddListener(() =>
            {
                Synthesizer.Instance.run = !Synthesizer.Instance.run;
                UpdatePlayPauseLabel();
            });
        }
    }

    private void UpdatePlayPauseLabel()
    {
        if (playPauseButton == null) return;
        TextMeshProUGUI label = playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = Synthesizer.Instance.run ? "Pause" : "Play";
    }

    private void CreateAddButton(string moduleType, Transform parent)
    {
        GameObject btn = Instantiate(addButtonPrefab, parent);
        btn.name = "ModuleBtn_" + moduleType;
 
        Image bg = btn.GetComponent<Image>();
        if (bg) bg.color = buttonBgColor;
 
        btn.GetComponentInChildren<TextMeshProUGUI>().text = moduleType;
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            ModuleUIFactory.Instance.CreateModule(moduleType);
        });
    }

    private void OnSavePatch()
    {
        StandaloneFileBrowser.SaveFilePanelAsync(
            title:            "Guardar Patch",
            directory:        "",
            defaultName:      "patchname",
            extension:        "patch.json",
            cb: path =>
            {
                if (string.IsNullOrEmpty(path)) return;

                if (!path.EndsWith(".patch.json"))
                    path += ".patch.json";

                PatchSerializerUI.Instance.SavePatchToPath(path);
            });
    }

    private void OnGoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}