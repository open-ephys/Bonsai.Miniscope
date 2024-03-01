using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Bonsai.Miniscope
{
    public enum GainV4
    {
        LOW = 225,
        MED = 228,
        HIGH = 36
    };

    public enum FPSV4
    {
        FPS10 = 39 & 0x000000FF | 16 << 8,
        FPS15 = 26 & 0x000000FF | 11 << 8,
        FPS20 = 19 & 0x000000FF | 136 << 8,
        FPS25 = 15 & 0x000000FF | 160 << 8,
        FPS30 = 12 & 0x000000FF | 228 << 8,
    };

    [Description("Produces a combined image/IMU sequence acquired from a UCLA Miniscope V4.")]
    public class UCLAMiniscopeV4 : Source<V4Frame>
    {
        // Frame size
        const int WIDTH = 608;
        const int HEIGHT = 608;

        // Settings
        [Description("The index of the camera from which to acquire images.")]
        public int Index { get; set; } = 0;

        [Range(0, 255)]
        [Editor(DesignTypes.SliderEditor, typeof(UITypeEditor))]
        [Description("LED brightness.")]
        public double LEDBrightness { get; set; } = 0;

        [Range(-127, 127)]
        [Editor(DesignTypes.SliderEditor, typeof(UITypeEditor))]
        [Description("Electro-wetting lens focus.")]
        public double EWL { get; set; } = 0;

        [Description("The image sensor gain.")]
        public GainV4 SensorGain { get; set; } = GainV4.LOW;

        [Description("Frames per second.")]
        public FPSV4 FramesPerSecond { get; set; } = FPSV4.FPS30;

        [Description("Turn off the LED when the trigger input is low. " +
            "Note that this pin is low by default. Therefore, if it is not driven and " +
            "this option is set to true, the LED will not turn on.")]
        public bool LEDRespectsTrigger { get; set; } = false;

        // State
        IObservable<V4Frame> source;
        readonly object captureLock = new object();


        // Camera regiser abuse
        //
        // CaptureProperty.Saturation -> Quaternion W and start acqusition
        // CaptureProperty.Hue -> Quaternion X
        // CaptureProperty.Gain -> Quaternion Y
        // CaptureProperty.Brightness -> Quaternion Z
        // CaptureProperty.Gamma -> Inverted state of Trigger Input (3.3 -> Gamma = 0, 0V -> Gamma != 0)
        // CaptureProperty.Contrast -> DAQ Frame number

        public UCLAMiniscopeV4()
        {
            source = Observable.Create<V4Frame>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        bool initialized = false;
                        var lastLEDBrightness = LEDBrightness;
                        var lastEWL = EWL;
                        var lastFPS = FramesPerSecond;
                        var lastSensorGain = SensorGain;

                        using (var capture = Capture.CreateCameraCapture(Index))
                        {
                            try
                            {
                                // Magik configuration sequence (configures SERDES and chip default states)
                                // 8-bit            7-bit           Description
                                // ---------------------------------------------
                                // 192 (0xc0)       96 (0x60)       Deserializer
                                // 176 (0xb0)       88 (0x58)       Serializer
                                // 160 (0xa0)       80 (0x50)       TPL0102 Digital potentiometer
                                // 80 (0x50)        40 (0x28)       BNO055
                                // 254 (0xfe)       127 (0x7F)      ??
                                // 238 (0xee)       119 (0x77)      MAX14574 EWL driver
                                // 32 (0x20)        16 (0x10)       ATTINY MCU

                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 31, 16)); // I2C: 0x60
                                Helpers.SendConfig(capture, Helpers.CreateCommand(176, 5, 32)); // I2C:0x58
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 34, 2));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 32, 10));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 7, 176));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(176, 15, 2));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(176, 30, 10));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 8, 32, 238, 160, 80));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(192, 16, 32, 238, 88, 80));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(80, 65, 9, 5)); // BNO Axis mapping and sign
                                Helpers.SendConfig(capture, Helpers.CreateCommand(80, 61, 12)); // BNO operation mode is NDOF
                                Helpers.SendConfig(capture, Helpers.CreateCommand(254, 0)); // 0x7F
                                Helpers.SendConfig(capture, Helpers.CreateCommand(238, 3, 3)); // 0x77

                                // Set frame size
                                capture.SetProperty(CaptureProperty.FrameWidth, WIDTH);
                                capture.SetProperty(CaptureProperty.FrameHeight, HEIGHT);

                                // Start the camera
                                capture.SetProperty(CaptureProperty.Saturation, 1);

                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    // Get trigger input state
                                    var gate = capture.GetProperty(CaptureProperty.Gamma) != 0;

                                    if (LEDRespectsTrigger)
                                    {
                                        if (!gate && lastLEDBrightness != 0)
                                        {
                                            Helpers.SendConfig(capture, Helpers.CreateCommand(32, 1, 255));
                                            Helpers.SendConfig(capture, Helpers.CreateCommand(88, 0, 114, 255));
                                            lastLEDBrightness = 0;
                                        }
                                        else if (gate && LEDBrightness != lastLEDBrightness || !initialized)
                                        {
                                            Helpers.SendConfig(capture, Helpers.CreateCommand(32, 1, (byte)(255 - LEDBrightness)));
                                            Helpers.SendConfig(capture, Helpers.CreateCommand(88, 0, 114, (byte)(255 - LEDBrightness)));
                                            lastLEDBrightness = LEDBrightness;
                                        }
                                    }
                                    else
                                    {
                                        if (LEDBrightness != lastLEDBrightness || !initialized)
                                        {
                                            Helpers.SendConfig(capture, Helpers.CreateCommand(32, 1, (byte)(255 - LEDBrightness)));
                                            Helpers.SendConfig(capture, Helpers.CreateCommand(88, 0, 114, (byte)(255 - LEDBrightness)));
                                            lastLEDBrightness = LEDBrightness;
                                        }
                                    }

                                    if (EWL != lastEWL || !initialized)
                                    {
                                        Helpers.SendConfig(capture, Helpers.CreateCommand(238, 8, (byte)(127 + EWL), 2));
                                        lastEWL = EWL;
                                    }

                                    if (FramesPerSecond != lastFPS || !initialized)
                                    {
                                        byte v0 = (byte)((int)FramesPerSecond & 0x00000FF);
                                        byte v1 = (byte)(((int)FramesPerSecond & 0x000FF00) >> 8);
                                        Helpers.SendConfig(capture, Helpers.CreateCommand(32, 5, 0, 201, v0, v1));
                                        lastFPS = FramesPerSecond;
                                    }

                                    if (SensorGain != lastSensorGain || !initialized)
                                    {
                                        Helpers.SendConfig(capture, Helpers.CreateCommand(32, 5, 0, 204, 0, (byte)SensorGain));
                                        lastSensorGain = SensorGain;
                                    }

                                    initialized = true;

                                    // Capture frame
                                    var image = capture.QueryFrame();

                                    // Get latest hardware frame count
                                    var frameNumber = (int)capture.GetProperty(CaptureProperty.Contrast);

                                    // Get BNO data
                                    var w = (ushort)capture.GetProperty(CaptureProperty.Saturation);
                                    var x = (ushort)capture.GetProperty(CaptureProperty.Hue);
                                    var y = (ushort)capture.GetProperty(CaptureProperty.Gain);
                                    var z = (ushort)capture.GetProperty(CaptureProperty.Brightness);

                                    if (image == null)
                                    {
                                        observer.OnCompleted();
                                        break;
                                    }
                                    else
                                    {
                                        observer.OnNext(new V4Frame(image.Clone(), new ushort[] { w, x, y, z }, frameNumber, gate));
                                    }
                                }
                            }
                            finally
                            {
                                Helpers.SendConfig(capture, Helpers.CreateCommand(32, 1, 255));
                                Helpers.SendConfig(capture, Helpers.CreateCommand(88, 0, 114, 255));
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

        public override IObservable<V4Frame> Generate()
        {
            return source;
        }
    }
}
