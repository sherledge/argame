using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI Reference")]
    public Toggle soundToggle;

    void Start()
    {
        // 1. Load saved value (Default to 1/True)
        bool isOn = PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
        
        // 2. Set the visual state of the toggle without triggering the event yet
        soundToggle.SetIsOnWithoutNotify(isOn);

        // 3. Apply the sound setting immediately so the game starts correctly
        ApplySoundSetting(isOn);

        // 4. AUTOMATICALLY Add the listener
        // This ensures the function runs whenever you click the toggle
        soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
    }

    // This runs when you click the toggle
    public void OnSoundToggleChanged(bool isOn)
    {
        // Save the preference
        PlayerPrefs.SetInt("SFX_ENABLED", isOn ? 1 : 0);
        PlayerPrefs.Save();

        // Actually mute/unmute the game
        ApplySoundSetting(isOn);
        
        Debug.Log($"Sound Toggled: {isOn}");
    }

    // Helper to actually change the volume
    private void ApplySoundSetting(bool isSoundOn)
    {
        // 0 = Muted, 1 = Full Volume
        AudioListener.volume = isSoundOn ? 1f : 0f;
    }
}