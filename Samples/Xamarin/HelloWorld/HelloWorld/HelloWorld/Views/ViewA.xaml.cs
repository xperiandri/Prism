﻿using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace HelloWorld.Views
{
    [NavigationPageProvider(typeof(ViewANavigationPageProvider))]
    public partial class ViewA : ContentPage
    {
        public ViewA()
        {
            InitializeComponent();
        }
    }
}
