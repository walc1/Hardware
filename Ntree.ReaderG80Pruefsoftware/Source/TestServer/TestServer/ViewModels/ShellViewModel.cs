using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;

namespace TestServer.ViewModels
{
    class ShellViewModel : Screen
    {
        public ShellViewModel()
        {
            DisplayName = "n-tree Test Server";
            MainViewModel = new MainViewModel();
        }

        public MainViewModel MainViewModel { get; set; }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            if (close)
            {
                MainViewModel.Dispose();
            }
        }
    }
}
