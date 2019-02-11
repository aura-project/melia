// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Melia.Shared.Network.Crypto;
using Melia.Shared.Util;

namespace Melia.Shared.Network
{
	public abstract class Connection
	{
		private byte[] _buffer, _backBuffer;
		private Socket _socket;
		private TOSCrypto _crypto;

		private object _cleanUpLock = new object();
		private bool _cleanedUp;

		/// <summary>
		/// State of the connection.
		/// </summary>
		public ConnectionState State { get; protected set; }

		/// <summary>
		/// True if logged in.
		/// </summary>
		public bool LoggedIn { get; set; }

		/// <summary>
		/// Remote address.
		/// </summary>
		public string Address { get; protected set; }

		/// <summary>
		/// Raised when connection is closed.
		/// </summary>
		public event EventHandler Closed;

		/// <summary>
		/// Connection's index on the connection manager's list.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// Session key for this connection.
		/// </summary>
		public readonly string SessionKey;

		/// <summary>
		/// Randomly generated value used for checking the integrity of IPF archives.
		/// </summary>
		public readonly int IntegritySeed;

		/// <summary>
		/// When set to 'true', client packets are ignored.
		/// </summary>
		public bool IgnorePackets = false;

		/// <summary>
		/// Creates new connection.
		/// </summary>
		public Connection()
		{
			_buffer = new byte[1024 * 500];
			_backBuffer = new byte[ushort.MaxValue];
			_crypto = new TOSCrypto();

			this.State = ConnectionState.Open;
			this.Address = "?:?";
			this.IntegritySeed = RandomProvider.Get().Next();

			// Generates a session key based on a GUID.
			using (var sha = new SHA1Managed())
			{
				var hash = sha.ComputeHash(Guid.NewGuid().ToByteArray());
				this.SessionKey = "*" + BitConverter.ToString(hash).Replace("-", String.Empty);
			}
		}

		/// <summary>
		/// Sets connection's socket once.
		/// </summary>
		/// <param name="socket"></param>
		/// <exception cref="InvalidOperationException">Thrown if socket was already set.</exception>
		public void SetSocket(Socket socket)
		{
			if (_socket != null)
				throw new InvalidOperationException("Socket is already set.");

			_socket = socket;
			this.Address = ((IPEndPoint)socket.RemoteEndPoint).ToString();
		}

		/// <summary>
		/// Closes the connection.
		/// </summary>
		public void Close()
		{
			if (this.State == ConnectionState.Closed)
			{
				Log.Warning("Attempted closing of an already closed connection.");
				return;
			}

			this.State = ConnectionState.Closed;

			try { _socket.Shutdown(SocketShutdown.Both); }
			catch { }
			try { _socket.Close(); }
			catch { }

			this.OnClosed();

			Log.Info("Closed connection from '{0}'.", this.Address);
		}

		/// <summary>
		/// Starts packet receiving.
		/// </summary>
		public void BeginReceive()
		{
			_socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, this.OnReceive, null);
		}

		/// <summary>
		/// Called when new data is available from socket.
		/// </summary>
		/// <param name="result"></param>
		private void OnReceive(IAsyncResult result)
		{
			try
			{
				var length = _socket.EndReceive(result);
				var read = 0;

				// Client disconnected
				if (length == 0)
				{
					this.State = ConnectionState.Closed;
					this.OnClosed();
					Log.Info("Connection was closed from '{0}'.", this.Address);
					return;
				}
				
				while (read < length)
				{
					var packetLength = BitConverter.ToUInt16(_buffer, read);
					if (packetLength > length)
					{
						Log.Debug(BitConverter.ToString(_buffer, read, length - read));
						throw new Exception("Packet length greater than buffer length (" + packetLength + " > " + length + ").");
					}

					// Read packet from buffer
					var packetBuffer = new byte[packetLength];
					Buffer.BlockCopy(_buffer, read + sizeof(short), packetBuffer, 0, packetLength);
					read += sizeof(short) + packetLength;
					_crypto.Decrypt(packetBuffer, 0, packetLength);

					// Get packet
					var packet = new Packet(packetBuffer);

					// Debug
					//var opName = Op.GetName(packet.Op);
					//var recvStr = BitConverter.ToString(packetBuffer).Replace("-", " ");
					//recvStr = recvStr.Insert(0, "[");
					//recvStr = recvStr.Insert(6, "]");
					//recvStr = recvStr.Insert(8, "[");
					//recvStr = recvStr.Insert(20, "]");
					//recvStr = recvStr.Insert(22, "[");
					//recvStr = recvStr.Insert(34, "]");
					//Log.Debug("Recv: {0} {1}", opName, recvStr);
					//Log.Debug("Recv:\n{0}", packet.ToString());

					// Check size from table?
					var size = Op.GetSize(packet.Op);
					if (size != 0 && packet.Length < size)
					{
						Log.Warning("Invalid packet size for '{0:X4}' ({1} < {2}), from '{3}'. Ignoring packet.", packet.Op, packet.Length, size, this.Address);
					}
					// Check padding
					// Packets have a padding at the end, since the encryption
					// requires multiples of 8. If this padding is greater
					// then 7, it means a packet got bigger and we need to
					// update the packet size table.
					// This should never happen, as long as the packet size
					// table is up-to-date.
					else if (size != 0 && packet.Length - size > 7)
					{
						Log.Warning("Invalid padding for '{0:X4}' ({1}, {2}), from '{3}'. Ignoring packet.", packet.Op, packet.Length, size, this.Address);
					}
					else
					{
						// Check login state
						if (packet.Op != Op.CB_LOGIN && packet.Op != Op.CB_LOGIN_BY_PASSPORT && packet.Op != Op.CS_LOGIN && packet.Op != Op.CZ_CONNECT)
						{
							if (!this.LoggedIn)
							{
								Log.Warning("Non-login packet ({0:X4}) sent before being logged in, from '{1}'. Killing connection.", packet.Op, this.Address);
								this.Close();
								return;
							}
						}

						// Handle
						try
						{
							if (!this.IgnorePackets)
							{
								this.HandlePacket(packet);
							}
						}
						catch (Exception ex)
						{
							Log.Exception(ex, "Error while handling packet '{0:X4}', {1}.", packet.Op, Op.GetName(packet.Op));
						}
					}
				}

				this.BeginReceive();
			}
			catch (SocketException)
			{
				this.State = ConnectionState.Closed;
				this.OnClosed();
				Log.Info("Lost connection from '{0}'.", this.Address);

			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Error while receiving packet.");
			}
		}

		/// <summary>
		/// To be called when connection is closed, calls event
		/// and CleanUp.
		/// </summary>
		private void OnClosed()
		{
			this.Closed?.Invoke(this, null);

			lock (_cleanUpLock)
			{
				if (!_cleanedUp)
					this.CleanUp();
				else
					Log.Warning("Trying to clean already cleaned connection.");

				_cleanedUp = true;
			}
		}

		/// <summary>
		/// Called when the connection is closed.
		/// </summary>
		protected virtual void CleanUp()
		{
			Log.Debug("CLEAN UP");
		}

		/// <summary>
		/// Called for every packet that is read from the network stream.
		/// </summary>
		/// <param name="packet"></param>
		protected virtual void HandlePacket(Packet packet)
		{
			Log.Warning("No packet handling.");
		}

		/// <summary>
		/// Sends packet to client.
		/// </summary>
		/// <param name="packet"></param>
		public void Send(Packet packet)
		{
			if (_socket == null || this.State == ConnectionState.Closed)
				return;

			// Get size from table
			var size = Op.GetSize(packet.Op);
			if (size == -1)
				throw new ArgumentException("Size for op '" + packet.Op.ToString("X4") + "' unknown.");

			// Prior to i174236 packet headers sent from the server to the
			// client were 4 bytes shorter, as they didn't have the part
			// that we call "checksum". Now they all have it. Should this
			// change again at some point, the respective sizeof(int)s need
			// to be removed again.

			// Calculate length
			var fixHeaderSize = (sizeof(short) + sizeof(int) + sizeof(int) + packet.Length);
			var dynHeaderSize = (sizeof(short) + sizeof(int) + sizeof(int) + sizeof(short) + packet.Length);
			var length = (size == 0 ? dynHeaderSize : size);

			// Check table length
			if (size != 0)
			{
				if (length < sizeof(short) + sizeof(int) + sizeof(int) + packet.Length)
					throw new Exception("Packet is bigger than specified in the packet size table.");

				if (size != sizeof(short) + sizeof(int) + sizeof(int) + packet.Length)
					Log.Warning("Packet size doesn't match packet table size. (op: {3} ({0:X4}), size: {1}, expected: {2})", packet.Op, fixHeaderSize, size, Op.GetName(packet.Op));
			}

			// Create packet
			var buffer = new byte[length];
			Buffer.BlockCopy(BitConverter.GetBytes((short)packet.Op), 0, buffer, 0, sizeof(short));
			Buffer.BlockCopy(BitConverter.GetBytes(-1), 0, buffer, sizeof(short), sizeof(int)); // checksum?

			var offset = (sizeof(short) + sizeof(int) + sizeof(int));
			if (size == 0)
			{
				Buffer.BlockCopy(BitConverter.GetBytes((short)length), 0, buffer, offset, sizeof(short));
				offset += sizeof(short);
			}

			packet.Build(ref buffer, offset);

			// Debug
			//var opName = Op.GetName(packet.Op);
			//var sendStr = BitConverter.ToString(buffer).Replace("-", " ");
			//sendStr = sendStr.Insert(0, "[");
			//sendStr = sendStr.Insert(6, "]");
			//sendStr = sendStr.Insert(8, "[");
			//sendStr = sendStr.Insert(20, "]");
			//if (size == 0)
			//{
			//	sendStr = sendStr.Insert(22, "[");
			//	sendStr = sendStr.Insert(28, "]");
			//}
			//Log.Debug("Send: {0} {1}", opName, sendStr);

			//Log.Debug("Send:\n{0}", packet.ToString());

			//Send
			_socket.Send(buffer);
		}
	}

	public enum ConnectionState
	{
		Closed,
		Open,
	}
}
