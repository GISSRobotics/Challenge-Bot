using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.Storage.Streams;
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

        private SerialHelper _serialHelper;
        ArduinoAmbassador arduino;

        public MainPage()
        {
            this.InitializeComponent();

            if (Gamepad.Gamepads.Count > 0)
            {
                // Vibrate the controller
                Task.Delay(2500).ContinueWith(_ => { Gamepad.Gamepads.First().Vibration = new GamepadVibration { LeftMotor = 0.5, RightMotor = 0.5, LeftTrigger = 0.5, RightTrigger = 0.5 }; });
                Task.Delay(2750).ContinueWith(_ => { Gamepad.Gamepads.First().Vibration = new GamepadVibration { LeftMotor = 0, RightMotor = 0, LeftTrigger = 0, RightTrigger = 0 }; });
            }

            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;

            Application.Current.Resuming += Application_ResumingAsync;
            Application.Current.Suspending += Application_SuspendingAsync;

            Application_ResumingAsync(null, null);

            // Start a loop checking the controller
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_TickAsync;
            dispatcherTimer.Interval = new TimeSpan(100);

            dispatcherTimer.Start();

        }

        private async void Application_ResumingAsync(object sender, object args)
        {
            _serialHelper = new SerialHelper();
            var deviceList = await _serialHelper.GetSerialDevicesAsync();
            if (deviceList.Count() > 0)
            {
                await _serialHelper.InitializeAsync(deviceList.First());
                arduino = new ArduinoAmbassador(_serialHelper);
            } else
            {
                arduino = new ArduinoAmbassador(null);
            }
            await StartCameraAsync();
        }

        private async void Application_SuspendingAsync(object sender, object args)
        {
            await StopCameraAsync();
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
            for (int i = 0; i < _camCount; i++)
            {
                await _mediaCapture[i].StopPreviewAsync();
                _camPreviewControl[i].Source = null;
                _mediaCapture[i].Dispose();
            }
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
                SendCommand_Async("STOP");
                motorSpeeds = new double[] { 0, 0 };
                SendCommand_Async("SETMOTORS 0,0");
                // Not running any functions anymore
                functionRunning = false;
                functionRunningName = "";
            }
        }

        private async void DispatcherTimer_TickAsync(object sender, object e)
        {
            if (arduino != null)
            {
                string arduinoStatus = arduino.ReadBufferAndGetStatus();
                ArduinoDisplay.Text = arduinoStatus;
                if (arduino.IsSimulator)
                {
                    ArduinoDisplay.Text += "\n(Simulator)";
                }
            }

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
                        SendCommand_Async("FOLLOW");
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
                        SendCommand_Async("GOTO RANGE");
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
                        SendCommand_Async("[CompleteSeriesOfCommandsAndAutomation]");
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
                        SendCommand_Async("[AutoFire5SeriesOfCommandsAndAutomation]");
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
                        SendCommand_Async("FIRE");
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
                        SendCommand_Async("[AutoAimSeriesOfCommandsAndAutomation]");
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
                        SendCommand_Async("STOP");
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
                        SendCommand_Async("RECORD START");
                    }
                    else if (functionRunningName == "RECORD")
                    {
                        functionRunning = false;
                        functionRunningName = "";
                        SendCommand_Async("RECORD STOP");
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
                        SendCommand_Async("RECORD GO");
                    }
                    buttonTimes[8] = 0;
                }

                if (buttons.HasFlag(GamepadButtons.View))
                {
                    buttonTimes[9] += 1;
                    if (!functionRunning && buttonTimes[9] == viewButtonLong)
                    {
                        // Set START
                        SendCommand_Async("SETSTART");
                    }
                }
                else if (buttonTimes[9] > 0)
                {
                    if (!functionRunning && buttonTimes[9] < viewButtonLong)
                    {
                        functionRunning = true;
                        functionRunningName = "GOTO START";
                        SendCommand_Async("GOTO START");
                    }
                    buttonTimes[9] = 0;
                }

                if (functionRunning && functionRunningName.StartsWith("AIM") && !targetSet)
                {
                    // Aiming right now, so camera is needed

                    // Get Frames
                    SoftwareBitmap[] camFrames = await CaptureCamFrameAsync();
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
                        // Once done, remove rounding!
                        double steering = reading.LeftThumbstickX;
                        // Use either the left stick Y or the triggers for acceleration value:
                        double acceleration = reading.LeftTrigger - reading.RightTrigger;           // Use triggers
                        //double acceleration = reading.LeftThumbstickY;                              // Use left stick Y
                        // Remove sensitivity around 0 zones
                        double zoneRadius = 0.1;
                        steering = -zoneRadius < steering && steering < zoneRadius ? 0 : steering;
                        acceleration = -zoneRadius < acceleration && acceleration < zoneRadius ? 0 : acceleration;
                        motorSpeeds = ComputeMotorSpeeds(steering, acceleration, buttons.HasFlag(GamepadButtons.LeftThumbstick) ? 0.5 : 1);
                    }

                    if (loopsUntilMotorSend <= 0)
                    {
                        SendCommand_Async("SETMOTORS " + Math.Round(motorSpeeds[0], 2).ToString() + "," + Math.Round(motorSpeeds[1], 2).ToString());
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

        private double[] ComputeMotorSpeeds(double steering, double acceleration, double sensitivity)
        {
            double leftMotor = steering;
            double rightMotor = -steering;

            leftMotor += acceleration;
            rightMotor += acceleration;

            if (acceleration < -0.5)
            {
                double swap = leftMotor;
                leftMotor = rightMotor;
                rightMotor = swap;
            }

            leftMotor = leftMotor > sensitivity ? sensitivity : leftMotor < -sensitivity ? -sensitivity : leftMotor;
            rightMotor = rightMotor > sensitivity ? sensitivity : rightMotor <= -sensitivity ? -sensitivity : rightMotor;

            return new double[] { leftMotor, rightMotor };
        }

        private async void SendCommand_Async(string command)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (command.StartsWith("SETMOTORS"))
                {
                    MotorOutputBuffer.Text = command;
                }
                else
                {
                    OutputBuffer.Text = command + "\n" + OutputBuffer.Text;
                }
            });

            arduino.WriteCommandAsync(command);
        }

        private async Task<SoftwareBitmap[]> CaptureCamFrameAsync()
        {
            if (_camPreviewControl[0].Source != null)
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
            else
            {
                return null;
            }
        }
    }
}
