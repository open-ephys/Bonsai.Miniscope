using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Bonsai.Miniscope
{
    public enum GainV3
    {
        LOW = 225,
        MED = 228,
        HIGH = 36
    };

    public enum FPSV3
    {
        FPS10,
        FPS30,
        FPS60
    };

    [Description("Produces a video sequence acquired from a UCLA Miniscope V3 (updated DAQ firmware).")]
    public class UCLAMiniscopeV3 : Source<IplImage>
    {
        // Frame size
        const int WIDTH = 752;
        const int HEIGHT = 480;

        // Settings
        [Description("The index of the camera from which to acquire images.")]
        public int Index { get; set; } = 0;

        [Range(0, 0xFF0)]
        [Editor(DesignTypes.SliderEditor, typeof(UITypeEditor))]
        [Description("LED brightness.")]
        public double LEDBrightness { get; set; } = 0;

        [Description("The image sensor gain.")]
        public GainV3 SensorGain { get; set; } = GainV3.LOW;

        [Description("Frames per second.")]
        public FPSV3 FramesPerSecond { get; set; } = FPSV3.FPS30;

        // State
        IObservable<IplImage> source;
        readonly object captureLock = new object();

        public UCLAMiniscopeV3()
        {
            source = Observable.Create<IplImage>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        bool initialized = false;
                        var lastLEDBrightness = LEDBrightness;
                        var lastFPS = FramesPerSecond;
                        var lastSensorGain = SensorGain;

                        using (var capture = Capture.CreateCameraCapture(Index))
                        {
                            try
                            {
                                // Magik configuration sequence (configures SERDES and chip default states)
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 31, 16));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(176, 5, 32));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 34, 2));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 32, 10));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 7, 176));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(176, 15, 2));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(176, 30, 10));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 8, 184, 152));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 16, 184, 152));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 12, 0, 1));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 175, 0, 0));

                                // Set frame size
                                capture.SetProperty(CaptureProperty.FrameWidth, WIDTH);
                                capture.SetProperty(CaptureProperty.FrameHeight, HEIGHT);

                                // Start the camera
                                capture.SetProperty(CaptureProperty.Saturation, 1);

                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    // Runtime settable properties
                                    if (LEDBrightness != lastLEDBrightness || !initialized)
                                    {
                                        Helpers.SendConfig(capture, Helpers.CreateCommand(152, (byte)((int)LEDBrightness >> 8), (byte)((int)LEDBrightness & 0xFF)));
                                        lastLEDBrightness = LEDBrightness;
                                    }
                                    if (FramesPerSecond != lastFPS || !initialized)
                                    {
                                        switch (FramesPerSecond)
                                        {
                                            case FPSV3.FPS10:
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 5, 2, 238, 4, 226));
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 11, 6, 184));
                                                break;
                                            case FPSV3.FPS30:
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 5, 0, 94, 2, 33));
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 11, 3, 232));
                                                break;
                                            case FPSV3.FPS60:
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 5, 0, 93, 0, 33));
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 11, 1, 244));
                                                break;
                                        }
                                        lastFPS = FramesPerSecond;
                                    }
                                    if (SensorGain != lastSensorGain || !initialized)
                                    {
                                        switch (SensorGain)
                                        {
                                            case GainV3.LOW:
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 53, 0, 16));
                                                break;
                                            case GainV3.MED:
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 53, 0, 32));
                                                break;
                                            case GainV3.HIGH:
                                                Helpers.SendConfig(capture, Helpers.CreateCommand(184, 53, 0, 64));
                                                break;
                                        }
                                        lastSensorGain = SensorGain;
                                    }

                                    initialized = true;

                                    // Capture frame
                                    var image = capture.QueryFrame();

                                    if (image == null)
                                    {
                                        observer.OnCompleted();
                                        break;
                                    }
                                    else
                                    {
                                        observer.OnNext(image.Clone());
                                    }
                                }
                            }
                            finally
                            {
                                Helpers.SendConfig(capture, Helpers.CreateCommand(152, 0, 0));
                                capture.SetProperty(CaptureProperty.Saturation, 0);
                                capture.Close();
                            }

                        }
                    }
                },
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            })
            .PublishReconnectable()
            .RefCount();
        }

        public override IObservable<IplImage> Generate()
        {
            return source;
        }
    }
}
