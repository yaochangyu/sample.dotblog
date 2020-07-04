using System;
using System.Windows;
using NetWpf48.Behaviors;
using PowerArgs;

namespace NetWpf48
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var args = Environment.GetCommandLineArgs();
            Args.InvokeAction<MultipleBehavior>(args);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var args = e.Args;
        }
    }
}