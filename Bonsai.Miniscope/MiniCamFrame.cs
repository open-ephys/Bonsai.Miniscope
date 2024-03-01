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

        public int FrameNumber { get; private set; }
        public IplImage Image { get; private set; }
        public bool Trigger { get; private set; }

    }
}
