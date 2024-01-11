using OpenCV.Net;
using System.Linq;

namespace Bonsai.Miniscope
{
    public class V4Frame
    {
        public V4Frame(IplImage image, ushort[] quaternion, int frameNumber, bool trigger)
        {
            FrameNumber = frameNumber;
            Image = image;
            Quaternion = GetQuat(quaternion);
            Trigger = trigger;
        }

        public int FrameNumber { get; private set; }
        public IplImage Image { get; private set; }
        public Mat Quaternion { get; private set; }
        public bool Trigger { get; private set; }

        Mat GetQuat(ushort[] sample)
        {
            // 1 quaternion (unitless) = 2^14 LSB
            const double scale = (1.0 / (1 << 14));
            var vec = new double[4];

            for (int i = 0; i < vec.Count(); i++)
            {
                var tmp = (short)sample[i];
                vec[i] = scale * tmp;
            }

            return Mat.FromArray(vec, vec.Length, 1, Depth.F64, 1);
        }
    }
}
