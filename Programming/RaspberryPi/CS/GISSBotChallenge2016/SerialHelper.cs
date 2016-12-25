using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace GISSBotChallenge2016
{
    class SerialHelper
    {

        private SerialDevice _device = null;
        private DataWriter _dataWriteObject = null;
        private DataReader _dataReadObject = null;
        private CancellationTokenSource _readCancellationTokenSource;

        private string _readBuffer = "";

        public SerialHelper()
        {
        }

        public async Task<DeviceInformationCollection> GetSerialDevicesAsync()
        {
            var dis = await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector());
            return dis;
        }

        public async Task InitializeAsync(DeviceInformation serialDeviceInfo)
        {
            try
            {
                _device = await SerialDevice.FromIdAsync(serialDeviceInfo.Id);
                _device.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                _device.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                _device.BaudRate = 9600;
                _device.Parity = SerialParity.None;
                _device.StopBits = SerialStopBitCount.One;
                _device.DataBits = 8;
                _device.Handshake = SerialHandshake.None;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public async Task WriteAsync(string data)
        {
            if (_device != null)
            {
                try
                {
                    _dataWriteObject = new DataWriter(_device.OutputStream);
                    _dataWriteObject.WriteString(data);
                    await _dataWriteObject.StoreAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                finally
                {
                    if (_dataWriteObject != null)
                    {
                        _dataWriteObject.DetachStream();
                        _dataWriteObject = null;
                    }
                }
            }
        }

        public async Task ListenAsync()
        {
            if (_device != null)
            {
                try
                {
                    _dataReadObject = new DataReader(_device.InputStream);

                    CancellationToken token = _readCancellationTokenSource.Token;

                    while (true)
                    {
                        token.ThrowIfCancellationRequested();
                        _dataReadObject.InputStreamOptions = InputStreamOptions.Partial;
                        UInt32 bytesRead = await _dataReadObject.LoadAsync(1024).AsTask(token);
                        _readBuffer += _dataReadObject.ReadString(bytesRead);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                finally
                {
                    if (_dataReadObject != null)
                    {
                        _dataReadObject.DetachStream();
                        _dataReadObject = null;
                    }
                }
            }
        }

        public string Buffer
        {
            get
            {
                string read = _readBuffer;
                _readBuffer = "";
                return read;
            }
        }

        public string StopListenAndGetBuffer()
        {
            if (_readCancellationTokenSource != null && !_readCancellationTokenSource.IsCancellationRequested)
            {
                _readCancellationTokenSource.Cancel();
            }
            return Buffer;
        }

        public void Dispose()
        {
            StopListenAndGetBuffer();
            if (_device != null)
            {
                _device.Dispose();
            }
            _device = null;
        }
    }
}
