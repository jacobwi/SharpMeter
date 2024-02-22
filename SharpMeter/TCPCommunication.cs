using NLog;
using SharpMeter.PSEM;
using SharpMeter.Utils;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static SharpMeter.LibraryConstants;

namespace SharpMeter
{
    /// <summary>
    /// The TCP communication.
    /// </summary>
    public class TCPCommunication : ICommunication
    {
        /// <summary>
        /// Gets or sets the t client.
        /// </summary>
        public TcpClient TClient { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the stream.
        /// </summary>
        public NetworkStream Stream { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TCPCommunication"/> class.
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        /// <param name="logLevel">
        /// Logging level.
        ///   <para>Default [Off].</para>
        ///   <para>Types:</para>
        ///   <para>"Off"</para>
        ///   <para>"Info"</para>
        ///   <para>"Debug"</para>
        ///   <para>"Trace"</para>
        /// </param>
        public TCPCommunication(string address, int port, string logLevel = "Debug")
        {
            if (!LogLevels.Contains(logLevel))
                throw new ArgumentException($"Unknown log level [{logLevel}]\nAvaliable: {string.Join(",", LibraryConstants.LogLevels)}]");

            LogManager.Configuration.Variables["LibraryLevel"] = logLevel;
            LogManager.ReconfigExistingLoggers();

            var tcpClient = new TcpClient();

            this.Port = port;
            this.Address = address;
            this.TClient = tcpClient;
        }

        /// <summary>
        /// Opens connection.
        /// </summary>
        /// <returns>A string.</returns>
        public string Open()
        {
            try
            {
                if (TClient == null) return "TCP Client is null";

                var typeOfAddress = Uri.CheckHostName(Address);
                IPEndPoint ipEndPoint = null;

                switch (typeOfAddress)
                {
                    case UriHostNameType.IPv4:
                        ipEndPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
                        break;
                    case UriHostNameType.IPv6:
                        TClient = new TcpClient(AddressFamily.InterNetworkV6);
                        ipEndPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
                        break;
                    case UriHostNameType.Dns:
                    {
                        var endpoint = GetIPEndPointFromHostName(Address, Port, true);
                        ipEndPoint = endpoint;
                        break;
                    }
                    default:
                        Log.Info($"Address Type: {typeOfAddress}");
                        return "ERR - Unable to parse address";
                }
                if (ipEndPoint == null) return "IP Endpoint is null";

                if (TClient.Connected)
                {
                    Log.Info($"{Address}:{Port}");
                    return "ERR - Connection is already open";
                }
                else
                {
                    TClient.Connect(ipEndPoint);
                    Stream = TClient.GetStream();
                    Log.Info($"Connected to {Address}:{Port}...");
                }

                return "OK";
            }
            catch (Exception ex)
            {
                return $"ERR - {ex.Message}";
            }
        }

        /// <summary>
        /// Closes connection.
        /// </summary>
        /// <returns>A string.</returns>
        public string Close()
        {
            if (TClient == null) return "TCP Client is null";
            if (TClient.Connected)
            {
                TClient.Close();
                Stream.Close();
                Log.Info($"Closing connection to {Address}:{Port}...");
            }
            else
            {
                Log.Info($"{Address}:{Port}");
                return "ERR - Connection is already closed.";
            }
            return "OK";
        }

        /// <summary>
        /// Recieves a packet.
        /// </summary>
        /// <param name="receivedData">The received data.</param>
        /// <param name="isMultiPacket">If true, is multi packet.</param>
        /// <param name="firstPacketData">The first packet data.</param>
        /// <returns>A string.</returns>
        public string RecievePacket(out byte[] receivedData, bool isMultiPacket = false, byte[] firstPacketData = null)
        {
            var isSuccessfulRead = false;
            try
            {
                // TOIMPROVE: Figure out the bytes size in stream before reading it... possible?!
                const int bytesToRead = (int)512;

                receivedData = new byte[bytesToRead];
                Log.Trace($"Serial bytes to read - {(bytesToRead)}");
                Stream.Read(receivedData, 0, receivedData.Length);

                Log.Trace("RX message: {0}", BitConverter.ToString(receivedData));

                // Verify Packet
                var intIndex = 0;
                while (intIndex < receivedData.Length)
                {
                    if (PSEMStandards.STP != receivedData[intIndex++]) continue;
                    // EE byte (a.k.a Start of packet)
                    var crcIndex = (ushort)(intIndex - 1);
                    Log.Trace($"crcIndex - {(crcIndex)}");
                    // Reserved Byte
                    intIndex++;

                    // Control Byte
                    var signlePacket = (PSEMStandards.SINGLE_PACKET == (receivedData[intIndex] & PSEMStandards.SINGLE_PACKET)) ? true : false;

                    var multiPacket = (PSEMStandards.MULTI_PACKET == (receivedData[intIndex] & PSEMStandards.MULTI_PACKET)) ? true : false;

                    var byteToggle = (PSEMStandards.CONTROL_TOGGLE_BIT == (receivedData[intIndex++] & PSEMStandards.CONTROL_TOGGLE_BIT)) ? (byte)0x01 : (byte)0x00;

                    Log.Trace($"signlePacket - {(signlePacket)}");
                    Log.Trace($"multiPacket - {(multiPacket)}");
                    Log.Trace($"byteToggle - {(byteToggle)}");

                    // Sequence Byte
                    var byteSeqNum = receivedData[intIndex++];

                    // Data length Byte
                    var dataLength = (receivedData[intIndex++] << 8) | receivedData[intIndex++];
                    Log.Debug($"Packet Data Size: [{dataLength}] bytes.");

                    var packetCRC = (ushort)((receivedData[dataLength + intIndex] << 8) |
                                             receivedData[dataLength + intIndex + 1]);
                    var calculatedCRC = CRC.CalcCRC(receivedData,
                        crcIndex,
                        (ushort)(intIndex + dataLength - crcIndex));

                    if (isMultiPacket)
                    {
                        Log.Debug("Multi packet received...");
                        multiPacket = false;
                        firstPacketData = firstPacketData.Skip(10).ToArray().SkipLast(2).ToArray();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log.Trace("Previous Packet: " + BitConverter.ToString(firstPacketData));
                        var sec = receivedData.Skip(10).ToArray().SkipLast(2).ToArray();

                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                        Log.Trace("Second Packet: " + BitConverter.ToString(sec));
                        Console.ResetColor();

                        var tempResult = receivedData;
                        var finalResult = Array.Empty<byte>();

                        finalResult = finalResult.Concat(receivedData.Take(9).Concat(firstPacketData)).ToArray();

                        // Second packet
                        finalResult = finalResult.Concat(receivedData.Skip(10)).ToArray();
                        Log.Trace("Final Packet: " + BitConverter.ToString(finalResult));
                        Log.Trace("Size Packet: " + finalResult.Length);

                        receivedData = finalResult;
                    }
                    Log.Trace($"packetCRC - {(packetCRC)}");
                    Log.Trace($"calculatedCRC - {(calculatedCRC)}");
                    if (packetCRC != calculatedCRC)
                    {
                        receivedData = null;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log.Debug("\n[CRC Check Failed]\n");
                        Console.ResetColor();
                        SendPacket(new byte[1] { 0x15 });
                        break;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Log.Debug("\n[CRC Check OK]\n");
                        Console.ResetColor();
                        isSuccessfulRead = true;
                        SendPacket(new byte[1] { 0x06 });

                        if (multiPacket)
                        {
                            Log.Debug($"Merging second packet...");

                            byte[] finalData;
                            RecievePacket(out finalData, true, receivedData);

                            if (finalData != null)
                            {
                                var newValues = new byte[finalData.Length + 1];
                                newValues[0] = 0x06;                                // set the prepended value
                                Array.Copy(finalData, 0, newValues, 1, finalData.Length);
                                receivedData = newValues;
                            }
                        }
                        break;
                    }
                }

                return isSuccessfulRead ? "OK" : "ERROR in CRC CHECK";
            }
            catch (Exception ex)
            {
                Log.Trace($"Receive - {ex.Message}");
                receivedData = null;
                return ex.Message;
            }
        }

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="packetToSend">The packet to send.</param>
        /// <returns>A string.</returns>
        public string SendPacket(byte[] packetToSend)
        {
            try
            {
                Log.Trace("TX message: {0}", BitConverter.ToString(packetToSend));
                Stream.Write(packetToSend, 0, packetToSend.Length);
                Thread.Sleep(500);
                return "OK";
            }
            catch (Exception ex)
            {
                Log.Info("Error Sending packet");
                Log.Debug($"Send - {ex.Message}");
                Log.Trace($"Send - {packetToSend}");
                return ex.Message;
            }
        }

        /// <summary>
        /// Gets the IP end point from host name.
        /// </summary>
        /// <param name="hostName">The host name.</param>
        /// <param name="port">The port.</param>
        /// <param name="throwIfMoreThanOneIP">If true, throw if more than one i p.</param>
        /// <returns>An IPEndPoint.</returns>
        private static IPEndPoint GetIPEndPointFromHostName(string hostName, int port, bool throwIfMoreThanOneIP)
        {
            var addresses = System.Net.Dns.GetHostAddresses(hostName);
            if (addresses.Length == 0)
            {
                throw new ArgumentException(
                    "Unable to retrieve address from specified host name.",
                    nameof(hostName)
                );
            }
            else if (throwIfMoreThanOneIP && addresses.Length > 1)
            {
                throw new ArgumentException(
                    "There is more that one IP address to the specified host.",
                    nameof(hostName)
                );
            }
            return new IPEndPoint(addresses[0], port); // Port gets validated here.
        }
    }
}