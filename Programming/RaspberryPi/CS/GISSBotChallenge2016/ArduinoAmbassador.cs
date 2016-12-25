using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GISSBotChallenge2016
{
    class ArduinoAmbassador
    {
        // Class for communicating with Arudino
        // Can also be used for simulation (requires more programming!)

        public string buffer = "";

        private SerialHelper _serialHelper;

        public ArduinoAmbassador(SerialHelper serialHelper)
        {
            _serialHelper = serialHelper;
            if (_serialHelper != null)
            {
                Task.Run(async () => { await _serialHelper.ListenAsync(); });
            }
        }

        public string ReadBufferAndGetStatus()
        {
            if (_serialHelper != null)
            {
                buffer += _serialHelper.Buffer;
                if (buffer.Count(c => c == '\n') >= 2)
                {
                    string[] statuses = buffer.Split('\n');
                    buffer = buffer.Remove(0, buffer.Count() - statuses.Last().Count() - statuses[buffer.Count() - 2].Count() - 1);
                    return statuses[buffer.Count() - 2];
                }
                else
                {
                    return "WAIT 0,0 0,0 0,0 0 0000 0 N";
                }
            }
            else
            {
                return "OK 0,0 0,0 0,0 0 0000 0 N";
            }
        }

        public bool IsOK
        {
            get
            {
                return ReadBufferAndGetStatus().Contains("OK");
            }
        }

        public bool IsSimulator
        {
            get
            {
                return _serialHelper == null;
            }
        }

        public async void WriteCommandAsync(string command)
        {
            string write = command + "\n";
            if (_serialHelper != null)
            {
                await _serialHelper.WriteAsync(write);
            }
        }

        public void Dispose()
        {
            if (_serialHelper != null)
            {
                _serialHelper.Dispose();
            }
            _serialHelper = null;
        }
    }
}
