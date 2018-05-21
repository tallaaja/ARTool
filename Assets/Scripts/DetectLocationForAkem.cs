using GoogleARCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectLocationForAkem : MonoBehaviour {


    public int NumberOfLocations = 0;

    public Image ImageObj;
    public GameObject ARController;
    private bool running;
    private int maxWait = 10;
    //public float dLatitude1 = 60.16954f, dLongitude1 = 24.93391f;
    private float sLatitude, sLongitude;
    private bool ready = false;
    private float distanceFromTarget = 0.004f;
    private float proximity = 0.001f;
    public Text text;
    private Vector2 deviceCoordinates;
    public Vector2[] targetCoordinates;
    public float[] latitudes, longitudes;
    public Sprite[] pictures;





    // Use this for initialization
    void Start () {
        latitudes = new float[NumberOfLocations];
        longitudes = new float[NumberOfLocations];
        targetCoordinates = new Vector2[NumberOfLocations];

        for(int i = 0; i<targetCoordinates.Length; i++)
        {
            targetCoordinates[i] = new Vector2(latitudes[i], longitudes[i]);
        }

        var firstPermission = AndroidPermissionsManager.RequestPermission("android.permission.ACCESS_FINE_LOCATION");
        

        if (firstPermission == null)
        {
            text.text += "null";
        }

        firstPermission.WaitForCompletion();

        StartCoroutine(getLocation());

    }
	
    private void OnDeny()
    {
        text.text += "deny";
    }
	// Update is called once per frame
	void Update () {
		
	}

    IEnumerator getLocation()
    {

        running = true;

        //LocationService service = Input.location;
        /*if (!Input.location.isEnabledByUser)
        {
            text.text += "Location Services not enabled by user";
            yield break;
        }*/

        Input.location.Start();


        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 3)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
            text.text = " wait";
        }
        if (maxWait < 1)
        {
            text.text = "Timed out";
            yield break;
        }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            text.text = "Unable to determine device location";
            yield break;
        }
        else
        {
            text.text = "Target Location : " + latitudes[0] + ", " + longitudes[0] + "\nMy Location: " + Input.location.lastData.latitude + ", " + Input.location.lastData.longitude;
            sLatitude = Input.location.lastData.latitude;
            sLongitude = Input.location.lastData.longitude;
        }
        //service.Stop();
        ready = true;
        startCalculate();

        //text.text += "wait" + sLatitude + sLongitude;
        yield return new WaitForSeconds(1);
        StartCoroutine(getLocation());

    }

    public void startCalculate()
    {
        deviceCoordinates = new Vector2(sLatitude, sLongitude);
        proximity = Vector2.Distance(targetCoordinates[0], deviceCoordinates);

        if (proximity <= distanceFromTarget)
        {
            Debug.Log("something");
            ImageObj.sprite = pictures[0];
            ImageObj.gameObject.SetActive(true);

        }
       
    }
}
