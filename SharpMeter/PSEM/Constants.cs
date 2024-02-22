namespace SharpMeter.PSEM
{
    /// <summary>
    /// PSEM constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The target.
        /// </summary>
        public enum Target
        {
            /// <summary>
            /// Gas meter.
            /// </summary>
            GAS = 0,

            /// <summary>
            /// Water meter.
            /// </summary>
            WATER = 1,

            /// <summary>
            /// Electric meter.
            /// </summary>
            ELECTRIC = 2,

            /// <summary>
            /// Dev meter.
            /// </summary>
            DEV = 3,
        }

        /// <summary>
        /// The access level.
        /// </summary>
        public enum AccessLevel : int
        {
            /// <summary>
            /// No Access.
            /// </summary>
            NoAccess = 0,

            /// <summary>
            /// Customer Access.
            /// </summary>
            Customer,

            /// <summary>
            /// Reader Access.
            /// </summary>
            Reader,

            /// <summary>
            /// Master/Admin Access.
            /// </summary>
            Master
        }

        /// <summary>
        /// The model.
        /// </summary>
        ///

        // La la.... need to figure out a way to identify meter... possibly firmware part number?
        public enum Model : int
        {
            GENERIC,
            I210,
            I210Plus,
            I210PlusC = 5,
            KV,
            KV2CEps,
            KV2CGen5,
            SM111,
            SM112,
            SM301
        }

        /// <summary>
        /// The configuration.
        /// </summary>
        public enum Configuration
        {
            /// <summary>
            /// Unconfigured mode.
            /// </summary>
            Unconfigured,

            /// <summary>
            /// Uncalibrated mode.
            /// </summary>
            Uncalibrated,

            /// <summary>
            /// Demand mode [Configured].
            /// </summary>
            DemandMode,
        }

        /// <summary>
        /// The mode.
        /// </summary>
        public enum Mode : int
        {
            /// <summary>
            /// Demand only mode.
            /// </summary>
            DemandOnly,

            /// <summary>
            /// Demand with load profile mode.
            /// </summary>
            DemandPlusLoadProfile,

            /// <summary>
            /// Time of use mode.
            /// </summary>
            TimeOfUse
        }

        /// <summary>
        /// The wireless communication device.
        /// </summary>
        public enum AMIDevice
        {
            ITRON,
            LAndG,
            SRFN,
            METRUM,
            UMT,
            VERIZON,
            ATT,
            TURTLE,
            TRILLIANT
        }

        /// <summary>
        /// The softswitch upgrades.
        /// </summary>
        public enum SoftswitchUpgrades : int
        {
            /// <summary>
            /// Time of use. Used to enable clock table.
            /// </summary>
            TOU,

            /// <summary>
            /// Second measure.
            /// </summary>
            SecondMeasures,

            /// <summary>
            /// Basic recording.
            /// </summary>
            BasicRecording,

            /// <summary>
            /// Event log.
            /// </summary>
            EventLog,

            /// <summary>
            /// Wireless/Alternate communication board.
            /// </summary>
            AMI,

            /// <summary>
            /// Digital signal processing.
            /// </summary>
            DSP,

            /// <summary>
            /// Pulse initialization output.
            /// </summary>
            PulseInitOutput,

            /// <summary>
            /// Demand.
            /// </summary>
            Demand,

            TwentyChannelRecording,
            TransformerLossCompensation,
            TransformerAccuracyAdjustment,
            RevenueGuard,
            VoltageEventMonitor,
            BiDirectional,
            WaveformCapture,
            ExpandedMeasure,
            VoltageMesasurement,
            Totalization,
            HugeTwentyChannelRecording,
            ECP = 23,
            DLP,
            PPM,
            MagTamperDetect
        }

        /// <summary>
        /// The standard status.
        /// </summary>
        public enum StandardStatus : int
        {
            Config = 1,
            SelfCheck = 2,
            RAM = 3,
            ROM = 4,
            NonVolMemory = 5,
            Clock = 6,
            Measure = 7,
            LowBattery = 8,
            LowLossPotential = 9,
            DemandOverload = 10,
            PowerFail = 11,
            Tamper = 12,
            All = 99
        }

        /// <summary>
        /// The MFG status.
        /// </summary>
        public enum MFGStatus : int
        {
            MeterError,
            WatchdogTimeout = 2,
            RXKwh,
            LeadingKVarh,
            LossOfProgram,
            HighTemp,
            All = 99
        }

        /// <summary>
        /// The calibration control.
        /// </summary>
        public enum CalibrationControl : int
        {
            EnterCalibration,
            StartCalibrationTest,
            ExitCalibrationTest
        }

        /// <summary>
        /// Remote disconnect options for [Manufacturer Procedure 89: Remote Disconnect Switch Control].
        /// </summary>
        public enum RemoteDisconnect : int
        {
            CloseManual = 1,
            CloseAutomatic,
            Open,
            UpdateStatus,
            OutageOpen,
            SampleChargeCapacitorVoltage,
            PerformTest,
            ResetSwitchBoard
        }

        /// <summary>
        /// The direct load.
        /// </summary>
        public enum DirectLoad : int
        {
            RD = 1,
            ECP,
            DLP = 4,
            PPM = 8,
            Lockout = 16,
            LockoutRD,
            RDManual = 32,
            All = 47
        }

        /// <summary>
        /// The energy type.
        /// </summary>
        public enum EnergyType : int
        {
            Energy,
            Quadergy,
            ApparentVAh,
            PhasorApparentVAh,
            ArthmeticApparentVAh
        }
    }
}