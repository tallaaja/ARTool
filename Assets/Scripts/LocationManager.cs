using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using GoogleARCore;
using ImageAndVideoPicker;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public class LocationManager : MonoBehaviour {

    public static string json;
    SpriteRenderer m_SpriteRenderer;
    private Texture2D texture;
    private Sprite mySprite;
    public Image m_Image;
    private string filename;

    public Button AddPhoto;
    public Button usethislocation;
    private float sLatitude, sLongitude;
    private bool ready = false;
    private bool running;
    private int maxWait = 10;
    private Vector2 deviceCoordinates;

    public InputField gpsValuesInput;
    public Button AddLocationButton;
    public string InputValueString;
    private int j = 0;
    public Text ShowAddedLocation;
    public string LastOpenedImagePath;
    public static byte[] bytes;
    public TCPTestClient tcpclient;
    public Button DeleteInsert;

	// Use this for initialization

	void Start () {

        System.Net.ServicePointManager.ServerCertificateValidationCallback +=
              delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                      System.Security.Cryptography.X509Certificates.X509Chain chain,
                                      System.Net.Security.SslPolicyErrors sslPolicyErrors)
              {
                  return true; // **** Always accept
              };

        //m_Image = GetComponent<Image>();
        PickerEventListener.onImageSelect += OnImageSelect;
        PickerEventListener.onImageLoad += OnImageLoad;


        m_SpriteRenderer = GetComponent<SpriteRenderer>();

        var firstPermission = AndroidPermissionsManager.RequestPermission("android.permission.ACCESS_FINE_LOCATION");
        
        AddLocationButton.onClick.AddListener(WriteString);
        usethislocation.onClick.AddListener(LocationToString);
        DeleteInsert.onClick.AddListener(DeleteNewestInsert);

        //AddPhoto.onClick.AddListener(opengallery);

        
        firstPermission.WaitForCompletion();

        StartCoroutine(getLocation());

    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            WriteImage(LastOpenedImagePath);
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LataaVeneKuva();
        }

    }

    void DeleteNewestInsert()
    {
        Debug.Log("old delete neweset sql insertion");
    }



    /*void opengallery()
    {
        AndroidPicker.BrowseImage();
    }*/

    void OnImageSelect(string imgPath, ImageAndVideoPicker.ImageOrientation imgOrientation)
    {
        Debug.Log("Image Location : " + imgPath);

    }

    Texture2D duplicateTexture(Texture2D texture)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(texture, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(texture.width, texture.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }

    void OnImageLoad(string imgPath, Texture2D tex, ImageAndVideoPicker.ImageOrientation imgOrientation)
    {

        LastOpenedImagePath = imgPath;
        filename = Path.GetFileName(imgPath);
        ShowAddedLocation.text += filename;
        Debug.Log("Image Location : " + imgPath);
        texture = duplicateTexture(tex);

        mySprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
        m_Image.sprite = mySprite;


       
        //File.WriteAllBytes(Application.persistentDataPath + "/Downloads/MyImage.png", bytes);
        

    }



    void LocationToString()
    {
        
        gpsValuesInput.text = sLatitude.ToString() + ", " + sLongitude.ToString();
    }

    void WriteString()
    {
        if(NativeGallery.Permission.Granted == NativeGallery.CheckPermission())
        {
            ShowAddedLocation.text += "lupa on";
        }
        if (texture)
        {
            ShowAddedLocation.text += "ei kusi";
            try
            {
                NativeGallery.SaveImageToGallery(texture, "GalleryTest", filename);
            }
            catch(Exception E)
            {
                ShowAddedLocation.text += E;
            }

        }

        else
        {
            ShowAddedLocation.text += "kusi";
        }

        j++;
        InputValueString = gpsValuesInput.text;
        InputValueString = InputValueString.Replace("(", "");
        InputValueString = InputValueString.Replace(")", "");
        string path = "Assets/Resources/test.json";
        //string path = Application.persistentDataPath + "/text.json";

        JsonObject myObject = new JsonObject();
        myObject.id = 1;
        myObject.gps = InputValueString;
        myObject.imgName = filename;
        json = JsonUtility.ToJson(myObject);

        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(json);
        ShowAddedLocation.text += InputValueString + "\n";

       
        //tcpclient.SendMessage(json);
        tcpclient.SendJson(json);
        Debug.Log(json);

        writer.Close();
        gpsValuesInput.text = "";

    }
    
    string[] AskForKeys(string videoID)
    {
        tcpclient.SendProtocolCode(PROTOCOL_CODES.REQUEST_VIEW_VIDEO);
        if(tcpclient.GetRequest() == PROTOCOL_CODES.ACCEPT)
        {
            tcpclient.SendMessage(videoID);
            return tcpclient.ReceiveListString();
        }

        return null;
    }

    public bool MyRemoteCertificateValidationCallback(System.Object sender,
    X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        bool isOk = true;
        // If there are errors in the certificate chain,
        // look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
            for (int i = 0; i < chain.ChainStatus.Length; i++)
            {
                if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    continue;
                }
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                bool chainIsValid = chain.Build((X509Certificate2)certificate);
                if (!chainIsValid)
                {
                    isOk = false;
                    break;
                }
            }
        }
        return isOk;
    }
    public void LataaVeneKuva()
    {
        
        if (tcpclient.SendRequest(PROTOCOL_CODES.POST_EDITS) == PROTOCOL_CODES.ACCEPT)
        {

            int bytesToCome = tcpclient.reader.ReadInt32(); //read how many bytes are incoming
            tcpclient.SendProtocolCode(PROTOCOL_CODES.ACCEPT);
            //int bytesToCome = BitConverter.ToInt32(tcpclient.bytesFrom, 0);

            Debug.Log("incoming : " + bytesToCome);
            byte[] bytes = tcpclient.ReceiveBytes(bytesToCome);

            Debug.Log("accepted " + System.Text.Encoding.UTF8.GetString(bytes));
            Debug.Log("accepted as int" + bytes[0]);
            string url = System.Text.Encoding.UTF8.GetString(bytes);
            var cloudBlob = new CloudBlob(new System.Uri(url));
            MemoryStream memStream = new MemoryStream();
            //cloudBlob.DownloadToStream(memStream);
            cloudBlob.DownloadToFile("Assets/Resources/boatphoto.png", FileMode.OpenOrCreate);
        }
        else
        {
            Debug.Log("vituiks män");
        }

        

       
        
    }


    public Texture2D SendingTexture;
    void WriteImage(String path)
    {
        if(path != "")
        {
            Debug.Log(path);
            bytes = texture.EncodeToPNG();
        }

        bytes = SendingTexture.EncodeToPNG();
        var str = System.Text.Encoding.Default.GetString(bytes) + "\0";
        //tcpclient.SendImage(System.Text.Encoding.Default.GetBytes(str));
        tcpclient.SendImage(bytes);

    }


    [Serializable]
    private class JsonObject
    {
        public int id;
        public string gps;
        public string imgName;


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
            //text.text = " wait";
        }
        if (maxWait < 1)
        {

            yield break;
        }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            //text.text = "Unable to determine device location";
            yield break;
        }
        else
        {
            //text.text = "Target Location : " + latitudes[0] + ", " + longitudes[0] + "\nMy Location: " + Input.location.lastData.latitude + ", " + Input.location.lastData.longitude;
            sLatitude = Input.location.lastData.latitude;
            sLongitude = Input.location.lastData.longitude;
        }
        deviceCoordinates = new Vector2(sLatitude,sLongitude);
        //service.Stop();
        ready = true;

        //text.text += "wait" + sLatitude + sLongitude;
        yield return new WaitForSeconds(1);
        StartCoroutine(getLocation());

    }


}
