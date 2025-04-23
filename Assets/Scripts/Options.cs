using UnityEngine;
using System.Collections;

public class Options : MonoBehaviour
{
    public dfButton muteButton;
    private static bool soundEnabled = true;

	// Use this for initialization
	void Awake ()
	{
        Debug.Log("Options: Awake");
        DontDestroyOnLoad(this);

	    SetVolume(soundEnabled);
	}
	
	// Update is called once per frame
	void Start () {
        Debug.Log("Options: Start");
	}

    public void MuteButtonPressed(dfControl ignore, dfMouseEventArgs args)
    {
        soundEnabled = !soundEnabled;

        SetVolume(soundEnabled);
    }

    private void SetVolume(bool isEnabled)
    {
        var volume = isEnabled ? 1 : 0;
        AudioListener.volume = volume;
        muteButton.BackgroundSprite = isEnabled ? "Mute-unclicked" : "Mute-clicked";
    }
}
