using System;
using OpenCV.Net;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Drawing.Design;

namespace Bonsai.Miniscope
{
    [Description("Produces a video sequence acquired from a UCLA Miniscope V3 (legacy DAQ firmware).")]
    public class UCLAMiniscope : Source<IplImage>
    {
        // Settings
        [Description("The index of the camera from which to acquire images.")]
        public int Index { get; set; } = 0;

        [Description("Indicates whether to activate the hardware frame pulse output.")]
        public bool RecordingFramePulse { get; set; } = false;

        [Range(0, 255)]
        [Editor(DesignTypes.SliderEditor, typeof(UITypeEditor))]
        [Description("LED Brightness.")]
        public double LEDBrightness { get; set; } = 0;

        [Range(1, 255)]
        [Editor(DesignTypes.SliderEditor, typeof(UITypeEditor))]
        [Description("Relative exposure time.")]
        public double Exposure { get; set; } = 255;

        [Range(16, 64)]
        [Precision(0, 2)]
        [Editor(DesignTypes.SliderEditor, typeof(UITypeEditor))]
        [Description("The sensor gain.")]
        public double SensorGain { get; set; } = 16;

        const int RecordStart = 0x01;
        const int RecordEnd = 0x02;

        public enum FPS
        {
            FPS5 = 0x11,
            FPS10 = 0x12,
            FPS15 = 0x13,
            FPS20 = 0x14,
            FPS30 = 0x15,
            FPS60 = 0x16
        };

        [Description("Frames per second.")]
        public FPS FramesPerSecond { get; set; } = FPS.FPS30;

        // State
        IObservable<IplImage> source;
        readonly object captureLock = new object();

        // Functor
        public UCLAMiniscope()
        {
            source = Observable.Create<IplImage>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        var lastRecordingFramePulse = false;
                        var lastLEDBrightness = LEDBrightness;
                        var lastExposure = Exposure;
                        var lastSensorGain = SensorGain;
                        using (var capture = Capture.CreateCameraCapture(Index))
                        {
                            try
                            {
                                capture.SetProperty(CaptureProperty.Saturation, (double)FramesPerSecond);
                                capture.SetProperty(CaptureProperty.Hue, LEDBrightness);
                                capture.SetProperty(CaptureProperty.Gain, SensorGain);
                                capture.SetProperty(CaptureProperty.Brightness, Exposure);
                                capture.SetProperty(CaptureProperty.Saturation, RecordEnd);
                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    // Runtime settable properties
                                    if (LEDBrightness != lastLEDBrightness)
                                    {
                                        capture.SetProperty(CaptureProperty.Hue, LEDBrightness);
                                        lastLEDBrightness = LEDBrightness;
                                    }
                                    if (SensorGain != lastSensorGain)
                                    {
                                        capture.SetProperty(CaptureProperty.Gain, SensorGain);
                                        lastSensorGain = SensorGain;
                                    }
                                    if (Exposure != lastExposure)
                                    {
                                        capture.SetProperty(CaptureProperty.Brightness, Exposure);
                                        lastExposure = Exposure;
                                    }
                                    if (RecordingFramePulse != lastRecordingFramePulse)
                                    {
                                        capture.SetProperty(CaptureProperty.Saturation, RecordingFramePulse ? RecordStart : RecordEnd);
                                        lastRecordingFramePulse = RecordingFramePulse;
                                    }

                                    var image = capture.QueryFrame();

                                    if (image == null)
                                    {
                                        observer.OnCompleted();
                                        break;
                                    }
                                    else observer.OnNext(image.Clone());
                                }
                            }
                            finally
                            {
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
