using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using System.Xml.Serialization;

namespace SharpMeter.PSEM.Models
{
    /// <summary>
    /// The meter.
    /// </summary>
    ///
    [Serializable]
    [JsonObject]
    public class Meter
    {
        /// <summary>
        /// Gets or sets the serial number.
        /// </summary>
        ///
        [XmlAttribute("SerialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the firmware version.
        /// </summary>
        ///
        [XmlArray("Firmware")]
        public int[] Firmware { get; set; }

        /// <summary>
        /// Gets or sets the access.
        /// </summary>
        ///
        [XmlAttribute("Access")]
        public Constants.AccessLevel Access { get; set; } = Constants.AccessLevel.NoAccess;

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        ///
        [XmlAttribute]
        public Constants.Model Model { get; set; } = Constants.Model.GENERIC;

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        ///
        [XmlAttribute]
        public Constants.Mode Mode { get; set; }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        ///
        [XmlAttribute]
        public Constants.Target Target { get; set; }

        /// <summary>
        /// Gets or sets the a m i device.
        /// </summary>
        ///
        [XmlAttribute]
        public Constants.AMIDevice AMIDevice { get; set; }

        [XmlAttribute]
        public Constants.Configuration Configuration { get; set; }

        [XmlArrayItem(Type = typeof(Table))]

        /// <summary>
        /// Gets or sets the tables.
        /// </summary>
        ///
        [XmlArrayItem(Type = typeof(Table))]
        public List<Table> StandardTables { get; set; }

        /// <summary>
        /// Gets or sets the tables.
        /// </summary>
        ///
        public List<string> AvailableStandardTables { get; set; }

        [XmlArrayItem(Type = typeof(Table))]
        public List<Table> ManufacturingTables { get; set; }

        public List<string> AvailableManufacturingTables { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is calibration.
        /// </summary>
        ///
        [XmlAttribute]
        public bool IsCalibration { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether is relay.
        /// </summary>
        ///
        [XmlAttribute]
        public bool IsRelay { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether mag is immune.
        /// </summary>
        ///
        [XmlAttribute]
        public bool IsMagImmune { get; set; } = false;
    }
}