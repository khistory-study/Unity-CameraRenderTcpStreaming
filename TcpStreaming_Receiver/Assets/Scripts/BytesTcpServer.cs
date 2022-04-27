using System;
using System.Collections.Generic;
using System.Linq;
using System.Net; 
using System.Net.Sockets; 
using UnityEngine;  

public class BytesTcpServer : MonoBehaviour {
	public float delayAvg;
	public bool isConnected = false;

	private readonly int messageByteLength = 24;
	
	private byte[] _recvBytes;
	private int _port = 56666;
	
	private TcpListener _tcpListener; 
	private TcpClient _connectedClient;
	private Queue<float> _readDelayQueue = new Queue<float>();
	private int _readDelayCheckCountMax = 10;

	private Action<byte[]> _onBytesRecv;
	private Action<bool> _onConnected;
	
	private void Awake()
	{
		Loom.Initialize();
	}

	public void BeginServer(int port, Action<byte[]> bytesRecvAction = null, Action<bool> onConnected = null)
	{
		_port = port;
		_onBytesRecv = bytesRecvAction;
		_onConnected = onConnected;
		EndServer();
		Loom.RunAsync(ReadLoop);
	}

	private void ReadLoop () { 		
		Debug.Log($"◆◇◇(1)미러링TCP - ReadLoop Start (port:{_port})");
		try {
			_tcpListener = new TcpListener(IPAddress.Any, _port); 			
			_tcpListener.Start();     

			while (true)
			{
				Debug.Log($"◆◆◇(2)미러링TCP - 클라이언트 접속을 대기 중...(port:{_port})");
				isConnected = false;
				_onConnected?.Invoke(false);
				_connectedClient = _tcpListener.AcceptTcpClient();
				isConnected = true;
				_onConnected?.Invoke(true);
				Debug.Log($"◆◆◆(3)미러링TCP - 클라이언트 연결 완료...(port:{_port})");

				while (true)
				{
					try
					{
						float streamReadTime = 0;
						Loom.QueueOnMainThread(() => streamReadTime = Time.time );
							
						//Read Image Count
						int imageSize = ReadImageByteSize(messageByteLength, _connectedClient);
						if (imageSize == -1)
							break;
						
						//Read Image Bytes and Display it
						ReadFrameByteArray(imageSize, _connectedClient);
					
						//Read Write Interval
						float clientFpsInterval = ReadSendInterval(4, _connectedClient);
						
						Loom.QueueOnMainThread(() =>
						{
							delayAvg = CalculateDelayValue(streamReadTime, clientFpsInterval);
							SendDelayValueToClient(delayAvg);
						});
					}
					catch (Exception e)
					{
						Debug.Log($"◇◇◇(0)미러링TCP - 연결이 끊기거나, Data를 못 받은 상태(detail:{e}))");
						break;
					}
				}
			} 		
		} 		
		catch (SocketException socketException) { 			
			Debug.Log("◇◇◇(-1)미러링TCP - SocketException " + socketException);
		}     
	}

	private float CalculateDelayValue(float streamReadTime, float sendInterval)
	{
		float ret = 0;
		
		_readDelayQueue.Enqueue(Time.time - streamReadTime);
		if (_readDelayQueue.Count > _readDelayCheckCountMax)
			_readDelayQueue.Dequeue();
		ret = _readDelayQueue.Average();
		ret = Mathf.Clamp((float)Math.Round(ret, 4) - sendInterval, 0, 10);
		
		return ret;
	}

	private void SendDelayValueToClient(float delay)
	{
		if (!isConnected)
			return;
			
		NetworkStream stream = _connectedClient.GetStream();
		byte[] delayBytes = BitConverter.GetBytes(delay);
		stream.Write(delayBytes, 0, delayBytes.Length);
	}
	
	private int ReadImageByteSize(int size, TcpClient client) {
		bool disconnected = false;

		NetworkStream serverStream = client.GetStream();
		
		byte[] imageBytesCount = new byte[size];
		var total = 0;
		
		do {
			var read = serverStream.Read(imageBytesCount, total, size - total);
			if (read == 0)
			{
				disconnected = true;
				break;
			}
			total += read;
		} while (total != size);
		
		int byteLength;

		if (disconnected) {
			byteLength = -1;
		} else {
			byteLength = FrameByteArrayToByteLength(imageBytesCount);
		}
		return byteLength;
	}
	
	private void ReadFrameByteArray(int size, TcpClient client)
	{
		if (size < 100) return;
		
		bool disconnected = false;

		NetworkStream serverStream = client.GetStream();

		byte[] imageBytes = new byte[size];
		var total = 0;
		do {
			var read = serverStream.Read(imageBytes, total, size - total); 
			if (read == 0)
			{
				disconnected = true;
				break;
			}
			total += read;
		} while (total != size);

		//Display Image
		if (!disconnected) {
			_recvBytes = imageBytes;
			Loom.QueueOnMainThread(() => _onBytesRecv?.Invoke(_recvBytes));
		}
	}
	
	private float ReadSendInterval(int size, TcpClient client)
	{
		bool disconnected = false;
		NetworkStream serverStream = client.GetStream();
		byte[] imageBytes = new byte[size];
		
		var total = 0;
		do {
			var read = serverStream.Read(imageBytes, total, size - total); 
			if (read == 0)
			{
				disconnected = true;
				break;
			}
			total += read;
		} while (total != size);

		if (disconnected)
			return 0;
		else
			return BitConverter.ToSingle(imageBytes, 0);
	}

	//Converts the byte array to the data size and returns the result
	int FrameByteArrayToByteLength(byte[] frameBytesLength) {
		int byteLength = BitConverter.ToInt32(frameBytesLength, 0);
		return byteLength;
	}

	private void EndServer()
	{
		try
		{
			_connectedClient?.Close();
			_tcpListener?.Stop();
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}
	}
	
	private void OnApplicationQuit()
	{
		EndServer();
	}
}