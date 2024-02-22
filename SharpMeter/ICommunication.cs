namespace SharpMeter
{
    /// <summary>
    /// Communication interface.
    /// </summary>
    public interface ICommunication
    {
        /// <summary>
        /// Opens connection.
        /// </summary>
        /// <returns>A string.</returns>
        string Open();

        /// <summary>
        /// Closes connection.
        /// </summary>
        /// <returns>A string.</returns>
        string Close();

        /// <summary>
        /// Recieves a packet.
        /// </summary>
        /// <param name="receivedData">The received data.</param>
        /// <param name="isMultiPacket">If true, is multi packet.</param>
        /// <param name="firstPacketData">The first packet data.</param>
        /// <returns>A string.</returns>
        string RecievePacket(out byte[] receivedData, bool isMultiPacket = false, byte[] firstPacketData = null);

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="packetToSend">The packet to send.</param>
        /// <returns>A string.</returns>
        string SendPacket(byte[] packetToSend);
    }
}