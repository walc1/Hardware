using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Caliburn.Micro;

namespace Ntree.ReaderTool.Light.ViewModels
{
    class ShellViewModel : Screen
    {
        public ShellViewModel()
        {
            var assem = Assembly.GetExecutingAssembly();
            var aName = assem.GetName();

            DisplayName = "n-tree ReaderTool Light  " + aName.Version.ToString();
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
