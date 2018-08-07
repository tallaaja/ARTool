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

public enum PROTOCOL_CODES
{
    ERROR = -1, ERROR_NO_DBCONNECTION
    , ACCEPT, DENY, SENDIMAGE, SENDVIDEO, SENDJSON, SENDLOCATION, QUIT

    , GET_MY_CONTENTPACKS, SEARCH_CONTENT_PACKS, SEARCH_CONTENTPACKS_BY_USER
    , GET_SERIES_IN_PACKAGE, GET_VIDEOS_IN_SERIES
    , REQUEST_VIEW_VIDEO, REQUEST_EDIT_VIDEO
    , POST_EDITS, UPLOAD_VIDEO

    ,KEEPALIVE_SIGNAL

};



public enum STATUS
{
    ERROR = -1, RUNNING, ENDED, QUIT
};

public class TCPTestClient : MonoBehaviour
{
    #region private members 	
    private TcpClient socketConnection;
    private TcpClient pingSocketConnection;
    private Thread clientReceiveThread;
    private Thread clientKeepAliveThread;
    MemoryStream message = new MemoryStream();
    NetworkStream stream;
    NetworkStream pingStream;
    StreamReader reader;
    StreamWriter writer;
    Byte[] buffer = new Byte[1024];
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
        if (sendProtocolCode(PROTOCOL_CODES.QUIT))
        {
            pingSocketConnection.Close();
            socketConnection.Close();
        }
        else
        {
            sendProtocolCode(PROTOCOL_CODES.ERROR);
        }
    }

    IEnumerator WaitForSeconds()
    {
        Debug.Log("before");
        yield return new WaitForSeconds(2);
        Debug.Log("after");
        KeepAlive();
    }

    void KeepAlive()
    {
        Debug.Log("Sending ping");
        if (sendPing())
        {
            //WaitForSeconds();
            lastPing = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        else
        {
            sendProtocolCode(PROTOCOL_CODES.ERROR);
        }

    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer()
    {
        try
        {

            socketConnection = new TcpClient("127.0.0.1", 8052);
            pingSocketConnection = new TcpClient("127.0.0.1", 8051);
            stream = socketConnection.GetStream();
            pingStream = socketConnection.GetStream();
            //StartCoroutine(KeepAlive());

            clientKeepAliveThread = new Thread(new ThreadStart(KeepAlive));
            clientKeepAliveThread.IsBackground = true;
            clientKeepAliveThread.Start();
            
            /*
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
            */
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

   

    public void sendJson(String message)
    {
        Debug.Log("json sent");
        if (sendProtocolCode(PROTOCOL_CODES.SENDJSON)) SendBytes(Encoding.ASCII.GetBytes(message));
        else Debug.Log("Server did not accept");
    }
    public void sendImage(byte[] image)
    {
        Debug.Log("sending image");
        if (sendProtocolCode(PROTOCOL_CODES.SENDIMAGE)) {
            //CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(image);
            //SendBytes(image);

        } 
        else Debug.Log("Server did not accept");
    }




    byte[] receiveBytes(int lenght)
    {
        try
        {
            byte[] bytes = new byte[lenght];
            int received;
            int receivedSofar = 0;
            while (receivedSofar < lenght && (received = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                Array.Copy(buffer, 0, bytes, receivedSofar, received);
                receivedSofar += received;
                // Convert byte array to string message. 							
                string clientMessage = Encoding.ASCII.GetString(buffer, 0, received);
                Console.WriteLine("received: " + received + " bytes");
            }
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

    bool sendPing()
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

                pingStream.Read(buffer, 0, 4); //read the replycode
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


    bool sendProtocolCode(PROTOCOL_CODES code)
    {
        if (socketConnection == null)
        {

            return false;
        }
        try
        {
            // Get a stream object for writing. 			
            if (stream.CanWrite)
            {
                byte[] message = BitConverter.GetBytes((int)code);
                stream.Write(message, 0, 4); //read the replycode
                Console.WriteLine("Sent protocol code:" + ((PROTOCOL_CODES)code).ToString());
                return true;
            }
            status = STATUS.ERROR;
            return false;
        }
        catch (SocketException socketException)
        {
            Console.WriteLine("Socket exception: " + socketException);
            status = STATUS.ERROR;
            return false;
        }
    }


    public void sendListString(string[][] obj)
    {
        int len = obj.Length;
        byte[] bytes = new byte[len];

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(stream, obj);
        stream.Flush();
    }

    public string[][] receiveListString()
    {
        BinaryFormatter bf = new BinaryFormatter();
        return (string[][])bf.Deserialize(stream);
    }







    public void SendBytes(Byte[] clientMessageAsByteArray)
    {
        if (socketConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing. 			
            if (stream.CanWrite)
            {            
                byte[] header = BitConverter.GetBytes(clientMessageAsByteArray.Length);
                stream.Write(header, 0, header.Length); //send the size of array
                stream.Flush();
                Debug.Log("halutaan lähettää näin pitkä: " + clientMessageAsByteArray.Length);

                stream.Read(buffer, 0, 4); //read the replycode
                Int32 reply = BitConverter.ToInt32(buffer, 0);
                if (reply == (int)PROTOCOL_CODES.ACCEPT)
                {
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                    stream.Flush();
                }
                else
                {
                    Debug.Log("Server denied request with: " + ((PROTOCOL_CODES)reply).ToString() + " !");
                    //TODO : handle not acccepting
                }
                Debug.Log("Client sent his message - should be received by server");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
        //Debug.Log("could not write");
    }



   
}