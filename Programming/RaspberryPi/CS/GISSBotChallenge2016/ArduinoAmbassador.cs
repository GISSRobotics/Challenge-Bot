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
