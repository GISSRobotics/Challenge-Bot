using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Gaming.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GISSBotChallenge2016
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        Gamepad controller;
        DispatcherTimer dispatcherTimer;

        private MediaCapture[] _mediaCapture;
        private CaptureElement[] _camPreviewControl;
        private int _camCount;

        bool functionRunning = false;
        string functionRunningName = "";
        int[] buttonTimes = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        bool inRange = true;
        bool targetSet = false;
        int viewButtonLong = 30;
        bool manualMotorDrive = false;
        int loopsUntilMotorSend = 0;
        int loopsUntilMotorSendReset = 3;
        double[] motorSpeeds = new double[] { 0, 0 };

        ArduinoAmbassador arduino = new ArduinoAmbassador();

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;

            if (Gamepad.Gamepads.Count > 0)
            {
                // Vibrate the controller
                Task.Delay(2500).ContinueWith(_ => { Gamepad.Gamepads.First().Vibration = new GamepadVibration { LeftMotor = 0.5, RightMotor = 0.5, LeftTrigger = 0.5, RightTrigger = 0.5 }; });
                Task.Delay(2750).ContinueWith(_ => { Gamepad.Gamepads.First().Vibration = new GamepadVibration { LeftMotor = 0, RightMotor = 0, LeftTrigger = 0, RightTrigger = 0 }; });
            }

            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;

            // Start a loop checking the controller
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_TickAsync;
            dispatcherTimer.Interval = new TimeSpan(100);

            // Task.Run(async () => { await StartCameraAsync(); });
            StartCameraAsync();

            dispatcherTimer.Start();

        }

        private async Task StartCameraAsync()
        {
            try
            {
                _camPreviewControl = new CaptureElement[] { CamPreviewControlL, CamPreviewControlR };
                DeviceInformationCollection vidDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                _camCount = vidDevices.Count();
                _camCount = _camCount > 2 ? 2 : _camCount;
                _mediaCapture = new MediaCapture[_camCount];
                for (int i = 0; i < _camCount; i++)
                {
                    _mediaCapture[i] = new MediaCapture();
                    await _mediaCapture[i].InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = vidDevices[i].Id });
                    _camPreviewControl[i].Source = _mediaCapture[i];
                    await _mediaCapture[i].StartPreviewAsync();
                }
            }
            catch (Exception e)
            {
                OutputBuffer.Text += e.StackTrace;
            }
        }

        private async Task StopCameraAsync()
        {

        }

        private async void OnLoaded(object sender, object e)
        {
            // Any async stuff to do on loading
        }

        private void Display(string text)
        {
            /*statusText.Text = text;*/
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            // Stuff to do once controller is added
            Display("Gamepad Added");
            // Vibrate controller
            Task.Delay(250).ContinueWith(_ => { e.Vibration = new GamepadVibration { LeftMotor = 0.5, RightMotor = 0.5, LeftTrigger = 0.5, RightTrigger = 0.5 }; });
            Task.Delay(500).ContinueWith(_ => { e.Vibration = new GamepadVibration { LeftMotor = 0, RightMotor = 0, LeftTrigger = 0, RightTrigger = 0 }; });
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            // Stuff to do when the last controller is removed
            if (Gamepad.Gamepads.Count == 0)
            {
                // STOP and set motor speeds to 0
                Display("Gamepad Removed");
                sendCommand("STOP");
                motorSpeeds = new double[] { 0, 0 };
                sendCommand("SETMOTORS 0,0");
                // Not running any functions anymore
                functionRunning = false;
                functionRunningName = "";
            }
        }

        private async void DispatcherTimer_TickAsync(object sender, object e)
        {
            string arduinoBuffer = arduino.ReadBuffer();
            ArduinoDisplay.Text = arduinoBuffer;

            if (Gamepad.Gamepads.Count > 0)
            {
                controller = Gamepad.Gamepads.First();                                              // Get the first controller
                GamepadReading reading = controller.GetCurrentReading();                            // Get the controller's reading
                GamepadButtons buttons = reading.Buttons;                                           // Get the button enum from this reading
                // Update button states on-screen
                LeftThumbstickState.Text = "Left Stick: " + Math.Round(reading.LeftThumbstickX, 1) + ", " + Math.Round(reading.LeftThumbstickY, 1);
                RightThumbstickState.Text = "Right Stick: " + Math.Round(reading.RightThumbstickX, 1) + ", " + Math.Round(reading.RightThumbstickY, 1);
                LeftTriggerState.Text = "Left Trigger: " + Math.Round(reading.LeftTrigger, 1);
                RightTriggerState.Text = "Right Trigger: " + Math.Round(reading.RightTrigger, 1);
                LeftBumperState.Text = "Left Bumper: " + buttons.HasFlag(GamepadButtons.LeftShoulder);
                RightBumperState.Text = "Right Bumper: " + buttons.HasFlag(GamepadButtons.RightShoulder);
                MenuState.Text = "Menu: " + buttons.HasFlag(GamepadButtons.Menu).ToString();
                ViewState.Text = "View: " + buttons.HasFlag(GamepadButtons.View).ToString();
                LeftThumbButtonState.Text = "Left Stick Button: " + buttons.HasFlag(GamepadButtons.LeftThumbstick).ToString();
                RightThumbButtonState.Text = "Right Stick Button: " + buttons.HasFlag(GamepadButtons.RightThumbstick).ToString();
                DPadUpState.Text = "D-Pad Up: " + buttons.HasFlag(GamepadButtons.DPadUp).ToString();
                DPadDownState.Text = "D-Pad Down: " + buttons.HasFlag(GamepadButtons.DPadDown).ToString();
                DPadLeftState.Text = "D-Pad Left: " + buttons.HasFlag(GamepadButtons.DPadLeft).ToString();
                DPadRightState.Text = "D-Pad Right: " + buttons.HasFlag(GamepadButtons.DPadRight).ToString();
                AState.Text = "A: " + buttons.HasFlag(GamepadButtons.A).ToString();
                BState.Text = "B: " + buttons.HasFlag(GamepadButtons.B).ToString();
                XState.Text = "X: " + buttons.HasFlag(GamepadButtons.X).ToString();
                YState.Text = "Y: " + buttons.HasFlag(GamepadButtons.Y).ToString();
                
                // Controller Mapping
                // ==================
                // Left Stick       : Steering (X) [Acceleration (Y)?]
                // Left Trigger     : Acceleration
                // Right Trigger    : Deceleration
                // D-Up             : Follow Line
                // D-Down           : Goto Range
                // D-Left           : Complete Auto
                // D-Right          : Auto Aim and Fire 5x
                // Right Bumper     : Fire
                // A                : Auto Aim
                // B                : Stop Automation
                // X                : Record Start/Stop
                // Y                : Record Go
                // View             : Goto Start / Set Start
                
                // Button Handlers - Can this be made better?
                // Maybe make event delegates and handlers?
                if (buttons.HasFlag(GamepadButtons.DPadUp))
                {
                    buttonTimes[0] += 1;
                }
                else if (buttonTimes[0] > 0)
                {
                    if (!functionRunning)
                    {
                        functionRunning = true;
                        functionRunningName = "FOLLOW";
                        sendCommand("FOLLOW");
                    }
                    buttonTimes[0] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.DPadDown))
                {
                    buttonTimes[1] += 1;
                }
                else if (buttonTimes[1] > 0)
                {
                    if (!functionRunning)
                    {
                        functionRunning = true;
                        functionRunningName = "GOTO RANGE";
                        sendCommand("GOTO RANGE");
                    }
                    buttonTimes[1] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.DPadLeft))
                {
                    buttonTimes[2] += 1;
                }
                else if (buttonTimes[2] > 0)
                {
                    if (!functionRunning)
                    {
                        functionRunning = true;
                        functionRunningName = "COMPLETE";
                        sendCommand("[CompleteSeriesOfCommandsAndAutomation]");
                    }
                    buttonTimes[2] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.DPadRight))
                {
                    buttonTimes[3] += 1;
                }
                else if (buttonTimes[3] > 0)
                {
                    if (!functionRunning && inRange)
                    {
                        functionRunning = true;
                        functionRunningName = "AIM THEN FIRE";
                        sendCommand("[AutoFire5SeriesOfCommandsAndAutomation]");
                    }
                    buttonTimes[3] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.RightShoulder))
                {
                    buttonTimes[4] += 1;
                }
                else if (buttonTimes[4] > 0)
                {
                    if (!functionRunning && inRange)
                    {
                        // Fire
                        sendCommand("FIRE");
                    }
                    buttonTimes[4] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.A))
                {
                    buttonTimes[5] += 1;
                }
                else if (buttonTimes[5] > 0)
                {
                    if (!functionRunning && inRange)
                    {
                        functionRunning = true;
                        functionRunningName = "AIM";
                        sendCommand("[AutoAimSeriesOfCommandsAndAutomation]");
                    }
                    buttonTimes[5] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.B))
                {
                    buttonTimes[6] += 1;
                }
                else if (buttonTimes[6] > 0)
                {
                    if (functionRunning)
                    {
                        // Stop
                        functionRunning = false;
                        functionRunningName = "";
                        sendCommand("STOP");
                    }
                    buttonTimes[6] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.X))
                {
                    buttonTimes[7] += 1;
                }
                else if (buttonTimes[7] > 0)
                {
                    if (!functionRunning)
                    {
                        functionRunning = true;
                        functionRunningName = "RECORD";
                        sendCommand("RECORD START");
                    }
                    else if (functionRunningName == "RECORD")
                    {
                        functionRunning = false;
                        functionRunningName = "";
                        sendCommand("RECORD STOP");
                    }
                    buttonTimes[7] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.Y))
                {
                    buttonTimes[8] += 1;
                }
                else if (buttonTimes[8] > 0)
                {
                    if (!functionRunning)
                    {
                        functionRunning = true;
                        functionRunningName = "RECORD GO";
                        sendCommand("RECORD GO");
                    }
                    buttonTimes[8] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.View))
                {
                    buttonTimes[9] += 1;
                    if (!functionRunning && buttonTimes[9] == viewButtonLong)
                    {
                        // Set START
                        sendCommand("SETSTART");
                    }
                }
                else if (buttonTimes[9] > 0)
                {
                    if (!functionRunning && buttonTimes[9] < viewButtonLong)
                    {
                        functionRunning = true;
                        functionRunningName = "GOTO START";
                        sendCommand("GOTO START");
                    }
                    buttonTimes[9] = 0;
                }

                if (functionRunning && functionRunningName.StartsWith("AIM") && !targetSet)
                {
                    // Aiming right now, so camera is needed

                    // Get Frames
                    SoftwareBitmap[] camFrames = await captureCamFrame();
                    // Find target
                    // [ Is target within parameters? ]
                    // [ If not, Define neccesary movement for t milliseconds ]
                    // [ If so, targetSet = true ]

                    // if function is AIM THEN FIRE:
                    //  check arduino ammo, if any left then fire/
                    //  if none left, end funtion
                    //  if fired, continue function and loop again to ensure aim is still good
                }
                else
                {

                    motorSpeeds = new double[] { 0, 0 };

                    if (manualMotorDrive)
                    {
                        motorSpeeds = new double[] { reading.LeftThumbstickY, reading.RightThumbstickY };
                    }
                    else
                    {
                        // Use either the left stick Y or the triggers for acceleration value:
                        double acceleration = reading.LeftTrigger - reading.RightTrigger;           // Use triggers
                        //double acceleration = reading.LeftThumbstickY;                              // Use left stick Y
                        motorSpeeds = computeMotorSpeeds(reading.LeftThumbstickX, acceleration);
                    }

                    if (loopsUntilMotorSend <= 0)
                    {
                        sendCommand("SETMOTORS " + Math.Round(motorSpeeds[0], 1).ToString() + "," + Math.Round(motorSpeeds[1], 1).ToString());
                        loopsUntilMotorSend = loopsUntilMotorSendReset;
                    }
                    else
                    {
                        loopsUntilMotorSend--;
                    }
                }

                FunctionDisplay.Text = functionRunningName;
            }
        }

        private double[] computeMotorSpeeds(double steering, double acceleration)
        {

            // Improve the steering algorithm!

            // Need to combine pivoting with moving & steering

            double sensitivity = 1;
            steering = steering * sensitivity;
            double m1 = 0;
            double m2 = 0;
            if (acceleration >= 0)
            {
                m1 = acceleration + steering;
                m2 = acceleration - steering;
            }
            else
            {
                m1 = acceleration - steering;
                m2 = acceleration + steering;
            }
            return new double[] { m1 > 1 ? 1 : m1 < -1 ? -1 : m1, m2 > 1 ? 1 : m2 < -1 ? -1 : m2 };
        }

        private async void sendCommand(string command)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (command.StartsWith("SETMOTORS"))
                {
                    MotorOutputBuffer.Text = command + "\n" + MotorOutputBuffer.Text;
                }
                else
                {
                    OutputBuffer.Text = command + "\n" + OutputBuffer.Text;
                }

                arduino.WriteCommand(command);

            });
        }

        private void printArduino(string data)
        {

        }

        private async Task<SoftwareBitmap[]> captureCamFrame()
        {
            var lowLagCaptureL = await _mediaCapture[0].PrepareAdvancedPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
            var lowLagCaptureR = await _mediaCapture[1].PrepareAdvancedPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
            var capturedPhotoL = await lowLagCaptureL.CaptureAsync();
            var capturedPhotoR = await lowLagCaptureR.CaptureAsync();
            SoftwareBitmap swBmpL = capturedPhotoL.Frame.SoftwareBitmap;
            SoftwareBitmap swBmpR = capturedPhotoR.Frame.SoftwareBitmap;
            await lowLagCaptureL.FinishAsync();
            await lowLagCaptureR.FinishAsync();
            return new SoftwareBitmap[] { swBmpL, swBmpR };
        }
    }

    public class ArduinoAmbassador
    {

        // Class for communicating with Arudino
        // Can also be used for simulation

        public string buffer = "";
        public bool isOK = true;

        public ArduinoAmbassador()
        {
        }

        public string ReadBuffer()
        {
            return "OK 0,0 0,0 0,0 0 0000 5 N";
        }

        public async void WriteCommand(string command)
        {
            string write = command + "\n";
            await Task.Delay(100);
        }

    }
}
