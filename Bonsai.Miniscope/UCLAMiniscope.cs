using System;
using OpenCV.Net;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Drawing.Design;

namespace Bonsai.Miniscope
{
    [Description("Produces a video sequence of images acquired from a UCLA Miniscope.")]
    public class UCLAMiniscope : Source<IplImage>
    {
        // Settings
        [Description("The index of the camera from which to acquire images.")]
        public int Index { get; set; } = 0;

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
        //readonly CapturePropertyCollection captureProperties = new CapturePropertyCollection();

        // Functor
        public UCLAMiniscope()
        {
            source = Observable.Create<IplImage>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        using (var capture = Capture.CreateCameraCapture(Index))
                        {
                            //foreach (var setting in captureProperties)
                            //{
                             //   capture.SetProperty(setting.Property, setting.Value);
                            //}
                            //captureProperties.Capture = capture;

                            capture.SetProperty(CaptureProperty.Saturation, (double)FramesPerSecond);
                            try
                            {
                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    // Runtime settable properties
                                    capture.SetProperty(CaptureProperty.Hue, LEDBrightness);
                                    capture.SetProperty(CaptureProperty.Gain, SensorGain);
                                    capture.SetProperty(CaptureProperty.Brightness, Exposure);

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
                                //captureProperties.Capture = null;
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

        //[Description("Specifies the set of capture properties assigned to the camera.")]
       // public CapturePropertyCollection CaptureProperties
        //{
        //    get { return captureProperties; }
        //}

        public override IObservable<IplImage> Generate()
        {
            return source;
        }
    }
}