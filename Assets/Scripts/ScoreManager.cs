using System;
using UnityEngine;
using System.Collections;

public enum PlayerType
{
    Server = 0,
    Client = 1
}
public class ScoreManager : MonoBehaviour
{
    public dfLabel serverScore;
    public dfLabel clientScore;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public int IncrementScore(int playerType)
    {
        var pType = (PlayerType) playerType;
        dfLabel textbox = pType == PlayerType.Server ? serverScore : clientScore;

        var currentScore = Convert.ToInt32(textbox.Text);
        textbox.Text = (++currentScore).ToString();

        return Convert.ToInt32(textbox.Text);
    }
}
