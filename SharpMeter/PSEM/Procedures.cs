using SharpMeter.PSEM.Models;
using SharpMeter.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static SharpMeter.LibraryConstants;
using static SharpMeter.PSEM.Constants;
using static SharpMeter.PSEM.PSEMStandards;

namespace SharpMeter.PSEM
{
    /// <summary>
    /// The procedures class contains methods to invoke MFG or STD procedures to the meter.
    /// </summary>
    public class Procedures
    {
        /// <summary>
        /// Gets or sets the serial communication class object.
        /// </summary>
        public ICommunication Communication { get; set; }

        /// <summary>
        /// Gets or sets the PSEM services class object.
        /// </summary>
        public PSEMMeter PS { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Procedures"/> class.
        /// </summary>
        /// <param name="communication">Communication object.</param>
        /// <param name="ps">PSEM services class object.</param>
        public Procedures(ICommunication communication, PSEMMeter ps)
        {
            this.Communication = communication;
            this.PS = ps;
        }
        public Procedures()
        {

        }
        public void Initialize(ICommunication communication, PSEMMeter ps)
        {
            this.Communication = communication;
            this.PS = ps;
        }
        #region Raw Executes

        /// <summary>
        /// Execute procedure to the device
        /// </summary>
        /// <param name="ProcedureID">Procedure number</param>
        /// <param name="parameters">parameters to be passed along</param>
        /// <param name="isMP">a boolean value to indicates standard or manf. procedure</param>
        /// <returns>Procedure result type</returns>
        public ProcedureResponse Execute(ushort ProcedureID, byte[] parameters, bool isMP = false)
        {
            byte[] results;

            var data = new byte[3];

            ProcedureID = (ushort)(isMP ? (ProcedureID + 2048) : ProcedureID);

            ProcedureID = (ushort)(ProcedureID & 0xFFF | 0 << 12);

            Log.Trace($"Executing procedure - ID [{ProcedureID}]");
            Log.Trace($"Executing procedure - Type [{(isMP ? "Manufacture" : "Standard")}]");

            var procIDBytes = BitConverter.GetBytes(ProcedureID);

            var bits = new BitArray(procIDBytes);

            Array.Copy(bits.ConvertToBytes(), 0, data, 0, 2);

            data[2] = 7;

            if (parameters != null)
                data = data.Concat(parameters).ToArray();
            Log.Trace($"Executing procedure - Parameters [{BitConverter.ToString(data)}]");
            PS.Write(7, data);

            var read = PS.Read(8, out results);

            if (results == null)
            {
                Log.Trace($"Executing procedure - ST8 Result [Null]");
                if (read != L7Response.OK)
                {
                    if (read == L7Response.ISC)
                    {
                        return ProcedureResponse.UNAUTH;
                    }
                    else
                    {
                        return ProcedureResponse.INVALID;
                    }
                }
                return ProcedureResponse.UNKNOWN;
            }
            else
            {
                Log.Trace($"Executing procedure - ST8 Result [{BitConverter.ToString(results)}]");
                if (read != L7Response.OK)
                {
                    if (read == L7Response.ISC)
                    {
                        return ProcedureResponse.UNAUTH;
                    }
                    else
                    {
                        return ProcedureResponse.INVALID;
                    }
                }
                if (results.Length > 2)
                {
                    Communication.SendPacket(new byte[1] { 0x06 });
                    return (ProcedureResponse)results[3];
                }
                else
                {
                    return ProcedureResponse.UNKNOWN;
                }
            }
        }

        /// <summary>Execute procedure to the device.</summary>
        /// <param name="ProcedureID">The procedure identifier.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="results">The results.</param>
        /// <param name="isMP">if set to <c>true</c> [is mp].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse Execute(ushort ProcedureID, byte[] parameters, out byte[] results, bool isMP = false)
        {
            results = null;

            var data = new byte[3];

            ProcedureID = (ushort)(isMP ? (ProcedureID + 2048) : ProcedureID);
            ProcedureID = (ushort)(ProcedureID & 0xFFF | 0 << 12);

            Log.Trace($"Executing procedure - ID [{ProcedureID}]");
            Log.Trace($"Executing procedure - Type [{(isMP ? "Manufacture" : "Standard")}]");

            var procIDBytes = BitConverter.GetBytes(ProcedureID);

            var bits = new BitArray(procIDBytes);

            Array.Copy(bits.ConvertToBytes(), 0, data, 0, 2);

            data[2] = 7;

            if (parameters != null)
                data = data.Concat(parameters).ToArray();

            Log.Trace($"Executing procedure - Parameters [{BitConverter.ToString(data)}]");

            PS.Write(7, data);

            var read = PS.Read(8, out results);

            if (results == null)
            {
                Log.Trace($"Executing procedure - ST8 Result [Null]");
                if (read != L7Response.OK)
                {
                    if (read == L7Response.ISC)
                    {
                        return ProcedureResponse.UNAUTH;
                    }
                    else
                    {
                        return ProcedureResponse.INVALID;
                    }
                }
                return ProcedureResponse.UNKNOWN;
            }
            else
            {
                Log.Trace($"Executing procedure - ST8 Result [{BitConverter.ToString(results)}]");
                if (read != L7Response.OK)
                {
                    if (read == L7Response.ISC)
                    {
                        return ProcedureResponse.UNAUTH;
                    }
                    else
                    {
                        return ProcedureResponse.INVALID;
                    }
                }
                if (results.Length > 2)
                {
                    Communication.SendPacket(new byte[1] { 0x06 });

                    return (ProcedureResponse)results[3];
                }
                else
                {
                    return ProcedureResponse.UNKNOWN;
                }
            }
        }

        #endregion Raw Executes

        #region DateTime Procedures

        /// <summary>Sets the date time.</summary>
        /// <param name="dt">The dt.</param>
        /// <param name="isDST">if set to <c>true</c> [is DST].</param>
        /// <param name="isGMT">if set to <c>true</c> [is GMT].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse SetDateTime(DateTime dt, bool isDST = false, bool isGMT = false)
        {
            var parameters = new byte[8];
            var bitArraySetter = new BitArray(8);
            bitArraySetter.Set(1, true);
            bitArraySetter.Set(1, false);
            bitArraySetter.Set(2, true);
            parameters[0] = bitArraySetter.ConvertToByte();

            // Date portion
            parameters[1] = (byte)(dt.Year % 100);
            parameters[2] = (byte)dt.Month;
            parameters[3] = (byte)dt.Day;

            // Time portion
            parameters[4] = (byte)dt.Hour;
            parameters[5] = (byte)dt.Minute;
            parameters[6] = (byte)dt.Second;

            // Week day, DST, and GMT portion
            var timeQual = (int)dt.DayOfWeek;
            var bitArr = new BitArray(new byte[1] { (byte)timeQual });
            bitArr.Set(3, isDST ? true : false);
            bitArr.Set(4, isGMT ? true : false);
            parameters[7] = (byte)bitArr.ToInt();




            Log.Info($"Datetime: {dt.ToString("MM/dd/yyyy HH:mm")}");
            if (StartProgramming() == ProcedureResponse.COMPLETED)
            {
                var resProcedure = Execute(10, parameters);
                if (resProcedure != ProcedureResponse.COMPLETED)
                {
                    return resProcedure;
                }
              
                if (EndProgramming() == ProcedureResponse.COMPLETED)
                {
                    var season = (dt.Month % 12 + 3) / 3 - 1;
                    RemoteReset(season);
                    return resProcedure;
                }
                else
                {
                    return ProcedureResponse.INVALID;
                }
            }
            else
            {
                return ProcedureResponse.INVALID;
            }
        }

        public ProcedureResponse SetDateTimeFromGMT(int offset, bool isDST = false, bool isGMT = false)
        {
            var parameters = new byte[8];
            var dt = DateTime.UtcNow.AddHours(offset);
            var bitArraySetter = new BitArray(8);
            bitArraySetter.Set(1, true);
            bitArraySetter.Set(1, false);
            bitArraySetter.Set(2, true);
            parameters[0] = bitArraySetter.ConvertToByte();

            // Date portion
            parameters[1] = (byte)(dt.Year % 100);
            parameters[2] = (byte)dt.Month;
            parameters[3] = (byte)dt.Day;

            // Time portion
            parameters[4] = (byte)dt.Hour;
            parameters[5] = (byte)dt.Minute;
            parameters[6] = (byte)dt.Second;

            // Week day, DST, and GMT portion
            var timeQual = (int)dt.DayOfWeek;
            var bitArr = new BitArray(new byte[1] { (byte)timeQual });
            bitArr.Set(3, isDST ? true : false);
            bitArr.Set(4, isGMT ? true : false);
            parameters[7] = (byte)bitArr.ToInt();
            Log.Info($"Datetime: {dt.ToString("MM/dd/yyyy HH:mm")}");
            if (StartProgramming() == ProcedureResponse.COMPLETED)
            {
                var resProcedure = Execute(10, parameters);
                if (resProcedure != ProcedureResponse.COMPLETED)
                {
                    return resProcedure;
                }


            
                if (EndProgramming() == ProcedureResponse.COMPLETED)
                {
                    var season = (dt.Month % 12 + 3) / 3 - 1;
                    RemoteReset(season);
                    return resProcedure;
                }
                else
                {
                    return ProcedureResponse.INVALID;
                }
            }
            else
            {
                return ProcedureResponse.INVALID;
            }
           
        }

        /// <summary>Sets the date time to current.</summary>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse SetDateTimeToCurrent()
        {
            var dt = DateTime.Now;
            var parameters = new byte[8];

            parameters[0] = 0x7;

            // Date portion
            parameters[1] = (byte)(dt.Year % 100);
            parameters[2] = (byte)dt.Month;
            parameters[3] = (byte)dt.Day;

            // Time portion
            parameters[4] = (byte)dt.Hour;
            parameters[5] = (byte)dt.Minute;
            parameters[6] = (byte)dt.Second;

            // Week day, DST, and GMT portion
            var timeQual = (int)dt.DayOfWeek;
            var bitArr = new BitArray(new byte[1] { (byte)timeQual });
            bitArr.Set(3, dt.IsDaylightSavingTime() ? true : false);

            parameters[7] = (byte)bitArr.ToInt();

            Execute(10, parameters);

            var season = (dt.Month % 12 + 3) / 3 - 1;
            RemoteReset(season);
            return ProcedureResponse.COMPLETED;
        }

        /// <summary>Remote reset.</summary>
        /// <param name="season">The season.</param>
        /// <param name="demandReset">if set to <c>true</c> [demand reset].</param>
        /// <param name="selfRead">if set to <c>true</c> [self read].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse RemoteReset(int season=0, bool demandReset = false, bool selfRead = false)
        {
           
            var bits = new BitArray(8);

            bits.SetAll(false);
            bits.Set(0, true);
            //if (demandReset)
            //    value |= 0x1;
            //if (selfRead)
            //    value |= 0x2;

            //if (season >= 0)
            //    value |= 0x4 | season << 3 & 0x78;

            return Execute(9, new byte[1] { (byte)bits.ToInt() });
        }

        #endregion DateTime Procedures

        #region Change Config/Mode Related

        /// <summary>
        /// Changes the meter mode.
        /// Manufacturer Procedure 67: Convert Meter Mode
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ChangeMeterMode(Mode mode)
        {
            var parameters = new byte[1] { (byte)mode };

            if (StartProgramming() == ProcedureResponse.COMPLETED)
            {
                var resProcedure = Execute(67, parameters, true);

                if (EndProgramming() == ProcedureResponse.COMPLETED)
                {
                    return resProcedure;
                }
                else
                {
                    return ProcedureResponse.UNKNOWN;
                }
            }
            else
            {
                return ProcedureResponse.UNKNOWN;
            }
        }

        /// <summary>Changes the configuration status.</summary>
        /// <param name="configuration">The configuration.</param>
        /// Manufacturer Procedure 66: Change Configuration Status
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ChangeConfigStatus(Configuration configuration)
        {
            var parameters = new byte[1] { (byte)configuration };

            var resProcedure = Execute(66, parameters, true);

            return resProcedure;
        }

        #endregion Change Config/Mode Related

        #region Load Profile Related

        /// <summary>
        /// Loads the profile recording.
        /// Standard Procedure 16: Start Load Profile Recording
        /// Standard Procedure 17: Stop Load Profile Recording
        /// </summary>
        /// <param name="isStart">if set to <c>true</c> [is start].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse LoadProfileRecording(bool isStart)
        {
            var resProcedure = Execute((isStart ? (ushort)16 : (ushort)17), null, false);

            return resProcedure;
        }

        #endregion Load Profile Related

        /// <summary>
        /// Colds the start.
        /// Standard Procedure 00: Cold Start
        /// </summary>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ColdStart()
        {
            var resProcedure = Execute(0, null, false);

            return resProcedure;
        }

        /// <summary>
        ///   <para>
        /// Tests the battery.</para>
        ///   <para>
        ///     <font color="#333333">[True] = Pass</font>
        ///   </para>
        ///   <para>
        ///     <font color="#333333">[False] = Fail</font>
        ///   </para>
        ///
        /// Manufacturer Procedure 03: Battery Test
        /// </summary>
        /// <returns>Test result</returns>
        public bool TestBattery()
        {
            byte[] procedureRes;
            var resProcedure = Execute(3, null, out procedureRes, true);

            if (procedureRes == null)
                return false;
            else
            {
                if (procedureRes[4] == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #region Calibration Related

        /// <summary>
        /// Calibrations the mode control.
        /// Manufacturer Procedure 73: Flash Calibration Mode Control
        /// </summary>
        /// <param name="cc">Calibration mode</param>
        /// <param name="duration">The duration.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">duration</exception>
        public ProcedureResponse CalibrationModeControl(CalibrationControl cc, int duration)
        {
            if (duration < 2)
            {
                throw new ArgumentOutOfRangeException("duration");
            }
            var parameters = new byte[2] { (byte)cc, (byte)duration };

            var resProcedure = Execute(73, parameters, true);

            if (resProcedure == ProcedureResponse.ACCEPTED || resProcedure == ProcedureResponse.COMPLETED)
            {
                // IsCalibration = true
            }
            return resProcedure;
        }

        /// <summary>
        /// Sets the last calibration date time.
        /// Manufacturer Procedure 75: Set Last Calibration Date/Time
        /// </summary>
        /// <param name="m">The meter object</param>
        /// <returns>
        ///   <br />
        /// </returns>
        /// <exception cref="System.Exception">Meter is not in calibration mode</exception>
        public ProcedureResponse SetLastCalibrationDateTime(Meter m)
        {
            if (!m.IsCalibration)
            {
                throw new Exception("Meter is not in calibration mode");
            }
            var dt = DateTime.Now;
            var parameters = new byte[5];

            parameters[0] = (byte)(dt.Year % 100);
            parameters[1] = (byte)dt.Month;
            parameters[2] = (byte)dt.Day;

            // Time portion
            parameters[3] = (byte)dt.Hour;
            parameters[4] = (byte)dt.Minute;

            var resProcedure = Execute(75, parameters, true);

            return resProcedure;
        }

        #endregion Calibration Related

        /// <summary>
        /// Sets the RTP.
        /// Manufacturer Procedure 78: RTP Control
        /// </summary>
        /// <param name="isRTP">if set to <c>true</c> [is RTP].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse SetRTP(bool isRTP)
        {
            var parameters = new byte[1];
            parameters[0] = isRTP ? (byte)1 : (byte)0;

            var resProcedure = Execute(78, parameters, true);

            return resProcedure;
        }

        /// <summary>
        /// Configures the test pulses.
        /// Manufacturer Procedure 64: Configure Test Pulses
        /// </summary>
        /// <param name="et">The energy type.</param>
        /// <param name="varhTimeout">The varh timeout.</param>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse ConfigureTestPulses(EnergyType et, int varhTimeout)
        {
            var parameters = new byte[1];

            parameters[0] = (byte)et;

            var varhTimeoutArray = BitConverter.GetBytes(varhTimeout);

            parameters = parameters.Concat(varhTimeoutArray).ToArray();
            if (varhTimeout < 0 || varhTimeout > 65535)
            {
                throw new ArgumentOutOfRangeException("varhTimeout - Must be between 0 or 65535");
            }

            var resProcedure = Execute(64, parameters, true);

            return resProcedure;
        }

        #region Reset Related

        /// <summary>
        /// Clears the data.
        /// Standard Procedure 03: Clear Data
        /// </summary>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ClearData()
        {
            var resProcedure = Execute(3, null, false);

            return resProcedure;
        }

        /// <summary>
        /// Clears the all pending tables.
        /// Standard Procedure 14: Clear All Pending Tables.
        /// [Programming Session Required]
        /// </summary>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse ResetManufacturerListPointers()
        {
            if (StartProgramming() == ProcedureResponse.COMPLETED)
            {
                var resProcedure = Execute(14, null, false);
                if (EndProgramming() == ProcedureResponse.COMPLETED)
                {
                    return resProcedure;
                }
                else
                {
                    return ProcedureResponse.UNKNOWN;
                }
            }
            else
            {
                return ProcedureResponse.UNKNOWN;
            }
        }

        /// <summary>
        /// Clears the all pending tables.
        /// Standard Procedure 14: Clear All Pending Tables.
        /// [Programming Session Required]
        /// </summary>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse ClearAllPendingTables()
        {
            if (StartProgramming() == ProcedureResponse.COMPLETED)
            {
                var resProcedure = Execute(14, null, false);
                if (EndProgramming() == ProcedureResponse.COMPLETED)
                {
                    return resProcedure;
                }
                else
                {
                    return ProcedureResponse.UNKNOWN;
                }
            }
            else
            {
                return ProcedureResponse.UNKNOWN;
            }
        }

        /// <summary>
        /// Resets the passwords.
        /// </summary>
        /// <param name="passwords">The passwords.</param>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse ResetPasswords(List<string> passwords)
        {
            if (passwords.Count != 3)
                throw new ArgumentOutOfRangeException("passwords");

            Console.ForegroundColor = ConsoleColor.Red;
            Log.Debug("Resesting meter's passwords...");

            Log.Trace($"Master: {passwords[0]}");
            Log.Trace($"Customer: {passwords[1]}");
            Log.Trace($"Read: {passwords[2]}");

            Console.ResetColor();
            var byParameter = new byte[1];
            var resProcedure = Execute(5, byParameter, true);
            // TODO: Not complete.... need to offset write to change the password

            // CHECK IF METER IS IN UNCONFIG MODE
            // WRITE TO OPTICAL AND AMI SEC TABLES
            // ELSE FAIL
            var passwoardsArr = new byte[31];
            var masterPassword = passwords[0].HexToByteArray();
            var customerPassword = passwords[1].HexToByteArray();
            var readPassword = passwords[2].HexToByteArray();

            Array.Copy(masterPassword, 0, passwoardsArr, 0, 10);
            Array.Copy(customerPassword, 0, passwoardsArr, 10, 10);
            Array.Copy(readPassword, 0, passwoardsArr, 20, 10);
            passwoardsArr[30] = 0x00;

            if (StartProgramming() == ProcedureResponse.COMPLETED)
            {
                PS.WriteOffset(2048 + 84, 0, passwoardsArr);
                PS.WriteOffset(2048 + 107, 0, passwoardsArr.Take(passwoardsArr.Length - 1).ToArray());

                if (EndProgramming() == ProcedureResponse.COMPLETED)
                {
                    return ProcedureResponse.COMPLETED;
                }
                else
                {
                    return ProcedureResponse.INVALID;
                }
            }
            else
            {
                return ProcedureResponse.INVALID;
            }
        }

        /// <summary>
        /// Resets the error history.
        /// Manufacturer Procedure 71: Reset Error History
        /// </summary>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ResetErrorHistory()
        {
            var resProcedure = Execute(71, null, true);

            return resProcedure;
        }

        /// <summary>
        /// Resets the standard flag.
        /// Manufacturer Procedure 00: Clear Individual Standard Status Flags
        /// </summary>
        /// <param name="ss">The standard flags.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ResetSTDFlag(StandardStatus ss)
        {
            var parameters = new byte[3] { 0, 0, 0 };
            var parametersBits = new BitArray(parameters);
            if (ss == StandardStatus.All)
            {
                parametersBits.SetAll(true);
                parametersBits.CopyTo(parameters, 0);
            }
            else
            {
                parametersBits.Set((int)ss, true);
            }
            var resProcedure = Execute(0, parameters, true);

            return resProcedure;
        }

        /// <summary>
        /// Resets the MFG flag.
        /// Manufacturer Procedure 01: Clear Individual Manufacturer Status Flags
        /// </summary>
        /// <param name="ms">The MFG flag.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse ResetMFGFlag(MFGStatus ms)
        {
            var parameters = new byte[1] { 0 };
            var parametersBits = new BitArray(parameters);
            if (ms == MFGStatus.All)
            {
                parametersBits.SetAll(true);
                parametersBits.CopyTo(parameters, 0);
            }
            else
            {
                parametersBits.Set((int)ms, true);
            }
            var resProcedure = Execute(1, parameters, true);

            return resProcedure;
        }

        #endregion Reset Related

        #region Softswitches

        /// <summary>
        /// Sets the softswitch.
        /// Manufacturer Procedure 68: Upgrade Meter Type (Enable Softswitch)
        /// Manufacturer Procedure 76: Remove Softswitch
        /// </summary>
        /// <param name="sw">The softswitch option.</param>
        /// <param name="isUpgrade">if set to <c>true</c> [is upgrade].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse SetSoftswitch(SoftswitchUpgrades sw, bool isUpgrade = true)
        {
            byte[] res;
            PS.Read(1, out res);
            if (res == null)
            {
                return ProcedureResponse.INVALID;
            }

            var field = PS.TableServices.FindField(1, res, "MFG_SERIAL_NUMBER");
            var sl = new SoftswitchLogic(field.Value, sw);

            byte[] parameters;
            var temp = new byte[1] { (byte)sw };
            parameters = temp.Concat(sl.Encrypt()).ToArray();

            var resProcedure = Execute(isUpgrade ? (ushort)68 : (ushort)76, parameters, true);

            return resProcedure;
        }

        #endregion Softswitches

        #region Relay Related

        /// <summary>
        /// Sets the relay.
        /// Manufacturer Procedure 89: Remote Disconnect Switch Control
        /// </summary>
        /// <param name="rd">The rd.</param>
        /// <param name="result">The result.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse SetRelay(RemoteDisconnect rd, out string result)
        {
            result = "";
            byte[] procedureRes = null;
            var parameters = new byte[1];
            parameters[0] = (byte)rd;

            var resProcedure = Execute(89, parameters, out procedureRes, true);
            if (procedureRes == null)
                result = "Error in changing relay phsyical status";
            else
            {
                if (procedureRes[4] == 1)
                {
                }
                else if (procedureRes[4] == 4)
                {
                    result = "Relay is already closed";
                }
                else if (procedureRes[4] == 0)
                {
                    if (rd == RemoteDisconnect.CloseManual || rd == RemoteDisconnect.CloseAutomatic)
                        result = $"Relay has successfully been closed";
                    else if (rd == RemoteDisconnect.Open || rd == RemoteDisconnect.OutageOpen)
                        result = $"Relay has successfully been opened";

                    // Update relay status
                    procedureRes = null;
                    parameters[0] = (byte)RemoteDisconnect.UpdateStatus;
                    resProcedure = Execute(89, parameters, out procedureRes, true);
                    if (procedureRes == null)
                        result = "Error in changing relay virtual status";
                    else
                    {
                        if (procedureRes[4] == 0)
                        {
                            result += " | Relay status has successfully been updated";
                        }
                        else
                        {
                            result += " | Relay status has failed to update";
                        }
                    }
                }
                else
                {
                    result = $"UNKNOWN RESULT [{procedureRes[4]}]";
                }
            }

            return resProcedure;
        }

        /// <summary>
        /// Directs the load.
        /// Standard Procedure 21: Direct Load Control
        /// </summary>
        /// <param name="isOn">if set to <c>true</c> [is on].</param>
        /// <param name="dl">The dl.</param>
        /// <param name="hours">The hours.</param>
        /// <param name="mins">The mins.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse DirectLoad(bool isOn, DirectLoad dl, int hours = 0, int mins = 0)
        {
            byte[] procedureRes = null;
            var parameters = new byte[5];
            parameters[0] = (byte)(isOn ? 100 : 0);
            parameters[1] = (byte)dl;
            parameters[2] = (byte)hours;
            parameters[3] = (byte)mins;
            parameters[4] = (byte)0;
            var resProcedure = Execute(21, parameters, out procedureRes, false);

            return resProcedure;
        }

        #endregion Relay Related

        #region Programming Procedures

        /// <summary>
        /// Starts the programming.
        /// Manufacturer Procedure 70: Program Control
        /// </summary>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse StartProgramming()
        {
            var dt = DateTime.Now;
            var parameters = new byte[6];
            parameters[0] = 0;
            parameters[1] = (byte)(dt.Year % 100);
            parameters[2] = (byte)dt.Month;
            parameters[3] = (byte)dt.Day;

            // Time portion
            parameters[4] = (byte)dt.Hour;
            parameters[5] = (byte)dt.Minute;

            var resProcedure = Execute(70, parameters, true);

            return resProcedure;
        }

        /// <summary>
        /// Ends the programming.
        /// Manufacturer Procedure 70: Program Control
        /// </summary>
        /// <param name="isClear">if set to <c>true</c> [is clear].</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public ProcedureResponse EndProgramming(bool isClear = false)
        {
            var dt = DateTime.Now;
            var parameters = new byte[6];
            parameters[0] = (byte)(isClear ? 4 : 3);
            parameters[1] = (byte)(dt.Year % 100);
            parameters[2] = (byte)dt.Month;
            parameters[3] = (byte)dt.Day;

            // Time portion
            parameters[4] = (byte)dt.Hour;
            parameters[5] = (byte)dt.Minute;

            var resProcedure = Execute(70, parameters, true);

            return resProcedure;
        }

        #endregion Programming Procedures

        /// <summary>
        /// Resets the cumulative power outage duration.
        /// Manufacturer Procedure 65: Reset Cumulative Power Outage Duration
        /// </summary>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse ResetCumulativePowerOutageDuration()
        {
            var resProcedure = Execute(65, null, true);

            return resProcedure;
        }

        /// <summary>
        /// Resets the lineside diagnostic counters.
        /// Manufacturer Procedure 69: Reset Line-side Diagnostic Counters
        /// </summary>
        /// <param name="diagonsticCounter">The diagonstic counter.</param>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse ResetLinesideDiagnosticCounters(byte diagonsticCounter)
        {
            var parameters = new byte[] { diagonsticCounter };
            var resProcedure = Execute(69, parameters, true);

            return resProcedure;
        }

        /// <summary>
        /// Waves the form capture.
        /// Manufacturer Procedure 72: Wave Form Capture
        /// </summary>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse WaveFormCapture()
        {
            var resProcedure = Execute(72, null, true);

            return resProcedure;
        }

        /// <summary>
        /// Downloads the mode.
        /// Manufacturer Procedure 83: Start Download
        /// </summary>
        /// <param name="isStart">If true, is start.</param>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse DownloadMode(bool isStart)
        {
            var parameters = new byte[] { (byte)(isStart ? 1 : 0) };
            var resProcedure = Execute(83, null, true);

            return resProcedure;
        }

        /// <summary>
        /// Downloads the manufacturer test code.
        /// Manufacturer Procedure 81: Download Manufacturer Test Code
        /// </summary>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse DownloadManufacturerTestCode()
        {
            var resProcedure = Execute(81, null, true);

            return resProcedure;
        }

        /// <summary>
        /// Snapshots the revenue data.
        /// Manufacturer Procedure 84: Snapshot Revenue Data
        /// </summary>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse SnapshotRevenueData()
        {
            var resProcedure = Execute(84, null, true);

            return resProcedure;
        }

        /// <summary>
        /// Directs the execute. [HIDDEN] [USE WITH CAUTION]
        /// Manufacturer Procedure 02: Direct Execute
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>A ProcedureResponse.</returns>
        public ProcedureResponse DirectExecute(byte[] address)
        {
            if (address.Length != 3)
            {
                throw new ArgumentException("address - Must be exactly 3 in length.");
            }

            var resProcedure = Execute(2, address, true);

            return resProcedure;
        }
    }
}