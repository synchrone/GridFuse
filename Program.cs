﻿using System;
using System.Collections.Specialized;
using System.Configuration;
using Dokan;

namespace GridFuse
{
    class Program
    {
        private static NameValueCollection Settings
        {
            get { return ConfigurationManager.AppSettings; }
        }

        static void Main()
        {
            Console.WriteLine("Connecting to GridFS");
            var gfs = new GridFs(
                Settings["MongoDBConnectionString"],
                Settings["GridFSDB"],
                Settings["PrefixPath"]
            );

            var opt = new DokanOptions{
#if DEBUG
                DebugMode = true,
#endif
                MountPoint = Settings["MountPoint"],
                ThreadCount = 1,

            };
            Console.WriteLine("Mounting {0}",Settings["MountPoint"]);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
            var status = DokanNet.DokanMain(opt, gfs);
            gfs.Dispose();

            switch (status)
            {
                case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                    Console.WriteLine("Drvie letter error");
                    break;
                case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                    Console.WriteLine("Driver install error");
                    break;
                case DokanNet.DOKAN_MOUNT_ERROR:
                    Console.WriteLine("Mount error");
                    break;
                case DokanNet.DOKAN_START_ERROR:
                    Console.WriteLine("Start error");
                    break;
                case DokanNet.DOKAN_ERROR:
                    Console.WriteLine("Unknown error");
                    break;
                case DokanNet.DOKAN_SUCCESS:
                    Console.WriteLine("Success");
                    break;
                default:
                    Console.WriteLine("Unknown status: {0}", status);
                    break;

            }
        }
        static void CurrentDomainProcessExit(object sender, EventArgs e)
        {
            DokanNet.DokanUnmount(Settings["MountPoint"].ToCharArray()[0]);
        }
    }
}
