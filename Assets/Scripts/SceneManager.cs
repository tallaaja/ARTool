using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneManager : MonoBehaviour {

    public Button LoadSceneButton;
    public Button CreateSceneButton;

	// Use this for initialization
	void Start () {

        LoadSceneButton.onClick.AddListener(Load);
        CreateSceneButton.onClick.AddListener(Create);

    }

    void Load() {

        Application.LoadLevel("LoadScene");
    }


    void Create()
    {
        Application.LoadLevel("CreateScene");
    }

    // Update is called once per frame
    void Update () {
		
	}
}
