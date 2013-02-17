using System;
using System.Collections;
using System.Configuration;
using Dokan;

namespace GridFS
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = ConfigurationSettings.AppSettings;

            var gfs = new GridFS(
                settings["MongoDBConnectionString"], 
                settings["GridFSDB"]
            );

            gfs.FindFiles(@"C:\Uploads", new ArrayList(), null);

            return; //still baby steps

            var opt = new DokanOptions{
#if DEBUG
                DebugMode = true,
#endif
                MountPoint = settings["MountPoint"],
                ThreadCount = 5,

            };

            var status = DokanNet.DokanMain(opt, gfs);
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
                    Console.WriteLine("Unknown status: %d", status);
                    break;

            }
        }
    }
}
