using SharpMeter.Utils;
using System.Linq;
using System.Text;
using static SharpMeter.PSEM.Constants;

namespace SharpMeter.PSEM
{
    /// <summary>
    /// The softswitch logic.
    /// </summary>
    internal class SoftswitchLogic
    {
        protected byte[] encryptionKey = new byte[16]; // Encryption key CHANGEME

        /// <summary>
        /// Gets or sets the meter serial number string.
        /// </summary>
        public string MeterSerialNumberString { get; set; }

        /// <summary>
        /// Gets or sets the meter serial number bytes.
        /// </summary>
        public byte[] MeterSerialNumberBytes { get; set; }

        /// <summary>
        /// Gets or sets the soft switch option for upgrade or downgrade.
        /// </summary>
        public SoftswitchUpgrades SoftSwitch { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftswitchLogic"/> class.
        /// </summary>
        /// <param name="meterSerialNumber">The meter serial number.</param>
        /// <param name="softswitch">The softswitch option.</param>
        public SoftswitchLogic(string meterSerialNumber, SoftswitchUpgrades softswitch)
        {
            this.MeterSerialNumberString = meterSerialNumber;
            this.SoftSwitch = softswitch;
            var bytes = Encoding.Default.GetBytes(meterSerialNumber);
            MeterSerialNumberBytes = bytes;
        }

        /// <summary>
        /// Encrypts meter's serial number + softswitch option using a key to produce a 4 bytes data packet for MT 68 or 76\
        ///
        /// See kvscript.c from KVT
        /// </summary>
        /// <returns>Bytes array</returns>
        public byte[] Encrypt()
        {
            // Zipped array to combine the serial number array and encryption keys together
            var resultBytes = new byte[33];

            // Softswitch option element (0)
            resultBytes[0] = (byte)((int)SoftSwitch % 4);
            var counter = 0;

            // Loop to zip serial number and encryption keys
            for (var i = 1; i <= resultBytes.Length; i = i + 2)
            {
                resultBytes[i] = encryptionKey[counter];
                resultBytes[i + 1] = MeterSerialNumberBytes[counter];
                counter++;

                // if counter is 16 which is the length of key, and length of serial number
                // break
                if (counter == 16)
                {
                    break;
                }
            }

            // loop to perform XOR operation on zipped array in rage of 331
            foreach (var i in Enumerable.Range(0, 330))
            {
                var s = resultBytes[0] ^ resultBytes[1] ^ resultBytes[2] ^ resultBytes[3] ^ resultBytes[9]
                    ^ resultBytes[10] ^ resultBytes[13] ^ resultBytes[15] ^ resultBytes[16]
                    ^ resultBytes[17] ^ resultBytes[18] ^ resultBytes[25] ^ resultBytes[27] ^ resultBytes[28] ^ resultBytes[29] ^ resultBytes[30];

                // Bitwise AND op
                s = (s + 77) & 0xFF;

                // Perform left and right bitwise shifts
                s = (s << 4) + (s >> 4);

                // Temporary array to hold the results so far except the softswitch element
                var temp = resultBytes.Skip(1).ToArray();

                var temp2 = new byte[1] { (byte)(s & 0xFF) };

                // Concat both temporary arrays
                resultBytes = temp.Concat(temp2).ToArray();
            }

            // Return the encrypted 4 bytes
            return resultBytes.TakeLast(4).ToArray();
        }
    }
}