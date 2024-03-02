using OpenCV.Net;

namespace Bonsai.Miniscope
{
    public class MiniCamFrame
    {
        public MiniCamFrame(IplImage image, int frameNumber, bool trigger)
        {
            FrameNumber = frameNumber;
            Image = image;
            Trigger = trigger;
        }

        public int FrameNumber { get; }
        public IplImage Image { get; }
        public bool Trigger { get; }

    }
}
