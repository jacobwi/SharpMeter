using Newtonsoft.Json;
using SharpMeter.PSEM.Models;
using SharpMeter.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static SharpMeter.LibraryConstants;
using static SharpMeter.PSEM.PSEMStandards;

namespace SharpMeter.PSEM
{
    /// <summary>
    /// PSEM services class to execute meter communication commands.
    /// </summary>
    public class PSEMMeter
    {
        /// <summary>
        /// Gets or sets the meter.
        /// </summary>
        public Meter Meter { get; set; }

        /// <summary>
        /// Gets or sets the table services.
        /// </summary>
        public PSEMTableServices TableServices { get; set; }

        /// <summary>
        /// PSEM toggle byte.
        /// </summary>
        protected byte stateToggleByte;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        private string Password { get; set; }

        /// <summary>Enumration for supported baud rates in PSEM</summary>
        public enum BaudRates : byte
        {
            BR_300 = 0x01,
            BR_600 = 0x02,
            BR_1200 = 0x03,
            BR_2400 = 0x04,
            BR_4800 = 0x05,
            BR_9600 = 0x06,
            BR_14400 = 0x07,
            BR_19200 = 0x08,
            BR_28800 = 0x09,
            BR_38400 = 0x0A
        }

        /// <summary>
        /// Saves meter to file.
        /// </summary>
        /// <param name="filePath">Directory path</param>
        /// <param name="fileName">File name.</param>
        public void SaveToFile(string filePath, string fileName)
        {
            try
            {
                Meter.Serialize(filePath, fileName);
            }
            catch (Exception ex)
            {
                Log.Error($"ERR - Saving meter [{ex.Message}]");
            }
        }

        /// <summary>
        /// Loads meter from file to instance.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public void LoadFromFile(string fileName)
        {
            try
            {
                var jsonString = File.ReadAllText(fileName);
                Meter = JsonConvert.DeserializeObject<Meter>(jsonString);
            }
            catch (Exception ex)
            {
                Log.Error($"ERR - Loading meter from file [{ex.Message}]");
            }
        }

        /// <summary>
        /// Loads meter from file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>A Meter.</returns>
        public static Meter LoadMeter(string fileName)
        {
            try
            {
                var jsonString = File.ReadAllText(fileName);
                var meter = JsonConvert.DeserializeObject<Meter>(jsonString);
                return meter;
            }
            catch (Exception ex)
            {
                Log.Error($"ERR - Loading meter [{ex.InnerException}]");
                return null;
            }
        }

        /// <summary>
        /// Gets or sets the serial communication class object.
        /// </summary>
        public ICommunication Communication { get; set; }

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSEMMeter"/> class.
        /// </summary>
        /// <param name="communication">The communication.</param>
        /// <param name="br">The br.</param>
        public PSEMMeter(ICommunication communication, int br = 9600)

        {
            this.Communication = communication;
            this.BaudRate = br;
        }
        public PSEMMeter()
        {

        }
        public void Initialize(ICommunication communication, int br = 9600) {
            this.Communication = communication;
            this.BaudRate = br;
        }
        /// <summary>Connects to meter.</summary>
        /// <param name="password">The password.</param>
        /// <returns>Operation result</returns>
        public string ConnectToMeter(string password)
        {
            Password = password;
            if (!string.IsNullOrEmpty(Password))
                HandlePasswordEntry();

            Log.Info($"Connecting to meter with password [{password}]");

            byte[] identity;

            Log.Info($"Identifying meter...");
            Identify(out identity);

            if (identity == null)
            {
                return "Identify: No Response";
            }
            else
            {
                if (identity[0] != 0x06)
                {
                    return "Identify: Bad Response";
                }
            }

            byte[] neg;
            Log.Info($"Negotiating with meter...");
            Negotiate(out neg);
            if (neg == null)
            {
                return "Negotiate: No Response";
            }
            else
            {
                if (neg[0] != 0x06)
                {
                    return "Negotiate: Bad Response";
                }
            }

            byte[] log;
            Log.Info($"Logon meter...");
            Logon(out log);
            if (log == null)
            {
                return "Logon: No Response";
            }
            else
            {
                if (log[0] != 0x06)
                {
                    return "Logon: Bad Response";
                }
            }

            byte[] sec;

            Log.Info($"SecurityLogin meter...");
            SecurityLogin(out sec, password: password);

            if (sec == null)
            {
                return "SecurityLogin: No Response";
            }
            else
            {
                if (sec[0] != 0x06)
                {
                    return "SecurityLogin: Bad Response";
                }
            }

            // Passed all checks
            // Create meter object
            Meter = new Meter();
            TableServices = new PSEMTableServices();
            byte[] read0 = null;
            byte[] read1 = null;
            byte[] read3 = null;

            byte[] readmt0 = null;
            byte[] readmt64 = null;
            byte[] readmt85 = null;
            Read(0, out read0);
           // Read(1, out read1);
  

            // Add tables to meter object
            Meter.StandardTables = new List<Table>();
          
         

            return "OK";
        }

        /// <summary>
        /// Setups the library.
        /// </summary>

        /// <summary>
        /// Handles the password entry.
        /// </summary>
        private void HandlePasswordEntry()
        {
            try
            {
                if (!string.IsNullOrEmpty(Password))
                {
                    var passwordsList = new List<string>();
                    var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var specificFolder = Path.Combine(folder, "SharpMeter");
                    var secretord = specificFolder + @"\passwords.SM";
                    var pass = "*?w#59qxx#PKTaj2";
                    var lines = File.ReadAllLines(secretord);

                    if (lines.Length > 0)
                    {
                        foreach (var line in lines)
                        {
                            passwordsList.Add(Crypto.Decrypt(line, pass));
                        }

                        if (!passwordsList.Contains(Password))
                        {
                            using (TextWriter tw = new StreamWriter(secretord, append: true))
                            {
                                var encrypted = Crypto.Encrypt(Password, pass);

                                tw.WriteLineAsync(encrypted);
                            }
                        }
                    }
                    else
                    {
                        using (var fs = new FileStream(secretord, FileMode.Open))
                        {
                            using (TextWriter tw = new StreamWriter(fs, Encoding.UTF8, 1024, true))
                            {
                                var encrypted = Crypto.Encrypt(Password, pass);

                                tw.WriteLine(encrypted);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Disconnects the meter.
        /// </summary>
        /// <returns>A string.</returns>
        public string DisconnectMeter()
        {
            Log.Info($"Disconnecting meter...");

            byte[] off;

            Logoff(out off);
            if (off == null)
            {
                return "Logoff: No Response";
            }
            else
            {
                if (off[0] != 0x06)
                {
                    return "Logoff: Bad Response";
                }
            }
            byte[] term;
            Terminate(out term);

            if (term == null)
            {
                return "Terminate: No Response";
            }
            else
            {
                if (term[0] != 0x06)
                {
                    return "Terminate: Bad Response";
                }
            }
            Meter = null;
            return "OK";
        }

        /*
         * Request:
         *  Ex: EE-00-00-00-00-01-20-13-10
         *      <STP>                   0xEE
         *  <ReservedByte>              0x00
         *  <ControlByte>               0x01 | 0x00
         *  <SequenceByte>              0x00
         *  <DataLength> (2 bytes)      0x00 0x01
         *  <Request/Data>              0x20 (Identity)
         *  <CRC> (2 bytes)             CRC 16
         *
         *
         */

        /// <summary>Send out an identification request to the meter</summary>
        /// <param name="resultData">The result data.</param>
        /// <returns>Opeartion result</returns>
        public string Identify(out byte[] resultData)
        {
            resultData = null;
            var identityByte = new byte[1] { (byte)PSEMStandards.Services.IDENTIFY };
            var identityRequest = GeneratePacket(identityByte);
            var res = Communication.SendPacket(identityRequest);

            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);

                return res;
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        /* FIX COMMENT
         * Request:
         *  Ex: EE-00-20-00-00-05-61-00-80-FE-06-16-3D
         *      <STP>                   0xEE
         *  <ReservedByte>              0x00
         *  <ControlByte>               0x01 | 0x00
         *  <SequenceByte>              0x00
         *  <DataLength> (2 bytes)      0x00 0x01
         *  <Request/Data>              0x20 (Identity)
         *  <CRC> (2 bytes)             CRC 16
         *
         *
         */

        /// <summary>Sends out negotation request to the meter</summary>
        /// <param name="resultData">The result data.</param>
        /// <returns>Opeartion result</returns>
        /// <exception cref="System.Exception">PSEM protocol does not support more than 11 rates in a Negotiate Request.</exception>
        public string Negotiate(out byte[] resultData)
        {
            resultData = null;
            var byteBauds = new List<byte>();
            byte[] dataPacket;
            var intIndex = 0;
            var intBaudIndex = 0;

            // Add baud rates to list that will be used to create the data packet
            byteBauds.Add((byte)BaudRates.BR_9600);
            byteBauds.Add((byte)BaudRates.BR_14400);
            byteBauds.Add((byte)BaudRates.BR_19200);
            byteBauds.Add((byte)BaudRates.BR_28800);
            byteBauds.Add((byte)BaudRates.BR_38400);

            intIndex = byteBauds.IndexOf((byte)BaudRateToByte(BaudRate));
            if (intIndex + 1 < byteBauds.Count)
            {
                byteBauds.RemoveRange(intIndex + 1, byteBauds.Count - (intIndex + 1));
            }

            if (byteBauds.Count > 11)
            {
                throw new Exception("PSEM protocol does not support more than 11 rates in a Negotiate Request.");
            }

            intIndex = 0;

            // Create the packet
            dataPacket = new byte[4 + byteBauds.Count];

            // Service request
            dataPacket[intIndex++] = (byte)((byte)(PSEMStandards.Services.NEGOTIATE_NO_BAUD) + (byte)byteBauds.Count);
            dataPacket[intIndex++] = (PSEMStandards.DEFAULT_MAX_PACKET_LEGNTH >> 8);

            // Default packet size [packet size]
            dataPacket[intIndex++] = (PSEMStandards.DEFAULT_MAX_PACKET_LEGNTH & 0x00FF);

            // Number of packets
            dataPacket[intIndex++] = (byte)PSEMStandards.DEFAULT_MAX_NUMBER_OF_PACKETS;

            for (intBaudIndex = 0; intBaudIndex < byteBauds.Count; intBaudIndex++)
            {
                dataPacket[intIndex++] = byteBauds[intBaudIndex];
            }

            var negRequest = GeneratePacket(dataPacket);
            var res = Communication.SendPacket(negRequest);
            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);

                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        #region Meter Log Methods

        /// <summary>Sends out a login request to the meter</summary>
        /// <param name="resultData">The result data.</param>
        /// <param name="username">The username.</param>
        /// <returns>Opeartion result</returns>
        public string Logon(out byte[] resultData, string username = "SharpMeter")
        {
            resultData = null;
            var bytUser = new byte[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var bytes = new ASCIIEncoding().GetBytes(username);
            var dataPacket = new byte[13];
            var intIndex = 0;
            long usrId = 2;

            Array.Copy(bytes, 0, bytUser, 0, bytes.Length);

            // Service request
            dataPacket[intIndex++] = (byte)PSEMStandards.Services.LOGON;

            // User id
            dataPacket[intIndex++] = (byte)(usrId >> 8);
            dataPacket[intIndex++] = (byte)(usrId & 0x00FF);

            Array.Copy(bytUser, 0, dataPacket, intIndex, bytUser.Length);
            intIndex += bytUser.Length;

            var loginRequest = GeneratePacket(dataPacket);
            var res = Communication.SendPacket(loginRequest);
            if (res == "OK")
            {
                Log.Debug($"Logged in as [{username}]");
                res = Communication.RecievePacket(out resultData);

                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        /// <summary>Sends a security request to the meter</summary>
        /// <param name="resultData">The result data.</param>
        /// <param name="password">The password subpacket.</param>
        /// <returns>Operation result</returns>
        public string SecurityLogin(out byte[] resultData, string password = "")
        {
            resultData = null;
            var intIndex = 0;
            var bytUserPassword = new byte[20] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] dataPacket;

            if (password.Length > 20)
            {
                resultData = null;
                return $"Inccorect password length [{password.Length}]";
            }

            var tmp = password.HexToByteArray();
            if (null != tmp)
            {
                Array.Copy(tmp, 0, bytUserPassword, 0, tmp.Length);
            }
            dataPacket = new byte[21];

            // Service request
            dataPacket[intIndex++] = (byte)PSEMStandards.Services.SECURITY;

            Array.Copy(bytUserPassword, 0, dataPacket, intIndex, bytUserPassword.Length);
            intIndex += bytUserPassword.Length;

            var secRequest = GeneratePacket(dataPacket);
            var res = Communication.SendPacket(secRequest);
            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);
                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        /// <summary>Sends out logoff request to the meter</summary>
        /// <param name="resultData">The result data.</param>
        /// <returns>Opearation result</returns>
        public string Logoff(out byte[] resultData)
        {
            resultData = null;

            // Service request
            var logoffByte = new byte[1] { (byte)PSEMStandards.Services.LOGOFF };
            var logoffRequest = GeneratePacket(logoffByte);
            var res = Communication.SendPacket(logoffRequest);

            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);
                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        #endregion Meter Log Methods

        /// <summary>Terminates meter's PSEM session and closes serial port</summary>
        /// <param name="resultData">The result data.</param>
        /// <returns>Operation result</returns>
        public string Terminate(out byte[] resultData)
        {
            resultData = null;

            // Service Request
            var terminateByte = new byte[1] { (byte)PSEMStandards.Services.TERMINATE };
            var terminateRequest = GeneratePacket(terminateByte);
            var res = Communication.SendPacket(terminateRequest);

            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);

                Communication.Close();
                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        #region Read Methods

        /// <summary>PSEM's full read request</summary>
        /// <param name="tableID">The table identifier.</param>
        /// <param name="resultData">The result data.</param>
        /// <returns>L7 response of the opearation</returns>
        public L7Response Read(ushort tableID, out byte[] resultData)
        {
            Log.Info($"Reading [{tableID}]...");
            var l7res = L7Response.RSPINVALID;
            var retries = 0;
            var intIndex = 0;
            resultData = null;

            var dataPacket = new byte[3];

            // Service request
            dataPacket[intIndex++] = (byte)PSEMStandards.Services.READ_FULL;

            // Table Id
            dataPacket[intIndex++] = (byte)(tableID >> 8);
            dataPacket[intIndex++] = (byte)(tableID & 0x00FF);

            RETRY_READ:
            var readRequest = GeneratePacket(dataPacket);
            var res = Communication.SendPacket(readRequest);
            if (res == "OK")
            {
                intIndex = 0;
                byte[] tempResult;

                res = Communication.RecievePacket(out tempResult);

                if (tempResult == null && retries != 5)
                {
                    Log.Debug($"Failed to read [{tableID}]. Retry # {retries}...");
                    retries++;
                    Thread.Sleep(300);
                    goto RETRY_READ;
                }
                if (tempResult == null)
                    return L7Response.RSPINVALID;
                var Result = tempResult[intIndex++];
                if (Result == 0x06)
                {
                    var offset = BitConverter.ToInt16(tempResult, 8) >> 8;

                    var hex = BitConverter.ToString(tempResult);
                    if (tempResult.Length < 7)
                    {
                        return L7Response.RSPINVALID;
                    }
                    else
                    {
                        if (offset >= 0)
                        {
                            // Get the data portion
                            resultData = tempResult.SubArray(10, offset);
                        }
                        return (L7Response)tempResult[7];
                    }
                }
                return L7Response.RSPINVALID;
            }
            else
            {
                resultData = null;
                return l7res;
            }
        }

        /// <summary>PSEM's partial read request</summary>
        /// <param name="tableID">The table identifier.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="resultData">The result data.</param>
        /// <returns>Opeartion result</returns>
        public L7Response ReadPartial(ushort tableID, int offset, ushort length, out byte[] resultData)
        {
            Log.Info($"Reading offset [{tableID}]...");
            resultData = null;
            var l7res = L7Response.RSPINVALID;
            var retries = 0;
            var intIndex = 0;

            var dataPacket = new byte[8];

            // Service request
            dataPacket[intIndex++] = (byte)PSEMStandards.Services.READ_OFFSET;

            // Table Id
            dataPacket[intIndex++] = (byte)(tableID >> 8);
            dataPacket[intIndex++] = (byte)(tableID & 0x00FF);
            dataPacket[intIndex++] = (byte)(offset >> 16);
            dataPacket[intIndex++] = (byte)(offset >> 8);
            dataPacket[intIndex++] = (byte)(offset & 0xFF);
            dataPacket[intIndex++] = (byte)(length >> 8);
            dataPacket[intIndex++] = (byte)(length & 0xFF);

            RETRY_READ:
            var readRequest = GeneratePacket(dataPacket); Log.Debug($"offset read:  {BitConverter.ToString(dataPacket)}");
            var res = Communication.SendPacket(readRequest);
            if (res == "OK")
            {
                intIndex = 0;
                byte[] tempResult;

                res = Communication.RecievePacket(out tempResult);

                if (tempResult == null && retries != 3)
                {
                    Log.Debug($"Failed to read [{tableID}]. Retry # {retries}...");
                    retries++;
                    goto RETRY_READ;
                }

                if (tempResult != null && tempResult[0] == 0x06)
                {
                    Log.Debug($"offset read:  {BitConverter.ToString(tempResult)}");
                    var offsetData = tempResult.Length - 12;

                    if (tempResult.Length < 7)
                    {
                        return L7Response.RSPINVALID;
                    }
                    else
                    {
                        if (offsetData >= 0)
                        {
                            // Get data portiojn
                            resultData = tempResult.SubArray(10, offsetData);
                        }
                        return (L7Response)tempResult[7];
                    }
                }
                return L7Response.RSPINVALID;
            }
            else
            {
                resultData = null;
                return l7res;
            }
        }

        #endregion Read Methods

        #region Write Methods

        /// <summary>
        /// Writes data in an offset manner to meter table.
        /// </summary>
        /// <param name="tableID">The table ID.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="data">The data.</param>
        /// <returns>A string.</returns>
        public string WriteOffset(ushort tableID, int offset, byte[] data)
        {
            byte[] resultData = null;
            var intIndex = 0;

            // Prep packet
            var dataPacket = new byte[9 + data.Length];
            dataPacket[intIndex++] = (byte)PSEMStandards.Services.WRITE_OFFSET;
            dataPacket[intIndex++] = (byte)(tableID >> 8);
            dataPacket[intIndex++] = (byte)(tableID & 0x00FF);
            dataPacket[intIndex++] = (byte)(offset >> 16);
            dataPacket[intIndex++] = (byte)(offset >> 8);
            dataPacket[intIndex++] = (byte)(offset & 0xFF);
            dataPacket[intIndex++] = (byte)(data.Length >> 8);
            dataPacket[intIndex++] = (byte)(data.Length & 0xFF);

            // Merge array
            Array.Copy(data, 0, dataPacket, intIndex, data.Length);

            // Move to the end of the data packet to get the CRC
            intIndex += data.Length;
            dataPacket[intIndex++] = GenerateChkSum(data, 0, data.Length);

            var writeRequest = GeneratePacket(dataPacket);
            var res = Communication.SendPacket(writeRequest);
            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);
                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        /// <summary>PSEM's full write. It writes data to the specified table.</summary>
        /// <param name="tableID">The table identifier.</param>
        /// <param name="data">The data.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public string Write(ushort tableID, byte[] data)
        {
            byte[] resultData = null;
            var intIndex = 0;

            var dataPacket = new byte[6 + data.Length];
            dataPacket[intIndex++] = (byte)PSEMStandards.Services.WRITE_FULL;
            dataPacket[intIndex++] = (byte)(tableID >> 8);
            dataPacket[intIndex++] = (byte)(tableID & 0x00FF);
            dataPacket[intIndex++] = (byte)(data.Length >> 8);
            dataPacket[intIndex++] = (byte)(data.Length & 0xFF);

            Array.Copy(data, 0, dataPacket, intIndex, data.Length);
            intIndex += data.Length;

            dataPacket[intIndex++] = GenerateChkSum(data, 0, data.Length);

            var writeRequest = GeneratePacket(dataPacket);
            var res = Communication.SendPacket(writeRequest);
            if (res == "OK")
            {
                res = Communication.RecievePacket(out resultData);
                if (resultData == null)
                {
                    return "NO RESPONSE";
                }
                else
                {
                    return res;
                }
            }
            else
            {
                resultData = null;
                return res;
            }
        }

        #endregion Write Methods

        /// <summary>Generates the CHK sum.</summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        private byte GenerateChkSum(byte[] buffer, int start, int count)
        {
            var index = 0;
            var chksSumIndex = 0;

            for (index = 0; index < count; index++)
                chksSumIndex += buffer[start + index];

            chksSumIndex = ~chksSumIndex + 1;
            chksSumIndex &= 0x000000FF;

            return (byte)chksSumIndex;
        }

        #region Generate TX Packet Method

        /// <summary>
        /// Generates the packet.
        /// </summary>
        /// <param name="l7Packet">The l7 packet.</param>
        /// <returns>An array of byte.</returns>
        private byte[] GeneratePacket(byte[] l7Packet)
        {
            bool singlePacket;
            bool multiPacket;
            byte sequenceNumber;
            ushort usPacketLength = 0;
            var intIndex = 0;

            // L2 data packet
            var l2Packet = new byte[l7Packet.Length];

            // Full TX Packet
            byte[] txPacket;
            var txPacketTemp = new byte[PSEMStandards.DEFAULT_PACKET_SIZE];

            Array.Copy(l7Packet,
                 0,
                 l2Packet,
                 0,
                 l2Packet.Length);

            // Handle Sequence Number
            var packetCount = (byte)(l2Packet.Length /
                                     (PSEMStandards.DEFAULT_PACKET_SIZE - PSEMStandards.DEFAULT_OVERHHEAD_SIZE));
            if ((packetCount * (PSEMStandards.DEFAULT_PACKET_SIZE - PSEMStandards.DEFAULT_OVERHHEAD_SIZE)) != l2Packet.Length)
            {
                packetCount++;
            }
            sequenceNumber = 0x00;

            // TODO: Library can send only single packets for now
            multiPacket = false;
            singlePacket = true;

            // End of "Handle Sequence Number"

            // STP
            txPacketTemp[intIndex++] = PSEMStandards.STP;

            // RESERVED BYTE
            txPacketTemp[intIndex++] = PSEMStandards.BYTE_RESERVED;

            // CONTROL BYTE

            txPacketTemp[intIndex] = 0;
            if ((multiPacket) || (sequenceNumber > 0))
            {
                txPacketTemp[intIndex] |= PSEMStandards.MULTI_PACKET;
            }
            if ((singlePacket) && (sequenceNumber > 0))
            {
                txPacketTemp[intIndex] |= PSEMStandards.SINGLE_PACKET;
            }

            if (stateToggleByte == 0x01)
            {
                txPacketTemp[intIndex] |= PSEMStandards.CONTROL_TOGGLE_BIT;
            }
            if (0x01 == stateToggleByte)
            {
                stateToggleByte = 0x00;
            }
            else
            {
                stateToggleByte = 0x01;
            }
            intIndex++;

            // SEQUENCE BYTE
            txPacketTemp[intIndex++] = sequenceNumber;
            // TODO: for multipacket
            if (sequenceNumber > 0)
            {
                usPacketLength = PSEMStandards.DEFAULT_PACKET_SIZE;
            }
            else if (l2Packet.Length >= PSEMStandards.DEFAULT_PACKET_SIZE - PSEMStandards.DEFAULT_OVERHHEAD_SIZE)
            {
                // Special case for boundary condition.
                usPacketLength = PSEMStandards.DEFAULT_PACKET_SIZE;
            }
            else
            {
                usPacketLength = (ushort)(PSEMStandards.DEFAULT_OVERHHEAD_SIZE + (l2Packet.Length % (PSEMStandards.DEFAULT_PACKET_SIZE - PSEMStandards.DEFAULT_OVERHHEAD_SIZE)));
            }

            Write16Bits(ref txPacketTemp, intIndex, (ushort)(usPacketLength - PSEMStandards.DEFAULT_OVERHHEAD_SIZE));
            intIndex += 2;

            Array.Copy(l2Packet, 0, txPacketTemp, intIndex, usPacketLength - PSEMStandards.DEFAULT_OVERHHEAD_SIZE);
            intIndex += (usPacketLength - (ushort)PSEMStandards.DEFAULT_OVERHHEAD_SIZE);

            if (l2Packet.Length > (usPacketLength - (ushort)PSEMStandards.DEFAULT_OVERHHEAD_SIZE))
            {
                // Remove the bytes that are being sent
                var temp = new byte[l2Packet.Length - usPacketLength + (ushort)PSEMStandards.DEFAULT_OVERHHEAD_SIZE];
                Array.Copy(l2Packet, usPacketLength - (ushort)PSEMStandards.DEFAULT_OVERHHEAD_SIZE, temp, 0, temp.Length);
                l2Packet = new byte[temp.Length];
                Array.Copy(temp, 0, l2Packet, 0, l2Packet.Length);
            }

            var usCRC = (ushort)(CRC.CalcCRC(txPacketTemp, 0, (ushort)((usPacketLength - (ushort)PSEMStandards.CRC_LENGTH))));
            Write16Bits(ref txPacketTemp, intIndex, usCRC);
            intIndex += 2;

            txPacket = new byte[intIndex];
            Array.Copy(txPacketTemp, 0, txPacket, 0, txPacket.Length);

            return txPacket;
        }

        #endregion Generate TX Packet Method

        #region BaudRate to Byte conversion Method

        /// <summary>
        /// Baud rate to byte.
        /// </summary>
        /// <param name="intRate">The int rate.</param>
        /// <returns>A BaudRates.</returns>
        private BaudRates BaudRateToByte(int intRate)
        {
            BaudRates BaudRate;

            switch (intRate)
            {
                case 300:
                    {
                        BaudRate = BaudRates.BR_300;
                        break;
                    }
                case 600:
                    {
                        BaudRate = BaudRates.BR_600;
                        break;
                    }
                case 1200:
                    {
                        BaudRate = BaudRates.BR_1200;
                        break;
                    }
                case 2400:
                    {
                        BaudRate = BaudRates.BR_2400;
                        break;
                    }
                case 4800:
                    {
                        BaudRate = BaudRates.BR_4800;
                        break;
                    }
                case 9600:
                    {
                        BaudRate = BaudRates.BR_9600;
                        break;
                    }
                case 14400:
                    {
                        BaudRate = BaudRates.BR_14400;
                        break;
                    }
                case 19200:
                    {
                        BaudRate = BaudRates.BR_19200;
                        break;
                    }
                case 28800:
                    {
                        BaudRate = BaudRates.BR_28800;
                        break;
                    }
                case 38400:
                    {
                        BaudRate = BaudRates.BR_38400;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Unknown baud rate.", "uintRate");
                    }
            }

            return BaudRate;
        }

        #endregion BaudRate to Byte conversion Method

        #region Method to write data length to TX Packet

        /// <summary>
        /// Write16S the bits.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        protected void Write16Bits(ref byte[] buffer, int index, ushort value)
        {
            // AND and shift bits
            buffer[index] = (byte)((value & 0xff00) >> 8);

            // AND the next element
            buffer[index + 1] = (byte)(value & 0xff);
        }

        #endregion Method to write data length to TX Packet
    }
}