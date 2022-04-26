using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 아래 링크 코드를 참고하였음
/// 1. https://code-examples.net/ko/q/28bd211
/// </summary>
public class BytesTcpClient : MonoBehaviour {  	
	private readonly int _messageByteLength = 24;

	private string _ip = "127.0.0.1";
	private int _port = 56666;
	private int _connectTryInterval = 3;

	private byte[] _sendBytes;

	private TcpClient _tcpClient;
	private bool _isConnected;

	private void Awake()
	{
		Loom.Initialize();
	}

	private void OnEnable()
	{
		StartCoroutine(StartTryConnectLoop(_connectTryInterval));
		StartCoroutine(SendLoop());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	public void SetBytes(byte[] bytes) {         
		if (_tcpClient == null)            
			return;         

		if (!_isConnected)
			return;
		
		if(bytes == null) 
			return;
		
		_sendBytes = bytes;
	}

	public void ConnectToServer(string targetIp, int port)
	{
		_ip = targetIp;
		_port = port;

		if (string.IsNullOrEmpty(_ip))
			return;
		
		QuitClient();
		Debug.Log($"◆◇◇(1)미러링TCP - ThreadStart ((info)ip:{_ip}/port:{_port}))");
		Loom.RunAsync(ConnectAndWait);
	}  	
	
	private IEnumerator StartTryConnectLoop(int interval)
	{
		while (true)
		{
			yield return new WaitForSeconds(interval);
			
			if (!_isConnected)
				ConnectToServer(_ip, _port);
		}
	}

	IEnumerator SendLoop() {
	    bool readyToGetFrame = true;
        byte[] frameBytesLength = new byte[_messageByteLength];

        while (true)
        {
	        yield return null;
	        
	        if(!_isConnected)
		        continue;
	        
            if (_sendBytes == null || _sendBytes.Length == 0)
	            continue;
            
            byte[] sendBytes = _sendBytes; 
            
            //Fill total byte length to send. Result is stored in frameBytesLength
            ByteLengthToFrameByteArray(sendBytes.Length, frameBytesLength);

            //Set readyToGetFrame false
            readyToGetFrame = false;

            Loom.RunAsync(() => {
	            try
	            {
		            NetworkStream stream = _tcpClient.GetStream();
		            //Send total byte count first
		            stream.Write(frameBytesLength, 0, frameBytesLength.Length);
		            //Send the image bytes
		            stream.Write(sendBytes, 0, sendBytes.Length);
		            //clear sendBytes
		            _sendBytes = null;
		            //Sent. Set readyToGetFrame true
		            readyToGetFrame = true;
	            }
	            catch (Exception e)
	            {
		            _isConnected = false;
		            Debug.Log("◇◇◇(-1)미러링TCP - Exception " + e);
	            }
	            finally
	            {
		            readyToGetFrame = true;
	            }
            });

            //Wait until we are ready to get new frame(Until we are done sending data)
            while (!readyToGetFrame) {
                yield return null;
            }
        }
    }
	
	private void ReadLoop()
	{
		while (true)
		{
			if (_isConnected) return;
        
        
			NetworkStream serverStream = _tcpClient.GetStream();
			byte[] imageBytesCount = new byte[4];
		}
	}
	
    
    void ByteLengthToFrameByteArray(int byteLength, byte[] fullBytes) {
	    Array.Clear(fullBytes, 0, fullBytes.Length);
	    byte[] bytesToSendCount = BitConverter.GetBytes(byteLength);
	    bytesToSendCount.CopyTo(fullBytes, 0);
    }

    private void ConnectAndWait() {
		try { 			
			_isConnected = false;
			_tcpClient = new TcpClient();
			Debug.Log($"◆◆◇(2)미러링TCP - 서버 접속 대기... ((info)ip:{_ip}/port:{_port}))");
			_tcpClient.Connect(IPAddress.Parse(_ip), _port);
			Debug.Log($"◆◆◆(3)미러링TCP - 서버 연결 완료... ((info)ip:{_ip}/port:{_port})");
			_isConnected = true;
		}         
		catch (SocketException socketException) {             
			Debug.Log("◇◇◇(-1)미러링TCP - SocketException " + socketException);
		}     
	}

	private void QuitClient()
	{
		try
		{
			_tcpClient?.Close();
		} 		
		catch (Exception e) { 	
			Debug.Log("◇◇◇(-1)미러링TCP - Exception " + e);
		} 
	}

	private void OnApplicationQuit()
	{
		QuitClient();
	}
}