using NLog;
using SharpMeter.PSEM;
using SharpMeter.Utils;
using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using static SharpMeter.LibraryConstants;

namespace SharpMeter
{
    /// <summary>
    /// The serial communication.
    /// </summary>
    public class SerialCommunication : ICommunication
    {
        private byte[] cosmBuffer;
        private bool multiData = false;

        /// <summary>
        /// Gets or sets the serial.
        /// </summary>
        public SerialPort Serial;

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        public int BaudRate { get; set; }


        public SerialCommunication()
        {

        }
        /// <summary>Initializes a new instance of the <see cref="SerialCommunication" /> class.</summary>
        /// <param name="comPort">The com port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="logLevel">
        /// Logging level.
        ///   <para>Default [Off].</para>
        ///   <para>Types:</para>
        ///   <para>"Off"</para>
        ///   <para>"Info"</para>
        ///   <para>"Debug"</para>
        ///   <para>"Trace"</para>
        /// </param>
        public SerialCommunication(int comPort, int baudRate, string logLevel = "Off")
        {
            if (!LogLevels.Contains(logLevel))
                throw new ArgumentException($"Unknown log level [{logLevel}]\nAvaliable: {string.Join(",", LibraryConstants.LogLevels)}]");

            LogManager.Configuration.Variables["LibraryLevel"] = logLevel;

            LogManager.ReconfigExistingLoggers();

            this.BaudRate = baudRate;
            this.Port = comPort;

            Serial = new SerialPort($"COM{comPort}", baudRate, Parity.None, 8, StopBits.One);

            Log.Debug($"Init Serial - COM{comPort}");
        }
        public int Initialize(int comPort, int baudRate, string logLevel)
        {
            if (!LogLevels.Contains(logLevel))
                throw new ArgumentException($"Unknown log level [{logLevel}]\nAvaliable: {string.Join(",", LibraryConstants.LogLevels)}]");

            LogManager.Configuration.Variables["LibraryLevel"] = logLevel;

            LogManager.ReconfigExistingLoggers();

            this.BaudRate = baudRate;
            this.Port = comPort;

            Serial = new SerialPort($"COM{comPort}", baudRate, Parity.None, 8, StopBits.One);

            Log.Debug($"Init Serial - COM{comPort}");
            return 0;
        }
        /// <summary>
        /// Opens serial port.
        /// </summary>
        /// <returns>a string of result</returns>
        public string Open()
        {
            if (Serial != null)
            {
                try
                {
                    if (Serial.IsOpen)
                    {
                        Log.Debug($"OK - Serial Port [{Port}] is already open");
                        return $"OK - Serial Port [{Port}] is already open";
                    }
                    else
                    {
                        Serial.Open();
                        Log.Debug($"OK - Serial Port [{Port}] is open");
                        return "OK";
                    }
                }
                catch (Exception ex)
                {
                    Log.Trace($"Open - {ex.Message}");
                    return $"ERROR - {ex.Message}";
                }
            }
            else
                return "ERROR - Serial is null when attempting to open port...";
        }

        /// <summary>
        /// Closes serial port.
        /// </summary>
        /// <returns>a string of result</returns>
        public string Close()
        {
            if (Serial != null)
            {
                try
                {
                    if (Serial.IsOpen)
                    {
                        Serial.Close();
                        return "OK";
                    }
                    else
                    {
                        return $"OK - Serial Port [{Port}] is already close";
                    }
                }
                catch (Exception ex)
                {
                    Log.Trace($"Close - {ex.Message}");
                    return $"ERROR - {ex.Message}";
                }
            }
            else
                return "ERROR - Serial is null when attempting to close port...";
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
                var bytesToRead = Serial.BytesToRead;

                if (bytesToRead == 0)
                {
                    receivedData = null;
                    return "Zero Bytes Received";
                }

                receivedData = new byte[Serial.BytesToRead];
                Log.Trace($"Serial bytes to read - {(Serial.BytesToRead)}");
                Serial.Read(receivedData, 0, receivedData.Length);

                Log.Trace("RX message: {0}", BitConverter.ToString(receivedData));

                // Verify Packet
                byte byteSeqNum = 0x0;
                ushort packetCRC = 0;
                ushort calculatedCRC = 0;
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
                    byteSeqNum = receivedData[intIndex++];

                    // Data length Byte
                    var dataLength = (receivedData[intIndex++] << 8) | receivedData[intIndex++];
                    Log.Debug($"Packet Data Size: [{dataLength}] bytes.");

                    packetCRC = (ushort)((receivedData[dataLength + intIndex] << 8) |
                                         receivedData[dataLength + intIndex + 1]);
                    calculatedCRC = CRC.CalcCRC(receivedData,
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

                            RecievePacket(out var finalData, true, receivedData);

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
                Serial.Write(packetToSend, 0, packetToSend.Length);
                Thread.Sleep(500);
                return "OK";
            }
            catch (Exception ex)
            {
                Log.Info("Error Sending packet");
                Log.Error($"Send - {ex.Message}");
                Log.Error($"Send - {packetToSend}");
                return ex.Message;
            }
        }

        /// <summary>
        /// Recieves COSM packet/frame.
        /// </summary>
        /// <param name="receivedData">The received data.</param>
        /// <returns>A string.</returns>
        public string RecieveCOSMPacket(out byte[] receivedData)
        {
            receivedData = null;
            try
            {
                var bytesToRead = Serial.BytesToRead;

                if (bytesToRead == 0)
                {
                    receivedData = null;
                    return "Zero Bytes Received";
                }

                receivedData = new byte[Serial.BytesToRead];
                Log.Trace($"Serial bytes to read - {(Serial.BytesToRead)}");
                Serial.Read(receivedData, 0, receivedData.Length);
                Console.ForegroundColor = ConsoleColor.Magenta;

                Log.Trace("RX message: {0}", BitConverter.ToString(receivedData));
                Console.ResetColor();

                return "OK";
            }
            catch (Exception ex)
            {
                Log.Info("Error recieving packet");
                Log.Error($"Send - {ex.Message}");
                //Log.Trace($"Send - {cp.Data}");
                return ex.Message;
            }
        }

        /// <summary>
        /// Recieves COSM packet/frame.
        /// </summary>
        /// <param name="receivedData">The received data.</param>
        /// <param name="payloadData">The payload data.</param>
        /// <returns>A string.</returns>
        public string RecieveCOSMPacket(out byte[] receivedData, out byte[] payloadData)
        {
            payloadData = null;
            try
            {
                var bytesToRead = Serial.BytesToRead;

                if (bytesToRead == 0)
                {
                    receivedData = null;
                    return "Zero Bytes Received";
                }

                receivedData = new byte[Serial.BytesToRead];
                Log.Trace($"Serial bytes to read - {(Serial.BytesToRead)}");
                Serial.Read(receivedData, 0, receivedData.Length);
                if (receivedData.Last() != (byte)0x7E)
                {
                    multiData = true;
                    cosmBuffer ??= new byte[receivedData.Length];
                    cosmBuffer.Concat(receivedData);

                    RecieveCOSMPacket(out cosmBuffer, out payloadData);
                }
                else
                {
                    if (multiData == true)
                        receivedData = cosmBuffer;
                    multiData = false;
                }
                Console.ForegroundColor = ConsoleColor.Magenta;
                Log.Trace("RX message: {0}", BitConverter.ToString(receivedData));
                Console.ResetColor();

                // payload if exists
                payloadData = receivedData.SubArray(14, receivedData.Length - 17);
                if (payloadData != null)
                    Log.Trace("RX Data: {0}", BitConverter.ToString(payloadData));

                return "OK";
            }
            catch (Exception ex)
            {
                receivedData = null;
                Log.Info("Error recieving packet");
                Log.Debug($"Send - {ex.Message}");
                //Log.Trace($"Send - {cp.Data}");
                return ex.Message;
            }
        }
    }
}