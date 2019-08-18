using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace SPVP
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            if (e.Args != null && e.Args.Length > 0)
            {
                string filePath = e.Args[0];
                if (System.IO.File.Exists(filePath))
                {
                    mainWindow.VideoPath = filePath;
                }
            }
            mainWindow.ShowDialog();
        }
    }
}
