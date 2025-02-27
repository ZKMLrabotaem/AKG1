using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Aspose.ThreeD;
using lab1.MatrixOperations;

namespace lab1
{
    public class MyClass
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            global::System.Windows.Forms.Application.EnableVisualStyles();
            global::System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            global::System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new Forms.Scene());
        }

    }
}