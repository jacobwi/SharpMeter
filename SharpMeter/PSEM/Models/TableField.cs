namespace SharpMeter.PSEM.Models
{
    /// <summary>
    /// The table field.
    /// </summary>
    ///

    public class TableField
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether read is only.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        ///

        public dynamic Value { get; set; }

        /// <summary>
        /// Gets or sets the hex string.
        /// </summary>
        public string HexString { get; set; }

        /// <summary>
        /// Gets or sets the bytes.
        /// </summary>
        public byte[] Bytes { get; set; }
    }
}