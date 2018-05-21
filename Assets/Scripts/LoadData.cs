using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class LoadData : MonoBehaviour {

    public Text Console;
    public Button loadButton;
    private Vector2 coordinate;
    public List<Vector2> Locations = new List<Vector2>();
    private int j;
    // Use this for initialization
    void Start () {

        loadButton.onClick.AddListener(ReadString);
		
	}
	

    void ReadString()
    {
        string path = "Assets/Resources/test.txt";

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);

        string[] splitArray;

        int i = 0;
        while (reader.Peek() > -1)
        {
            splitArray = reader.ReadLine().Split(new string[] { ";" }, StringSplitOptions.None);
            float latitude = float.Parse(splitArray[0]);
            float longitude = float.Parse(splitArray[1]);
            coordinate = new Vector2(latitude, longitude);

            Locations.Add(coordinate);
            Console.text += coordinate + "\n";
            Debug.Log(Locations[i].x);
            i++;
        }

        reader.Close();
    }

	// Update is called once per frame
	void Update () {
		
	}
}
