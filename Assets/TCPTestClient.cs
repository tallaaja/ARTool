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

public enum PROTOCOL_CODES
{
    ERROR = -1, ACCEPT, DENY, SENDIMAGE, SENDVIDEO, SENDJSON, SENDLOCATION, QUIT
};

public class TCPTestClient : MonoBehaviour
{
    #region private members 	
    private TcpClient socketConnection;
    //private Thread clientReceiveThread;
    MemoryStream message = new MemoryStream();
    NetworkStream stream;
    StreamReader reader;
    StreamWriter writer;
    Byte[] buffer = new Byte[1024];




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

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer()
    {
        try
        {
            NpgsqlConnection conn = new NpgsqlConnection("Server=23.100.5.134;Port=5432;User Id=postgres;Password=ilove1;Database=simlab;");
            conn.Open();

            using (NpgsqlCommand command = new NpgsqlCommand("select * from resourcetype", conn))
            {
                Debug.Log("sfd");
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    Debug.Log("reader");
                    while (reader.Read())
                    {
                        Debug.Log(reader.HasRows);
                        //Debug.Log("reading");
                        //Debug.Log(reader.ToString());
                        Debug.Log(reader["type"].ToString());
 
                    }
                }
            }

            /*string connectionString = "Server=23.100.5.134, 5432;" + // put the ip here!
            "Database=simlab;" +
            "User ID=postgres;" +
            "Password=ilove1;";
            SqlConnection dbcon = new SqlConnection(connectionString);
            dbcon.Open();
            Debug.Log(dbcon.State);*/


            //clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            //clientReceiveThread.IsBackground = true;
            // clientReceiveThread.Start();



        }
        catch (Exception e)
        {

        }
    }



    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incomming data. 	
    /// </summary>     
    private void ListenForData()
    {
        try
        {
            Byte[] bytes = new Byte[1024];
            Debug.Log("listening for server");
            while (true)
            {
                // Get a stream object for reading 				
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;
                    // Read incomming stream into byte arrary. 					
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        //var incommingData = new byte[length];
                        //Array.Copy(bytes, 0, incommingData, 0, length);

                        message.Write(bytes, 0, length);

                        // Convert byte array to string message. 						
                        string serverMessage = Encoding.ASCII.GetString(bytes, 0, length);
                        Debug.Log("server message received as: " + serverMessage);
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }
    /// <summary> 	
    /// Send message to server using socket connection. 	
    /// </summary> 	
    public void SendMessage(String clientMessage)
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
                
                // Convert string message to byte array.                 
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);       

                // Write byte array to socketConnection stream.                 
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                stream.Flush();
                Debug.Log("Client sent his message - should be received by server");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public void sendJson(String message)
    {
        PROTOCOL_CODES code = sendRequest(PROTOCOL_CODES.SENDJSON);
        Debug.Log(code.ToString());
        if (code == PROTOCOL_CODES.ACCEPT) SendBytes(Encoding.ASCII.GetBytes(message));
        else Debug.Log("Server did not accept");
    }


    public PROTOCOL_CODES sendRequest(PROTOCOL_CODES code)
    {
        if (socketConnection == null)
        {
            return PROTOCOL_CODES.ERROR;
        }
        try
        {
            // Get a stream object for writing. 			
            if (stream.CanWrite)
            {
                byte[] message = BitConverter.GetBytes((int)code);
                stream.Write(message, 0, 4);
                stream.Flush();
                Debug.Log("awating server reply.");
                stream.Read(buffer, 0, 4); //read the replycode

                Int32 reply = BitConverter.ToInt32(buffer, 0);      
                Debug.Log("Client sent request. Received reply:" + ((PROTOCOL_CODES)reply).ToString());
                return (PROTOCOL_CODES)reply;
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
            return PROTOCOL_CODES.ERROR;
        }
        Debug.Log("could not write");
        return PROTOCOL_CODES.ERROR;

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

    public void sendImage(byte[] image)
    {
        PROTOCOL_CODES code = sendRequest(PROTOCOL_CODES.SENDIMAGE);
        Debug.Log(code.ToString());
        if (code == PROTOCOL_CODES.ACCEPT) SendBytes(image);
        else Debug.Log("Server did not accept");
    }

    /*public void SendImage(byte[] image)
    {
        if (socketConnection == null)
        { 
            return;
        }
        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                SendMessage(image.Length.ToString());
                //string clientMessage = LocationManager.json;
                // Convert string message to byte array. 
                stream.Write(image, 0, image.Length);
                // Write byte array to socketConnection stream.                 
                //stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                Debug.Log("Client sent his image - should be received by server");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }*/
}