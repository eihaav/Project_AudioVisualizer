using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPresetManager : MonoBehaviour
{
    public List<ColorPreset> ColorPresets;
    public GameObject ButtonTemplate;
    public RectTransform ColorSelectButtonsParent;
    public static ColorPresetManager Instance;

    private EffectDriverBase _effectDriver;
    private BeatDriverBase _beatDriver;
    private int currentActivePreset = -1;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        GenerateButtons();
    }
    public void SetActiveEffectDriver(EffectDriverBase driver)
    {
        _effectDriver = driver;
        if (currentActivePreset == -1) 
        {
            SetColorPreset(ColorPresets[0]);
        }
        else
        {
            SetColorPreset(ColorPresets[currentActivePreset]);
        }
    }
    public void SetActiveBeatDriver(BeatDriverBase driver)
    {
        _beatDriver = driver;
        
        if (currentActivePreset == -1)
        {
            SetColorPreset(ColorPresets[0]);
        }
        else
        {
            SetColorPreset(ColorPresets[currentActivePreset]);
        }
    }
    private void GenerateButtons()
    {
        for (int i = 0; i < ColorPresets.Count; i++)
        {
            GenerateButton(ColorPresets[i]);
        }
    }
    private void GenerateButton(ColorPreset preset)
    {
        GameObject buttonObj = Instantiate(ButtonTemplate);
        buttonObj.SetActive(true);
        buttonObj.transform.SetParent(ColorSelectButtonsParent, false);
        Button button = buttonObj.GetComponent<Button>();
        Image buttonImage = buttonObj.GetComponent<Image>();
        button.onClick.AddListener(delegate { SetColorPreset(preset); });
        buttonImage.color = preset.color2;
    }
    private void SetColorPreset(ColorPreset preset)
    {
        if (_effectDriver != null)
        {
            _effectDriver.SetColorScheme(preset.color1, preset.color2);
        }
        if (_beatDriver != null)
        {
            _beatDriver.SetColorScheme(preset.color1, preset.color2);
        }
        currentActivePreset = ColorPresets.IndexOf(preset);
    }
}
