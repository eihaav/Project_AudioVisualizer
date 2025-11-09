using CSCore.CoreAudioAPI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public RectTransform AudioDeviceSelectionParent;
    public GameObject AudioDeviceButtonTemplate;

    private MMDeviceCollection _audioDevices;

    private void Start()
    {
        _audioDevices = InputAudioManager.Instance.GetActiveAudioDevices();
        SetupAudioDeviceButtons(_audioDevices);
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
}
