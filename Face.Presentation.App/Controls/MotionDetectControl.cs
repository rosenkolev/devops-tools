using System;
using System.Device.Gpio;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Face.Presentation.App.Controls
{
    public sealed class MotionDetectControl
    {
        private const int SecondsDelayBetweenMotionDetection = 7;

        private static object _locker = new object();
        private static object _lockerRegister = new object();
        private static DateTime _lastMotionDateTime = DateTime.MinValue;
        private readonly GpioController _controller = new GpioController(PinNumberingScheme.Logical);

        private readonly ILogger _logger;

        public MotionDetectControl(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>Gets or sets the motion detection action.</summary>
        public Action OnMotionDetected { get; set; }

        public void AttachToPin(int pin)
        {
            _logger.LogDebug($"Open pin {pin}!");
            if (!_controller.IsPinModeSupported(pin, PinMode.Input))
            {
                _logger.LogError($"Pin {pin} do not support pin mode!");
                return;
            }
            
            _controller.OpenPin(pin, PinMode.Input);
            if (!_controller.IsPinOpen(pin))
            {
                _logger.LogError($"Pin {pin} do not open!");
                return;
            }

            _logger.LogDebug($"Attach to pin {pin}!");
            _controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising, OnPinValueChanged);
            _logger.LogDebug($"Registered pin {pin} rising!");
            _controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Falling, OnPinValueChanged);
            _logger.LogDebug($"Registered pin {pin} falling!");
        }

        public void DetachFromPin(int pin)
        {
            _controller.UnregisterCallbackForPinValueChangedEvent(pin, OnPinValueChanged);
        }

        public void OnPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            lock (_locker)
            {
                _logger.LogDebug("Motion detected");

                var now = DateTime.Now;
                var timeDifference = now -_lastMotionDateTime;
                if (timeDifference.Seconds < SecondsDelayBetweenMotionDetection)
                {
                    _logger.LogDebug("Skip this motion!");
                    return;
                }

                _lastMotionDateTime = now;
                OnMotionDetected();               
            }
        }
    }
}