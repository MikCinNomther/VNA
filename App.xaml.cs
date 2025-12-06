using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace VNA
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        static App application = new App();
        [STAThread]
        static void Main(string[] args)
        {
            application.Run(new MainWindow(args));
        }
    }
}
