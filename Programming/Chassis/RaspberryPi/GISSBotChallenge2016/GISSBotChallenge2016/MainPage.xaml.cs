using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        public MainPage()
        {
            this.InitializeComponent();

            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += readGamepad;
            dispatcherTimer.Start();
        }

        private async Task Display(string text)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    /*currentState.Text = text;*/
                }
                );
        }

        private async void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            await Display("Gamepad Added");
        }

        private async void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            await Display("Gamepad Removed");
        }

        private void readGamepad(object sender, object e)
        {
            if (Gamepad.Gamepads.Count > 0)
            {
                controller = Gamepad.Gamepads.First();
                GamepadReading reading = controller.GetCurrentReading();
                LeftThumbstickState.Text = "Left Stick: " + Math.Round(reading.LeftThumbstickX, 1) + ", " + Math.Round(reading.LeftThumbstickY, 1);
                RightThumbstickState.Text = "Right Stick: " + Math.Round(reading.RightThumbstickX, 1) + ", " + Math.Round(reading.RightThumbstickY, 1);
                LeftTriggerState.Text = "Left Trigger: " + Math.Round(reading.LeftTrigger, 1);
                RightTriggerState.Text = "Right Trigger: " + Math.Round(reading.RightTrigger, 1);
                GamepadButtons buttons = reading.Buttons;
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
                if (buttons.HasFlag(GamepadButtons.Y))
                {
                    controller.Vibration = new GamepadVibration { LeftTrigger = reading.LeftTrigger, RightTrigger = reading.RightTrigger };
                }
                else if (buttons.HasFlag(GamepadButtons.X))
                {
                    controller.Vibration = new GamepadVibration { LeftMotor = reading.LeftTrigger, RightMotor = reading.RightTrigger };
                }
                else
                {
                    controller.Vibration = new GamepadVibration { LeftTrigger = 0, RightTrigger = 0, LeftMotor = 0, RightMotor = 0 };
                }
            }
        }
    }
}
