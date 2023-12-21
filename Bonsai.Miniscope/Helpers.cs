using OpenCV.Net;
using System;

namespace Bonsai.Miniscope
{
    class Helpers
    {
        // V4-capable firmware configuration protocol functions

        // They are using a simple protocol for universal device configuration
        // Settings are sent using 64 bit words over 3 fixed configuration registers
        static internal void SendConfig(Capture capture, ulong command)
        {
            capture.SetProperty(CaptureProperty.Contrast, command & 0x00000000FFFF);
            capture.SetProperty(CaptureProperty.Gamma, (command & 0x0000FFFF0000) >> 16);
            capture.SetProperty(CaptureProperty.Sharpness, (command & 0xFFFF00000000) >> 32);
        }

        /// <summary>
        /// Create a command to be decoded by the Miniscope firmware.
        /// </summary>
        /// <param name="address">I2C address in 8 bit format. LSB is always 0.</param>
        /// <param name="values">Bytes to be decoded by miniscope.</param>
        /// <returns></returns>
        static internal ulong CreateCommand(byte address, params byte[] values)
        {
            if (values.Length > 5)
                throw new ArgumentException(string.Format("{0} has more than 5 elements", values), nameof(values));

            ulong packet = address;

            if (values.Length == 5)
            {
                packet |= 0x01; // address with bottom bit flipped to 1 to indicate a full 6 byte package
                for (int i = 0; i < values.Length; i++)
                    packet |= (ulong)values[i] << 8 * (1 + i);
            }
            else
            {
                packet |= (ulong)(values.Length + 1) << 8; // address and number of data bytes
                for (int i = 0; i < values.Length; i++)
                    packet |= (ulong)values[i] << 8 * (2 + i);
            }
            return packet;
        }
    }
}
