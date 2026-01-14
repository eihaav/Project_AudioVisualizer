using CSCore.CoreAudioAPI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public RectTransform AudioDeviceSelectionParent;
    public GameObject AudioDeviceButtonTemplate;
    public TMP_Dropdown NormalizationTypeDropdown;
    public float[] NormalizationAmplitudeMultipliers;

    private MMDeviceCollection _audioDevices;

    private Volume _volume;
    private VolumeProfile _volumeProfile;
    public TMP_Dropdown BloomDropdown, SSRDropdown, CloudsDropdown, FogDropdown;
    public Button OpenGraphicsButton, CloseGraphicsButton;
    public GameObject GraphicsMenu;
    private Canvas _thisCanvas;
    public string SceneToLoad;
    public Button SwitchSceneButton;
    public int FrequencyBandsOnChange;

    private void Start()
    {
        _thisCanvas = GetComponent<Canvas>();
        _volume = FindAnyObjectByType<Volume>();
        _volumeProfile = _volume.sharedProfile;
        _audioDevices = InputAudioManager.Instance.GetActiveAudioDevices();
        SetupAudioDeviceButtons(_audioDevices);
        NormalizationTypeDropdown.onValueChanged.AddListener(NormalizationTypeChanged);
        NormalizationTypeChanged(NormalizationTypeDropdown.value);
        InitGraphics();
        BloomDropdown.onValueChanged.AddListener(SetBloomValue);
        SSRDropdown.onValueChanged.AddListener(SetSSRValue);
        CloudsDropdown.onValueChanged.AddListener(SetCloudsValue);
        FogDropdown.onValueChanged.AddListener(SetFogValue);
        OpenGraphicsButton.onClick.AddListener(() => OpenGraphicsMenu(true));
        CloseGraphicsButton.onClick.AddListener(() => OpenGraphicsMenu(false));
        SwitchSceneButton.onClick.AddListener(() => 
        { 
            InputAudioManager.Instance.FrequencyBandCount = FrequencyBandsOnChange;
            SceneManager.LoadSceneAsync(SceneToLoad); 
        });
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _thisCanvas.enabled = !_thisCanvas.enabled;
        }
    }
    private void InitGraphics()
    {
        if (1 != PlayerPrefs.GetInt("bloomValue", 1))
        {
            BloomDropdown.value = 0;
        }
        else
        {
            BloomDropdown.value = PlayerPrefs.GetInt("bloomQuality", 1) + 1;
        }
        if (1 != PlayerPrefs.GetInt("ssrValue", 1))
        {
            SSRDropdown.value = 0;
        }
        else
        {
            SSRDropdown.value = PlayerPrefs.GetInt("ssrQuality", 1) + 1;
        }
        if (1 != PlayerPrefs.GetInt("cloudsValue", 0))
        {
            CloudsDropdown.value = 0;
        }
        else
        {
            CloudsDropdown.value = PlayerPrefs.GetInt("cloudsValue", 0) + 1;
        }
        if (1 != PlayerPrefs.GetInt("fogValue", 1))
        {
            FogDropdown.value = 0;
        }
        else
        {
            FogDropdown.value = PlayerPrefs.GetInt("fogQuality", 1) + 1;
        }
        SetBloom(1 == PlayerPrefs.GetInt("bloomValue", 1), PlayerPrefs.GetInt("bloomQuality", 1));
        SetClouds(1 == PlayerPrefs.GetInt("cloudsValue", 0), PlayerPrefs.GetInt("cloudsQuality", 1));
        SetSSR(1 == PlayerPrefs.GetInt("ssrValue", 1), PlayerPrefs.GetInt("ssrQuality", 1));
        SetFog(1 == PlayerPrefs.GetInt("fogValue", 1), PlayerPrefs.GetInt("fogQuality", 1));
    }
    private void SetupAudioDeviceButtons(MMDeviceCollection audioDevices)
    {
        for (int i = 0; i < audioDevices.Count; i++) 
        {
            MMDevice device = audioDevices.ItemAt(i);
            if (device.DeviceState != DeviceState.Disabled)
            {
                CreateAudioDeviceButton(device, i);
            }
        }
    }
    private void CreateAudioDeviceButton(MMDevice device, int deviceIndex)
    {
        GameObject newButtonObject = Instantiate(AudioDeviceButtonTemplate);
        newButtonObject.SetActive(true);
        newButtonObject.transform.SetParent(AudioDeviceSelectionParent.transform, false);

        Button button = newButtonObject.GetComponentInChildren<Button>();
        int indexOfDevice = deviceIndex;
        button.onClick.AddListener(delegate { InputAudioManager.Instance.ChangeAudioDevice(indexOfDevice); });
        TextMeshProUGUI deviceText = newButtonObject.GetComponentInChildren<TextMeshProUGUI>();
        deviceText.text = device.FriendlyName;
    }
    private void NormalizationTypeChanged(int newType)
    {
        InputAudioManager.Instance.SpectrumAmplitudeMultiplier = NormalizationAmplitudeMultipliers[newType];
        newType += 1; // Since 0 is without normalization and we'd like to skip that
        InputAudioManager.Instance.SetNormalizationType(newType);
    }
    public void ExitApplication()
    {
        Application.Quit();
    }
    public void SetBloomValue(int value)
    {
        value -= 1;
        if (value == -1)
        {
            SetBloom(false, 0);
        }
        else
        {
            SetBloom(true, value);
        }
    }
    public void SetSSRValue(int value)
    {
        value -= 1;
        if (value == -1)
        {
            SetSSR(false, 0);
        }
        else
        {
            SetSSR(true, value);
        }
    }
    public void SetCloudsValue(int value)
    {
        value -= 1;
        if (value == -1)
        {
            SetClouds(false, 0);
        }
        else
        {
            SetClouds(true, value);
        }
    }
    public void SetFogValue(int value) 
    {
        value -= 1;
        if (value == -1)
        {
            SetFog(false, 0);
        }
        else
        {
            SetFog(true, value);
        }
    }
    private void SetBloom(bool bloom, int quality)
    {
        if (_volumeProfile.TryGet<Bloom>(out var bloomComp))
        {
            ClampedFloatParameter value = bloomComp.intensity;
            value.value = bloom ? 0.49f : 0f;
            bloomComp.intensity = value;
            ScalableSettingLevelParameter setting = bloomComp.quality;
            setting.overrideState = true;
            setting.value = quality;
            bloomComp.quality = setting;
        }
        PlayerPrefs.SetInt("bloomValue", bloom ? 1 : 0);
        PlayerPrefs.SetInt("bloomQuality", quality);
    }
    private void SetSSR(bool ssr, int quality)
    {
        if (_volumeProfile.TryGet<ScreenSpaceReflection>(out var screenSpaceReflection)) 
        {
            screenSpaceReflection.active = true;
            BoolParameter enabledParam = screenSpaceReflection.enabled;
            enabledParam.value = ssr;
            screenSpaceReflection.enabled = enabledParam;
            screenSpaceReflection.enabledTransparent = enabledParam;
            ScalableSettingLevelParameter setting = screenSpaceReflection.quality;
            setting.overrideState = true;
            setting.value = quality;
            screenSpaceReflection.quality = setting;
        }
        PlayerPrefs.SetInt("ssrValue", ssr  ? 1 : 0);
        PlayerPrefs.SetInt("ssrQuality", quality);
    }
    private void SetClouds(bool clouds, int quality)
    {
        if (_volumeProfile.TryGet<VolumetricClouds>(out var volumetricClouds))
        {
            BoolParameter enabledParam = volumetricClouds.enable;
            enabledParam.value = clouds;
            volumetricClouds.enable = enabledParam;
            volumetricClouds.active = true;
            volumetricClouds.cloudSimpleMode.overrideState = true;
            volumetricClouds.cloudSimpleMode.value = quality <= 0 ? VolumetricClouds.CloudSimpleMode.Performance : VolumetricClouds.CloudSimpleMode.Quality;
        }
        PlayerPrefs.SetInt("cloudsValue", clouds ? 1 : 0);
        PlayerPrefs.SetInt("cloudsQuality", quality);
    }
    private void SetFog(bool fog, int quality)
    {
        if (_volumeProfile.TryGet<Fog>(out var volumetricFog))
        {
            BoolParameter enabledParam = volumetricFog.enabled;
            enabledParam.value = fog;
            //volumetricFog.enableVolumetricFog = enabledParam;
            volumetricFog.enabled = enabledParam;
            ScalableSettingLevelParameter setting = volumetricFog.quality;
            setting.overrideState = true;
            setting.value = quality;
            volumetricFog.quality = setting;
        }
        PlayerPrefs.SetInt("fogValue", fog ? 1 : 0);
        PlayerPrefs.SetInt("fogQuality", quality);
    }
    private void OpenGraphicsMenu(bool open)
    {
        if (open)
        {
            OpenGraphicsButton.gameObject.SetActive(false);
            GraphicsMenu.SetActive(true);
        }
        else
        {
            OpenGraphicsButton.gameObject.SetActive(true);
            GraphicsMenu.SetActive(false);
        }
    }
}
