using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SharpMeter.PSEM.Models
{
    /// <summary>
    /// Meter table class.
    /// </summary>
    ///
    [Serializable]
    public class Table
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public int Size { get; set; }

        public string HexData { get; set; }
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        ///
        [XmlArray("Fields")]
        public List<TableField> Fields { get; set; }
    }
}