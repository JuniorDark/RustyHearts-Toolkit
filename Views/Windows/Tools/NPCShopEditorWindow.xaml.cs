﻿using RHToolkit.ViewModels.Windows;
using System.ComponentModel;

namespace RHToolkit.Views.Windows
{
    public partial class NPCShopEditorWindow : Window
    {
        public NPCShopEditorWindow(NPCShopEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is NPCShopEditorViewModel viewModel)
            {
                bool canClose = await viewModel.CloseFile();

                if (!canClose)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
