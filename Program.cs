using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace GridFS
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = ConfigurationSettings.AppSettings;
            string cs = settings["MongoDBConnectionString"];
            string gdb = settings["GridFSDB"];

            var gfs = new GridFS(cs, gdb);
            gfs.FindFiles(@"C:\Uploads", new ArrayList(), null);
        }
    }
}
