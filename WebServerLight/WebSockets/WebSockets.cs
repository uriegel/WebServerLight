// using System;
// using System.Collections.Generic;
// using System.Text;

// namespace Caseris.Http.WebSockets
// {
// 	record Extensions(bool PerMessageDeflate, bool WatchDog);
// }


// using Caseris.Http.Interfaces;
// using System;
// using System.Net;
// using System.Threading.Tasks;

// namespace Caseris.Http.WebSockets
// {
// 	class WebSocket : IWebSocket
//     {
//         #region IWebSocket

//         public string[] Protocols { get => webSocketSession.Protocols; }

// 		public bool IsSecureConnection { get => webSocketSession.IsSecureConnection; }
// 		public IPEndPoint? LocalEndPoint { get => webSocketSession.LocalEndPoint; }
// 		public IPEndPoint? RemoteEndPoint { get => webSocketSession.RemoteEndPoint; }
// 		public string? UserAgent { get => webSocketSession.UserAgent; }

// 		public Task SendAsync(string payload) => webSocketSession.SendAsync(payload);

//         public Task SendJsonAsync(object jsonObject) => webSocketSession.SendJsonAsync(jsonObject);

//         public void Initialize(Func<string, Task> onMessage, Func<Task> onClosed)
//         {
//             webSocketSession.Initialize(onMessage, onClosed);
//             webSocketSession.StartMessageReceiving();
//         }

// 		public void Close()
// 			=> webSocketSession.Close();
//         #endregion

//         public WebSocket(WebSocketSession webSocketSession) => this.webSocketSession = webSocketSession;

//         readonly WebSocketSession webSocketSession;
//     }
// }



// using Caseris.Http.Interfaces;

// using System;
// using System.Threading.Tasks;

// namespace Caseris.Http.WebSockets
// {
// 	class WebSocketSession : WebSocketSessionBase
//     {
//         public WebSocketSession(RequestSession session, IServer server, Configuration configuration, bool deflate) 
//             : base(session, server, configuration, deflate)
//         {
//         }

//         public void Initialize(Func<string, Task> onMessage, Func<Task> onClosed)
//         {
//             this.onMessage = onMessage;
//             this.onClosed = onClosed;
//         }

//         protected override Task OnMessage(string payload) => onMessage(payload);

//         protected override async Task OnClose() => await onClosed();

//         Func<string, Task> onMessage = s => Task.FromResult(0);
//         Func<Task> onClosed = () => Task.FromResult(0);
//     }
// }


// using Caseris.Http.Interfaces;
// using System;
// using System.IO;
// using System.IO.Compression;
// using System.Net;
// using System.Runtime.Serialization.Json;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;

// namespace Caseris.Http.WebSockets
// {
// 	abstract class WebSocketSessionBase : IWebSocketSender, IWebSocketInternalSession
//     {
//         public static Counter Instances { get; } = new Counter();

//         public string[] Protocols { get => protocols; }
//         readonly string[] protocols;

//         public int Id { get => id; }
//         readonly int id;

// 		public bool IsSecureConnection { get => session?.IsSecureConnection ?? false; }
// 		public IPEndPoint? LocalEndPoint { get => session?.LocalEndPoint; }
// 		public IPEndPoint? RemoteEndPoint { get => session?.RemoteEndPoint; }

// 		public string? UserAgent { get => session?.Headers?.UserAgent;  }

//         public void Send(string payload)
//         {
//             var memStm = new MemoryStream(Encoding.UTF8.GetBytes(payload));
//             WriteStream(memStm);
//         }

//         public void SendJson(object jsonObject)
//         {
//             var type = jsonObject.GetType();
//             var jason = new DataContractJsonSerializer(type);
//             var memStm = new MemoryStream();
//             jason.WriteObject(memStm, jsonObject);
//             memStm.Position = 0;
//             WriteStream(memStm);
//         }

//         public Task SendAsync(string payload)
//         {
//             var buffer = Encoding.UTF8.GetBytes(payload);
//             var memStm = new MemoryStream(buffer, 0, buffer.Length, false, true);
//             return WriteStreamAsync(memStm);
//         }

//         public Task SendJsonAsync(object jsonObject)
//         {
//             var type = jsonObject.GetType();
//             var jason = new DataContractJsonSerializer(type);
//             var memStm = new MemoryStream();
//             jason.WriteObject(memStm, jsonObject);
//             memStm.Position = 0;
//             return WriteStreamAsync(memStm);
//         }

//         protected virtual void IncrementCounter() => Instances.Increment();

//         protected virtual void  DecrementCounter() => Instances.Decrement();

//         protected virtual void DecrementActiveCounter()
//         {
//             if (!isDisposed)
//             {
//                 isDisposed = true;
//                 Instances.DecrementActive();
//             }
//         }

//         protected virtual void AddBytes(long bytes) => Instances.AddBytes(bytes);

//         #region Constructor	

//         public WebSocketSessionBase(RequestSession session, IServer server, Configuration configuration, bool deflate)
//         {
//             IncrementCounter();
//             useDeflate = deflate;
//             id = Interlocked.Increment(ref sessionIDCreator);
//             this.session = session;
//             this.server = server;
//             this.configuration = configuration;
//             host = session.Headers.Host;
//             protocols = session.Headers["sec-websocket-protocol"]?.Split(new[] { ',' }) ?? new string[0];
//             networkStream = session.GetNetworkStream();
//         }

//         ~WebSocketSessionBase() => DecrementCounter();

//         #endregion

//         #region Methods	

//         public async void StartMessageReceiving()
//         {
//             var wsr = new WebSocketReceiver(networkStream);

//             async Task action(WsDecodedStream? wsDecodedStream, Exception? exception)
//             {
//                 try
//                 {
//                     if (exception != null)
//                     {
//                         if (exception != null && !(exception is ConnectionClosedException))
//                             Logger.Current.Warning($"Exception in WebSocket Receiving: {exception}");
//                         if (exception is ConnectionClosedException)
//                         {
//                             Logger.Current.LowTrace(() => "Connection closed");
//                             await DoCloseAsync();
//                         }
//                     }
//                     else
//                     {
//                         var payload = wsDecodedStream?.Payload;
//                         try
//                         {
//                             await OnMessage(payload ?? "");
//                         }
//                         catch (Exception e)
//                         {
//                             Logger.Current.Warning($"Error in OnMessage while processing web socket request: {e}");
//                         }
//                     }
//                 }
//                 catch (Exception e)
//                 {
//                     Logger.Current.Warning($"Exception occurred while processing web socket request: {e}");
//                     await DoCloseAsync();
//                 }
//             }

//             while (true)
//                 if (!await wsr.StartMessageReceiving(action, this))
//                     break;
//         }

//         public async void Close()
//         {
//             try
//             {
//                 await DoCloseAsync();
//             }
//             catch { }
//         }

//         public void SendPong(string payload)
//         {
//             var bytes = Encoding.UTF8.GetBytes(payload);
//             var stream = new MemoryStream(bytes, 0, bytes.Length, false, true);
//             WriteStream(stream, OpCode.Pong);
//         }

//         async Task DoCloseAsync()
//         {
//             if (isClosed)
//                 return;
//             DecrementActiveCounter();
//             isClosed = true;
//             try
//             {
//                 networkStream.Close();
//             }
//             catch { }

//             await OnClose();
//         }

//         protected virtual Task OnClose() => Task.FromResult(0);
//         protected abstract Task OnMessage(string payload);

//         void WriteStream(MemoryStream payloadStream, OpCode? opCode = null)
//         {
//             try
//             {
//                 var (buffer, deflate) = GetPayload(payloadStream);
//                 var header = WriteHeader(buffer.Length, deflate, opCode);
//                 semaphoreSlim.Wait();
//                 try
//                 {
//                     networkStream.Write(header, 0, header.Length);
//                     networkStream.Write(buffer, 0, buffer.Length);
//                     AddBytes(header.Length + buffer.Length);
//                 }
//                 catch
//                 {
//                     try
//                     {
//                         networkStream.Close();
//                     }
//                     catch { }
//                 }
//                 finally
//                 {
//                     semaphoreSlim.Release();
//                 }
//             }
// 			catch (ConnectionClosedException)
//             {
//             }
//         }

//         async Task WriteStreamAsync(MemoryStream payloadStream)
//         {
//             var (buffer, deflate) = GetPayload(payloadStream);
//             var header = WriteHeader(buffer.Length, deflate);
//             await semaphoreSlim.WaitAsync();
//             try
//             {
//                 networkStream.Write(header, 0, header.Length);
//                 networkStream.Write(buffer, 0, buffer.Length);
//                 AddBytes(header.Length + buffer.Length);
//             }
//             catch
//             {
//                 try
//                 {
//                     networkStream.Close();
//                 }
//                 catch { }
//                 throw;
//             }
//             finally
//             {
//                 semaphoreSlim.Release();
//             }
//         }

//         (byte[] buffer, bool deflate) GetPayload(MemoryStream payloadStream)
//         {
//             var deflate = useDeflate && payloadStream.Length > configuration.MinSizeForDeflate;
//             if (deflate)
//             {
//                 var ms = new MemoryStream();
//                 var compressedStream = new DeflateStream(ms, CompressionMode.Compress, true);
//                 payloadStream.CopyTo(compressedStream);
//                 compressedStream.Close();
//                 ms.WriteByte(0); // BFinal!
//                 payloadStream = ms;
//             }

//             payloadStream.Capacity = (int)payloadStream.Length;
//             return (payloadStream.GetBuffer(), deflate);
//         }

//         /// <summary>
//         /// Schreibt den WebSocketHeader
//         /// </summary>
//         /// <param name="payloadLength"></param>
//         /// <param name="deflate"></param>
//         /// <param name="opcode"></param>
//         byte[] WriteHeader(int payloadLength, bool deflate, OpCode? opcode = null)
//         {
//             if (opcode == null)
//                 opcode = OpCode.Text;
//             var length = payloadLength;
//             var FRRROPCODE = (byte)((deflate ? 0xC0 : 0x80) + (byte)(int)opcode.Value); //'FIN is set, and OPCODE is 1 (Text) or opCode

//             int headerLength;
//             if (length <= 125)
//                 headerLength = 2;
//             else if (length <= ushort.MaxValue)
//                 headerLength = 4;
//             else
//                 headerLength = 10;
//             var buffer = new byte[headerLength];
//             if (length <= 125)
//             {
//                 buffer[0] = FRRROPCODE;
//                 buffer[1] = Convert.ToByte(length);
//             }
//             else if (length <= ushort.MaxValue)
//             {
//                 buffer[0] = FRRROPCODE;
//                 buffer[1] = 126;
//                 var sl = (ushort)length;
//                 var byteArray = BitConverter.GetBytes(sl);
//                 var eins = byteArray[0];
//                 buffer[2] = byteArray[1];
//                 buffer[3] = eins;
//             }
//             else
//             {
//                 buffer[0] = FRRROPCODE;
//                 buffer[1] = 127;
//                 var byteArray = BitConverter.GetBytes((ulong)length);
//                 var eins = byteArray[0];
//                 var zwei = byteArray[1];
//                 var drei = byteArray[2];
//                 var vier = byteArray[3];
//                 var fünf = byteArray[4];
//                 var sechs = byteArray[5];
//                 var sieben = byteArray[6];
//                 buffer[2] = byteArray[7];
//                 buffer[3] = sieben;
//                 buffer[4] = sechs;
//                 buffer[5] = fünf;
//                 buffer[6] = vier;
//                 buffer[7] = drei;
//                 buffer[8] = zwei;
//                 buffer[9] = eins;
//             }
//             return buffer;
//         }

//         #endregion

//         #region Fields	

//         static int sessionIDCreator;
//         readonly RequestSession session;
//         readonly IServer server;
//         readonly Configuration configuration;
//         readonly Stream networkStream;
//         readonly string host;
//         readonly object locker = new object();
// 		readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
//         readonly bool useDeflate;
//         bool isClosed;
//         bool isDisposed;

//         #endregion
//     }
// }


		// internal async Task<WebSocketSessionBase> UpgradeWebSocketAsync(Extension extension, IWebSockets webSockets)
        // {
        //     var extensions = await UpgradeToWebSocketAsync();
        //     var webSocketSession = new WebSocketSynchronousSession(this, Server, ServerInstance.Configuration, webSockets, extensions.PerMessageDeflate);
        //     webSocketSession.StartMessageReceiving();
        //     return webSocketSession;
        // }


                // if (session.CheckWsUpgrade())
                // {
                //     if (extension is IWebSocketsConsumer webSocketsConsumer)
                //     {
                //         var webSocketSession = session.UpgradeWebSocketAsync(false).Synchronize();
                //         var webSocket = new WebSocket(webSocketSession);
                //         webSocketsConsumer.OnNew(webSocket, query);
                //     }
                //     else if (extension is IWebSockets webSockets)
                //     {
                //         var webSocketSession = session.UpgradeWebSocketAsync(this, webSockets).Synchronize();
                //         webSockets.Initialize(webSocketSession, query);
                //     }
                //     return false;
                // }



// namespace Caseris.Http.WebSockets
// {
// 	public enum OpCode : byte
// 	{
// 		/// <summary>
// 		/// Diese Nachricht muss an die vorherige angehängt werden. Wenn der fin-Wert 0 ist, folgen weitere Fragmente, 
// 		/// bei fin=1 ist die Nachricht komplett verarbeitet.
// 		/// </summary>
// 		ContinuationFrame = 0,
// 		Text,
// 		Binary,
// 		Close = 8,
// 		/// <summary>
// 		/// Ping erhalten, direkt einen Pong zurücksenden mit denselben payload-Daten
// 		/// </summary>
// 		Ping,
// 		/// <summary>
// 		/// Wird serverseitig ignoriert
// 		/// </summary>
// 		Pong
// 	}
// }

// namespace Caseris.Http.WebSockets;

// public class Settings
// {
// 	public ProtocolExtensions? Extensions { get; set; }
// }

// public class ProtocolExtensions
// {
// 	public bool Watchdog { get; set; }
// }


// public class WebSocketReceiver
// {
// 	#region Constructor	

// 	public WebSocketReceiver(Stream networkStream) => this.networkStream = networkStream;

// 	#endregion

// 	#region Methods	

// 	public Task<bool> StartMessageReceiving(Func<WsDecodedStream?, Exception?, Task> action, IWebSocketInternalSession internalSession)
// 		=> StartMessageReceiving(action, null, internalSession);

// 	public async Task<bool> StartMessageReceiving(Func<WsDecodedStream?, Exception?, Task> action, Action<string>? onPong, IWebSocketInternalSession? internalSession)
// 	{
// 		try
// 		{
// 			var headerBuffer = new byte[14];
// 			this.internalSession = internalSession;
// 			var read = await networkStream.ReadAsync(headerBuffer, 0, 2);
// 			if (read == 1)
// 				read = Read(headerBuffer, 1, 1);
// 			if (read == 0)
// 				// TODO:
// 				throw new ConnectionClosedException();
// 			return await MessageReceiving(headerBuffer, action, onPong);
// 		}
// 		catch (ConnectionClosedException ce)
// 		{
// 			await action(null, ce);
// 			return false;
// 		}
// 		catch (IOException)
// 		{
// 			await action(null, new ConnectionClosedException());
// 			return false;
// 		}
// 		catch
// 		{
// 			// TODO:
// 			return false;
// 		}
// 	}

// 	async Task<bool> MessageReceiving(byte[] headerBuffer, Func<WsDecodedStream, Exception?, Task> action, Action<string>? onPong)
// 	{
// 		var read = 2;
// 		var fin = (byte)((byte)headerBuffer[0] & 0x80) == 0x80;
// 		var deflated = (byte)((byte)headerBuffer[0] & 0x40) == 0x40;
// 		var opcode = (OpCode)((byte)headerBuffer[0] & 0xf);
// 		switch (opcode)
// 		{
// 			case OpCode.Close:
// 				Close();
// 				break;
// 			case OpCode.Ping:
// 			case OpCode.Pong:
// 			case OpCode.Text:
// 			case OpCode.ContinuationFrame:
// 				break;
// 			default:
// 			{
// 				Close();
// 				break;
// 			}
// 		}
// 		var mask = (byte)(headerBuffer[1] >> 7);
// 		var length = (ulong)(headerBuffer[1] & ~0x80);

// 		//If the second byte minus 128 is between 0 and 125, this is the length of message. 
// 		if (length < 126)
// 		{
// 			if (mask == 1)
// 				read += Read(headerBuffer, read, 4);
// 		}
// 		else if (length == 126)
// 		{
// 			// If length is 126, the following 2 bytes (16-bit unsigned integer), if 127, the following 8 bytes (64-bit unsigned integer) are the length.
// 			read += Read(headerBuffer, read, mask == 1 ? 6 : 2);
// 			var ushortbytes = new byte[2];
// 			ushortbytes[0] = headerBuffer[3];
// 			ushortbytes[1] = headerBuffer[2];
// 			length = BitConverter.ToUInt16(ushortbytes, 0);
// 		}
// 		else if (length == 127)
// 		{
// 			// If length is 127, the following 8 bytes (64-bit unsigned integer) is the length of message
// 			read += Read(headerBuffer, read, mask == 1 ? 12 : 8);
// 			var ulongbytes = new byte[8];
// 			ulongbytes[0] = headerBuffer[9];
// 			ulongbytes[1] = headerBuffer[8];
// 			ulongbytes[2] = headerBuffer[7];
// 			ulongbytes[3] = headerBuffer[6];
// 			ulongbytes[4] = headerBuffer[5];
// 			ulongbytes[5] = headerBuffer[4];
// 			ulongbytes[6] = headerBuffer[3];
// 			ulongbytes[7] = headerBuffer[2];
// 			length = BitConverter.ToUInt64(ulongbytes, 0);
// 		}
// 		else if (length > 127)
// 			Close();
// 		if (length == 0)
// 		{
// 			//if (opcode == OpCode.Ping)
// 			// TODO: Send pong
// 			// await MessageReceivingAsync(action);
// 			return false;
// 		}

// 		byte[] key = Array.Empty<byte>();
// 		if (mask == 1)
// 			key = new byte[4] { headerBuffer[read - 4], headerBuffer[read - 3], headerBuffer[read - 2], headerBuffer[read - 1] };
// 		if (wsDecodedStream == null)
// 			wsDecodedStream = new WsDecodedStream(networkStream, (int)length, key, mask == 1, deflated);
// 		else
// 			wsDecodedStream.AddContinuation((int)length, key, mask == 1);
// 		if (fin)
// 		{
// 			var receivedStream = wsDecodedStream;
// 			wsDecodedStream = null;
// 			switch (opcode)
// 			{
// 				case OpCode.Ping:
// 					internalSession?.SendPong(receivedStream?.Payload ?? "");
// 					break;
// 				case OpCode.Pong:
// 					onPong?.Invoke(receivedStream?.Payload ?? "");
// 					break;
// 				default:
// 					await action(receivedStream, null);
// 					break;
// 			}
// 		}

// 		return true;
// 	}

// 	int Read(byte[] buffer, int offset, int length)
// 	{
// 		var result = networkStream.Read(buffer, offset, length);
// 		if (result == 0)
// 			throw new ConnectionClosedException();
// 		return result;
// 	}

// 	void Close()
// 	{
// 		try
// 		{
// 			networkStream.Close();
// 		}
// 		catch { }
// 		throw new ConnectionClosedException();
// 	}

// 	#endregion

// 	#region Fields	

// 	IWebSocketInternalSession? internalSession;
// 	Stream networkStream;
// 	WsDecodedStream? wsDecodedStream;

// 	#endregion
// }


// namespace Caseris.Http.WebSockets
// {
// 	public class WsDecodedStream : Stream
// 	{
// 		#region Properties	

// 		public int DataPosition { get; protected set; }
// 		public string? Payload { get; protected set; }

// 		#endregion

// 		#region Constructor	

// 		public WsDecodedStream(Stream stream, int length, byte[] key, bool encode, bool isDeflated)
// 		{
// 			this.stream = stream;
// 			this.length = length;
// 			this.key = key;
// 			this.encode = encode;
// 			buffer = new byte[length];
// 			this.isDeflated = isDeflated;
// 			ReadStream(0);
// 		}

// 		protected WsDecodedStream()
// 		{
// 		}

// 		#endregion

// 		#region Stream	

// 		public override bool CanRead { get { return true; } }

// 		public override bool CanSeek { get { return false; } }

// 		public override bool CanWrite { get { return false; } }

// 		public override long Length { get { return length - DataPosition; } }

// 		public override long Position
// 		{
// 			get { return _Position; }
// 			set
// 			{
// 				if (value > Length)
// 					throw new IndexOutOfRangeException();
// 				_Position = value;
// 			}
// 		}
// 		long _Position;

// 		public override void Flush()
// 		{
// 		}

// 		public override int Read(byte[] buffer, int offset, int count)
// 		{
// 			if (Position + count > length - DataPosition)
// 				count = (int)length - DataPosition - (int)Position;
// 			if (count == 0)
// 				return 0;

// 			Array.Copy(this.buffer, offset + DataPosition + Position, buffer, offset, count);
// 			Position += count;

// 			return count;
// 		}

// 		public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

// 		public override void SetLength(long value) => throw new NotImplementedException();

// 		public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

// 		public virtual int WriteHeaderToAnswer(byte[] bytes, int position)
// 		{
// 			Array.Copy(buffer, 0, bytes, position, DataPosition);
// 			return DataPosition;
// 		}

// 		void ReadStream(int position)
// 		{
// 			var read = 0;
// 			while (read < length - position)
// 			{
// 				var newlyRead = stream?.Read(buffer, read + position, (int)length - position - read) ?? 0;
// 				if (newlyRead == 0)
// 					throw new ConnectionClosedException();
// 				read += newlyRead;
// 			}

// 			if (encode)
// 				for (var i = 0; i < length - position; i++)
// 					buffer[i + position] = (Byte)(buffer[i + position] ^ key[i % 4]);

// 			if (position == 0)
// 			{
// 				if (isDeflated)
// 				{
// 					var ms = new MemoryStream(buffer, 0, (int)length);
// 					var outputStream = new MemoryStream();
// 					var compressedStream = new DeflateStream(ms, CompressionMode.Decompress, true);
// 					compressedStream.CopyTo(outputStream);
// 					compressedStream.Close();
// 					outputStream.Capacity = (int)outputStream.Length;
// 					var deflatedBuffer = outputStream.GetBuffer();
// 					Payload = Encoding.UTF8.GetString(deflatedBuffer, 0, deflatedBuffer.Length);
// 				}
// 				else
// 					Payload = Encoding.UTF8.GetString(buffer, 0, (int)length);
// 				DataPosition = Payload.Length + 1;
// 			}
// 		}

// 		#endregion

// 		#region Methods

// 		public void AddContinuation(int length, byte[] key, bool encode)
// 		{
// 			var oldLength = buffer.Length;
// 			Array.Resize<byte>(ref buffer, oldLength + length);
// 			this.key = key;
// 			this.encode = encode;
// 			this.length += length;
// 			ReadStream(oldLength);
// 		}

// 		#endregion

// 		#region Fields

// 		readonly Stream? stream;
// 		byte[] buffer = Array.Empty<byte>();
// 		long length;
// 		byte[] key = Array.Empty<byte>();
// 		bool encode;
// 		readonly bool isDeflated;

// 		#endregion
// 	}
// }



		// 	const url = `${serverUrl}/proxy/Events/${WebSocketEventProcessing.sessionId}`.replace('http', 'ws')
		// 	this.webSocket = new WebSocket(url)
		// 	this.pinger = new Pinger(this.webSocket)

		// 	if (!WebSocketEventProcessing.sessionId)
		// 		return

		// 	this.webSocket.onopen = () => {
		// 		this.opened = true
		// 		WebSocketEventProcessing.resetReconnecting()
		// 		setConnected(true)
		// 		this.pinger.send()
		// 	}

		// 	this.webSocket.onmessage = evt => {
		// 		if (evt.data === 'pong') {
		// 			this.pinger.onPong()
		// 			return
		// 		}
		// 		if (evt.data === 'invalid') {
		// 			WebSocketEventProcessing.destroy(false, true)
		// 			if (this.onError)
		// 				this.onError({ Code: 5, Description: 'Invalid session id' } as ReturnValue)
		// 			this.onError = undefined
		// 			return
		// 		}
		// 		const event = JSON.parse(evt.data)
		// 		trace('proxy-event', event)
		// 		const loggedOut = event.ProxyMessages && event.ProxyMessages.find((proxyMessage: ProxyMessage) => proxyMessage.Reason === ProxyMessageReason.Logout)
		// 		const cancel = event.returnValue.Code === 111
		// 		if (!cancel) {
		// 			this.pinger.onPong()
		// 			WebSocketEventProcessing.newEvent(event)
		// 		}
		// 		else
		// 			trace('proxy-event', 'canceled')
		// 		if (loggedOut)
		// 			trace('proxy-event', 'loggedOut')

		// 		if (loggedOut || cancel)
		// 			WebSocketEventProcessing.destroy(false, !loggedOut || cancel)
		// 	}

		// 	this.webSocket.onerror = error => {
		// 		console.warn('proxy-event: Error in WebSocket connection', error)
		// 	}

		// 	this.webSocket.onclose = () => {
		// 		console.log('proxy-event: Connection closed')
		// 		if (!this.opened && !reconnect)
		// 			startHttpEventProcessingWithPolling(WebSocketEventProcessing.sessionId, WebSocketEventProcessing.newEvent, WebSocketEventProcessing.callbackError)
		// 		else
		// 			WebSocketEventProcessing.destroy(true, true)
		// 	}

		// 	access.stopEventProcessing = () => WebSocketEventProcessing.destroy(false, false)
		// }

		// private destroy() {		
		// 	console.log('proxy-event: destroying...')
		// 	this.pinger.destroy()
		// 	this.webSocket.onopen = null
		// 	this.webSocket.onmessage = null
		// 	this.webSocket.onerror = null
		// 	this.webSocket.onclose = null
		// 	this.webSocket.close()
		// }
