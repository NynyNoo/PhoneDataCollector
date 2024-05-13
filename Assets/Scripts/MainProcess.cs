using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
public class MainProcess : MonoBehaviour
{
    [SerializeField] TMP_InputField textAddress;
    [SerializeField] TMP_InputField textPort;
    [SerializeField] TMP_InputField textFreqency;
    [SerializeField] TextMeshProUGUI textStatus;
    private UdpClient udpClient;
    Vector3 angularVelocity = new(0, 0, 0);
    Vector3 acceleration = new(0, 0, 0);
    Quaternion attitudeSensor = new(0, 0, 0, 0);
    private bool isSendingData = false;
    private Thread sendingThread;
    string format = "H:mm:ss.fff";
    UnityEngine.Gyroscope gyr;

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        InputSystem.EnableDevice(Gyroscope.current);
        InputSystem.EnableDevice(Accelerometer.current);
        InputSystem.EnableDevice(AttitudeSensor.current);
    }
    private void FixedUpdate()
    {
        InputSystem.Update();
        angularVelocity = Gyroscope.current.angularVelocity.ReadValue();
        acceleration = Accelerometer.current.acceleration.ReadValue();
        attitudeSensor = AttitudeSensor.current.attitude.ReadValue();
    }
    public void ConnectButton_Clicked()
    {
        if (!isSendingData)
        {
            try
            {
                string ipAddress = textAddress.text;
                string portText = textPort.text;

                int port = int.Parse(portText);
                bool isConnected = ConnectToServer(ipAddress, port);


                if (isConnected)
                {
                    textStatus.text = "Po��czono z serwerem";
                    isSendingData = true;
                    StartSendingData();
                }
                else
                {
                    textStatus.text = "B��d po��czenia z serwerem";
                }
            }
            catch (Exception ex)
            {
                textStatus.text = $"B��d: Error 001";
            }
        }
    }
    public void DisconnectButton_Clicked()
    {
        try
        {
            StopSendingData();
            DisconnectFromServer();
            isSendingData = false;
            textStatus.text = "Roz��czono z serwerem";
        }
        catch (Exception ex)
        {
            textStatus.text = $"B��d podczas roz��czania: {ex.Message}";
        }
    }
    private void StopSendingData()
    {
        isSendingData = false;
        sendingThread?.Join(1000);
    }
    public bool ConnectToServer(string ipAddress, int port)
    {
        try
        {
            udpClient = new UdpClient(ipAddress, port);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public void SendDataToServer(string data)
    {
        if (udpClient == null)
            throw new InvalidOperationException("Not connected to the server.");

        byte[] byteData = Encoding.UTF8.GetBytes(data);
        udpClient.Send(byteData, byteData.Length);
    }

    public void DisconnectFromServer()
    {
        udpClient?.Close();
    }
    private void StartSendingData()
    {
        isSendingData = true;
        Debug.Log(isSendingData);
        sendingThread = new Thread(SendDataLoop);
        sendingThread.Start();
    }
    private void SendDataLoop()
    {
        int freq = int.Parse(textFreqency.text);
        while (isSendingData)
        {
            try
            {
                SendDataToServer(converter());
                Thread.Sleep(freq);
            }
            catch (Exception ex)
            {
                textStatus.text += ex.Message;
            }
        }
    }
    string converter()
    {
        string data;
        data =
            "X: " + acceleration.x + ", Y: " + acceleration.y + ", Z: " + acceleration.z + ";" +
            " X: " + angularVelocity.x + ", Y: " + angularVelocity.y + ", Z: " + angularVelocity.z + ";" +
            " X: " + attitudeSensor.x + ", Y: " + attitudeSensor.y + ", Z: " + attitudeSensor.z + ", W: " + attitudeSensor.w + ";" +
            " " + DateTime.Now.ToString(format);
        return data;
    }
}
