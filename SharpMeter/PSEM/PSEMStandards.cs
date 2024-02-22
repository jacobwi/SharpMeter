using System.Collections.Generic;

namespace SharpMeter.PSEM
{
    /// <summary>
    /// The PSEM standards class. Contains L2 and L7 logic layers for communication with meters.
    /// </summary>
    public static class PSEMStandards
    {
        /// <summary>
        /// Starting byte of a packet.
        /// </summary>
        public const byte STP = 0xEE;

        /// <summary>
        /// Default packet size.
        /// </summary>
        public const int DEFAULT_PACKET_SIZE = 64;

        /// <summary>
        /// Default byte reserved.
        /// </summary>
        public const byte BYTE_RESERVED = 0x0;

        /// <summary>
        /// Last two bytes in a packet. Used to verify the packet data.
        /// </summary>
        public const int CRC_LENGTH = 2;

        /// <summary>
        /// The header size in a packet
        /// </summary>
        public const uint DEFAULT_OVERHHEAD_SIZE = 8;

        /// <summary>
        /// Packet Control Bit/Byte.
        /// </summary>
        public const byte CONTROL_TOGGLE_BIT = 0x20;

        /// <summary>
        /// Single p a c k e t byte.
        /// </summary>
        public const byte SINGLE_PACKET = 0x40;

        /// <summary>
        /// Multi p a c k e t byte.
        /// </summary>
        public const byte MULTI_PACKET = 0x80;

        /// <summary>
        /// Default max packet length.
        /// </summary>
        public const ushort DEFAULT_MAX_PACKET_LEGNTH = 0x80;

        /// <summary>
        /// Default max number of packets.
        /// </summary>
        public const ushort DEFAULT_MAX_NUMBER_OF_PACKETS = 0xFE;

        /// <summary>
        /// PSEM services/commands.
        /// </summary>
        internal enum Services : byte
        {
            IDENTIFY = 0x20,
            TERMINATE = 0x21,
            DISCONNECT = 0x22,
            READ_FULL = 0x30,
            READ_INDEX = 0x31,
            READ_DEFAULT = 0x3E,
            READ_OFFSET = 0x3F,
            WRITE_FULL = 0x40,
            WRITE_INDEX = 0x41,
            WRITE_OFFSET = 0x4F,
            LOGON = 0x50,
            SECURITY = 0x51,
            LOGOFF = 0x52,
            AUTHENTICATE = 0x53,
            NEGOTIATE_NO_BAUD = 0x60,
            WAIT = 0x70,
            TIMING_SETUP = 0x71
        }

        /// <summary>
        /// L7 response.
        /// </summary>
        public enum L7Response : byte
        {
            /// <summary>
            /// No Error
            /// </summary>
            OK = 0x00,

            /// <summary>
            /// Unspecified Error
            /// </summary>
            ERR = 0x01,

            /// <summary>
            /// Service Not Supported
            /// </summary>
            SNS = 0x02,

            /// <summary>
            /// Insufficient Security Clearance
            /// </summary>
            ISC = 0x03,

            /// <summary>
            /// Operation Not Possible
            /// </summary>
            ONP = 0x04,

            /// <summary>
            /// Inappropriate Action Requested
            /// </summary>
            IAR = 0x05,

            /// <summary>
            /// Device's Busy
            /// </summary>
            BSY = 0x06,

            /// <summary>
            /// Data Not Ready
            /// </summary>
            DNR = 0x07,

            /// <summary>
            /// Data Locked
            /// </summary>
            DLK = 0x08,

            /// <summary>
            /// Renegotiate Request
            /// </summary>
            RNO = 0x09,

            /// <summary>
            /// Invalid Service Sequence State
            /// </summary>
            ISSS = 0x0A,

            /// <summary>
            /// INVALID L7 Response
            /// </summary>
            RSPINVALID = 0xE0,

            /// <summary>
            /// Invalid CRC / Total data size
            /// </summary>
            RSPCHECKSUM = 0xF0,
        }

        /// <summary>
        /// Translations of L7 reponses.
        /// </summary>
        public static Dictionary<L7Response, string> L7ResponseString = new Dictionary<L7Response, string>
        {
            {L7Response.OK , "OK - No Error" },
            {L7Response.ERR , "ERR - Unspecified Error" },
            {L7Response.SNS , "SNS - Service Not Supported" },
            {L7Response.ISC , "ISC - Insufficient Security Clearance" },
            {L7Response.ONP , "ONP - Operation Not Possible" },
            {L7Response.IAR , "IAR - Inappropriate Action Requested" },
            {L7Response.BSY , "BSY - Device's Busy" },
            {L7Response.DNR , "DNR - Data Not Ready" },
            {L7Response.DLK , "DLK - Data Locked" },
            {L7Response.RNO , "RNO - Renegotiate Request" },
            {L7Response.ISSS , "ISSS - Invalid Service Sequence State" },
            {L7Response.RSPINVALID , "RSPINVALID - INVALID L7 Response" },
            {L7Response.RSPCHECKSUM , "RSPCHECKSUM - Invalid CRC" },
        };

        /// <summary>
        /// Procedure response.
        /// </summary>
        public enum ProcedureResponse : byte
        {
            /// <summary>
            /// Complete response.
            /// </summary>
            COMPLETED = 0x00,

            /// <summary>
            /// Accepeted response. Note: Some procedures produce accept instead of complete.
            /// </summary>
            ACCEPTED = 0x01,

            /// <summary>
            /// Invalid response.
            /// </summary>
            INVALID = 0x02,

            /// <summary>
            /// Conflicted response.
            /// </summary>
            CONFLICTED = 0x03,

            /// <summary>
            /// Timeout response.
            /// </summary>
            TIMED = 0x04,

            /// <summary>
            /// Unauthenticated response. Usually due to invalid password or limited access level.
            /// </summary>
            UNAUTH = 0x05,

            /// <summary>
            /// Unknown response.
            /// </summary>
            UNKNOWN = 0x06,
        }
    }
}