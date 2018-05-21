using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExitToMainMenu : MonoBehaviour {


    public Button exit;
    

	// Use this for initialization
	void Start () {

        exit.onClick.AddListener(exitOnClick);
		
	}

    void exitOnClick()
    {
        Application.LoadLevel("MainMenu");
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
