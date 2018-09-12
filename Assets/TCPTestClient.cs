using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Data;
using System.Data.SqlClient;
using Npgsql;
using NpgsqlTypes;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

public enum PROTOCOL_CODES
{
    ERROR = -1, ERROR_NO_DBCONNECTION
    , ACCEPT, DENY, SENDIMAGE, SENDVIDEO, SENDJSON, SENDLOCATION, QUIT, OK

    , GET_MY_CONTENTPACKS, SEARCH_CONTENT_PACKS, SEARCH_CONTENTPACKS_BY_USER
    , GET_SERIES_IN_PACKAGE, GET_VIDEOS_IN_SERIES
    , REQUEST_VIEW_VIDEO, REQUEST_EDIT_VIDEO
    , POST_EDITS, UPLOAD_VIDEO, UPLOAD_ASSETPACKAGE

    ,KEEPALIVE_SIGNAL

};



public enum STATUS
{
    ERROR = -1, RUNNING, ENDED, QUIT
};

public class TCPTestClient : MonoBehaviour
{
    #region private members 	
    private TcpClient serverSocket;
    private TcpClient pingSocketConnection;
    private Thread clientReceiveThread;
    private Thread clientKeepAliveThread;
    MemoryStream message = new MemoryStream();
    public NetworkStream stream;
    NetworkStream pingStream;
    public BinaryReader reader;
    public BinaryWriter writer;
    public Byte[] bytesFrom = new Byte[102400];
    NpgsqlConnection conn;
    public STATUS status = STATUS.RUNNING;

    Int32 lastPing = 0;
    private object cloudBlobContainer;
    string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=platformvideos;AccountKey=h6iS/e7UEIOXoLpd3UeECNXZhjOzVSvbdsn6QWs5+k0kJH/iBKZzxZFBJ41TTBZnkrtBC3WKOM2Xmp0ouBFXUg==;EndpointSuffix=core.windows.net";
    string policyName = "SimLabIT_Policy";

    #endregion
    // Use this for initialization 	
    void Start()
    {

        ConnectToTcpServer();
    }
    // Update is called once per frame
    void Update()
    {

    }

    public void OnApplicationQuit()
    {
        Debug.Log("exit");
        if (SendProtocolCode(PROTOCOL_CODES.QUIT))
        {
            pingSocketConnection.Close();
            serverSocket.Close();
        }
        else
        {
            SendProtocolCode(PROTOCOL_CODES.ERROR);
        }
    }

    IEnumerator WaitForSeconds()
    {
       // Debug.Log("before");
        yield return new WaitForSeconds(2);
        //Debug.Log("after");
        KeepAlive();
    }

    void KeepAlive()
    {
      //  Debug.Log("Sending ping");
        if (SendPing())
        {
            //WaitForSeconds();
            lastPing = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        else
        {
            SendProtocolCode(PROTOCOL_CODES.ERROR);
        }

    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer()
    {
        try
        {

            serverSocket = new TcpClient("127.0.0.1", 8052);
            pingSocketConnection = new TcpClient("127.0.0.1", 8051);
            stream = serverSocket.GetStream();
            pingStream = serverSocket.GetStream();
            //StartCoroutine(KeepAlive());

            clientKeepAliveThread = new Thread(new ThreadStart(KeepAlive));
            clientKeepAliveThread.IsBackground = true;
            clientKeepAliveThread.Start();

            /*
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
            */

            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }




   

    public void SendJson(String message)
    {
        Debug.Log("json sent");
        if (SendRequest(PROTOCOL_CODES.SENDJSON) == PROTOCOL_CODES.ACCEPT) SendBytes(Encoding.UTF8.GetBytes(message));
        else Debug.Log("Server did not accept");
    }

    public void SendImage(byte[] image)
    {
        Debug.Log("sending image");
        if (SendRequest(PROTOCOL_CODES.SENDIMAGE) == PROTOCOL_CODES.ACCEPT) {
            //CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(image);
            SendBytes(image);

        } 
        else Debug.Log("Server did not accept");
    }




    public byte[] ReceiveBytes(int lenght)
    {
        try
        {
            byte[] bytes;
            int received;
            int receivedSofar = 0;
            /*while (receivedSofar < lenght && (received = stream.Read(bytesFrom, 0, bytesFrom.Length)) > 0)
            {
                Debug.Log("received: " + received + " bytes.len:" + bytes.Length);
                Debug.Log("msg: " + Encoding.UTF8.GetString(bytesFrom, 0, received));
                Array.Copy(bytesFrom, 0, bytes, receivedSofar, received);
                receivedSofar += received;
                // Convert byte array to string message. 							
                string clientMessage = Encoding.UTF8.GetString(bytesFrom, 0, received);
                Console.WriteLine("received: " + received + " bytes");
            }*/

            bytes = reader.ReadBytes(lenght);
            Console.WriteLine("received full : " + receivedSofar + " bytes");
            return bytes;
        }
        catch (SocketException socketException)
        {
            Console.WriteLine("Socket exception: " + socketException);
            status = STATUS.ERROR;
            return null;
        }
    }

    bool SendPing()
    {
        if (pingSocketConnection == null)
        {

            return false;
        }
        try
        {
            // Get a stream object for writing. 			
            if (pingStream.CanWrite)
            {
                //byte[] message = BitConverter.GetBytes((int)PROTOCOL_CODES.KEEPALIVE_SIGNAL);

                pingStream.Read(bytesFrom, 0, 4); //read the replycode
                Debug.Log("received ping");
                return true;
            }
            status = STATUS.ERROR;
            return false;
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
            status = STATUS.ERROR;
            return false;
        }
    }


    public PROTOCOL_CODES GetRequest()
    {
        if (serverSocket == null)
        {
            return PROTOCOL_CODES.ERROR;
        }
        try
        {
             //read the replycode
            Int32 request = reader.ReadInt32();
            Console.WriteLine("Received request: " + ((PROTOCOL_CODES)request).ToString());
            return (PROTOCOL_CODES)request;
        }
        catch (Exception socketException)
        {
            Console.WriteLine("Socket exception: " + socketException);
            status = STATUS.ERROR;
            return PROTOCOL_CODES.ERROR;
        }
    }
    public PROTOCOL_CODES receiveProtocolCode()
    {
        if (serverSocket == null)
        {
            return PROTOCOL_CODES.ERROR;
        }
        try
        {
            //read the replycode
            Int32 request = reader.ReadInt32(); ;
            Console.WriteLine("Received request: " + ((PROTOCOL_CODES)request).ToString());
            return (PROTOCOL_CODES)request;
        }
        catch (Exception socketException)
        {
            Console.WriteLine("Socket exception: " + socketException);
           
            status = STATUS.ERROR;
            return PROTOCOL_CODES.ERROR;
        }
    }

    public bool SendProtocolCode(PROTOCOL_CODES code)
    {
        if (serverSocket == null)
        {

            return false;
        }
        try
        {
            // Get a stream object for writing. 			

            byte[] message = BitConverter.GetBytes((int)code);
            writer.Write(message, 0, 4); //read the replycode
            Console.WriteLine("Sent protocol code:" + ((PROTOCOL_CODES)code).ToString());
            return true;

            status = STATUS.ERROR;
            return false;
        }
        catch (Exception socketException)
        {
            Console.WriteLine("Socket exception: " + socketException);
            status = STATUS.ERROR;
            return false;
        }
    }

    public PROTOCOL_CODES SendRequest(PROTOCOL_CODES code)
    {
        if (serverSocket == null)
        {
            return PROTOCOL_CODES.ERROR;
        }
        try
        {
            // Get a stream object for writing. 			

            byte[] message = BitConverter.GetBytes((int)code);
            writer.Write((Int32)code);
            //read the replycode
            Int32 reply = reader.ReadInt32(); ;
            Console.WriteLine("Client sent request. Received reply:" + ((PROTOCOL_CODES)reply).ToString());
            return (PROTOCOL_CODES)reply;

        }
        catch (Exception socketException)
        {
            Console.WriteLine("Socket exception: " + socketException);
            status = STATUS.ERROR;
            return PROTOCOL_CODES.ERROR;
        }
        return PROTOCOL_CODES.ERROR;
    }


    public void SendBytes(Byte[] clientMessageAsByteArray)
    {
        if (serverSocket == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing. 			

            byte[] header = BitConverter.GetBytes(clientMessageAsByteArray.Length);
            writer.Write(header, 0, header.Length); //send the size of array


            //read the replycode
            Int32 reply = reader.ReadInt32();
            if (reply == (int)PROTOCOL_CODES.ACCEPT)
            {
                writer.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
            }
            else
            {
                Console.WriteLine("Server denied request to send something so large!");
                //TODO : handle not acccepting
            }
            Console.WriteLine("Client sent his message - should be received by server");
        }
        catch (Exception socketException)
        {
            status = STATUS.ERROR;
            Console.WriteLine("Socket exception: " + socketException);
        }
    }

    public void SendMessage(string msg)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
        SendBytes(buffer);
    }

    public string ReceiveMessage()
    {
        //stream.Read(bytesFrom, 0, 4); //read how many bytes are incoming
        int bytesToCome = (int) GetRequest();
        //int bytesToCome = BitConverter.ToInt32(bytesFrom, 0);

        Debug.Log("|||||||||||||||||||||||||||||| " + System.Text.Encoding.UTF8.GetString(ReceiveBytes(bytesToCome)));
        return System.Text.Encoding.UTF8.GetString(ReceiveBytes(bytesToCome));
    }

    public void SendListString(string[] obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(stream, obj);
    }
    public string[] ReceiveListString()
    {
        BinaryFormatter bf = new BinaryFormatter();
        return (string[])bf.Deserialize(stream);
    }


    public void SendArrayArrayString(string[][] obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(stream, obj);
    }



    public string[][] ReceiveArrayArrayString()
    {
        BinaryFormatter bf = new BinaryFormatter();
        return (string[][])bf.Deserialize(stream);
    }


    public bool UploadAssetPackage(string name, byte[] data)
    {
        if(SendRequest(PROTOCOL_CODES.UPLOAD_ASSETPACKAGE) == PROTOCOL_CODES.ACCEPT)
        {
            SendMessage(name);
            string url = ReceiveMessage();

            var cloudBlob = new CloudBlob(new System.Uri(url));
            
            MemoryStream memStream = new MemoryStream();
            //CloudBlockBlob cloudBlockBlob = cloudBlobContainer.(name);  //TÄMÄ KOODI KUTSUTAAN UNITY APPLIKAATIOSTA
            //cloudBlockBlob.UploadFromByteArray(bytesFrom, 0, 4);

            //ok at this point the file is uploaded

            SendProtocolCode(PROTOCOL_CODES.OK);
            PROTOCOL_CODES reply = receiveProtocolCode();
            if (reply == PROTOCOL_CODES.ERROR_NO_DBCONNECTION)
            {//TODO handling of error

            }
            if(reply == PROTOCOL_CODES.OK)
            {//party party all is fine
                return true;
            }


            return true;
        }
        return false;
    }




}