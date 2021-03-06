﻿using System.Windows;

namespace Octide
{
    using GalaSoft.MvvmLight.Threading;
    using Octgn.Library;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void StartupHandler(object sender, System.Windows.StartupEventArgs e)
        {
            Elysium.Manager.Apply(this, Elysium.Theme.Dark, Elysium.AccentBrushes.Blue, Brushes.White);
			Config.Instance = new Config();
        }

        static App()
        {
            DispatcherHelper.Initialize();
        }
    }
}
