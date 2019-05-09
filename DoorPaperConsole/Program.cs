using System;
using Doorgen.Core;

namespace DoorGenConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            int result = 0;

            DoorgenCoreClass doorgenCore = new DoorgenCoreClass();
            if (doorgenCore.CheckConfigs())
            {
                try
                {
                    //doorgenCore.WriteTestConfig();
                    doorgenCore.ReadConfig();
                    doorgenCore.Process();
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    result = 1;
                }

            }

            return result;
        }
    }
}
