using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class LocationManager : MonoBehaviour {

    public InputField gpsValuesInput;
    public Button AddLocationButton;
    public string InputValueString;
    private int j = 0;
    public Text ShowAddedLocation;

	// Use this for initialization
	void Start () {

        AddLocationButton.onClick.AddListener(WriteString);

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void WriteString()
    {
        j++;
        InputValueString = gpsValuesInput.text;
        string path = "Assets/Resources/test.txt";

        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(InputValueString);
        ShowAddedLocation.text += InputValueString + "\n";
        writer.Close();
        gpsValuesInput.text = "";

    }

    
}
