﻿using RHGMTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RHGMTool.Views
{
    public partial class GearFrameUserControl : UserControl
    {
        private readonly GearFrameViewModel? _viewModel;

        public GearFrameUserControl()
        {
            InitializeComponent();
            _viewModel = new GearFrameViewModel();
            DataContext = _viewModel;
        }

        private Dictionary<TextBlock, double> visibleControlsHeights = [];

        private void TextBlock_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Cast sender to TextBlock

            if (sender is TextBlock textBlock)
            {
                if ((bool)e.NewValue)
                {
                    // TextBlock is visible
                    if (!visibleControlsHeights.TryGetValue(textBlock, out double value))
                    {
                        value = textBlock.ActualHeight;
                        // Add the TextBlock to the dictionary with its current height
                        visibleControlsHeights[textBlock] = value;
                    }
                    // Increase window height by the current height of the TextBlock
                    Height += value;
                }
                else
                {
                    // TextBlock is collapsed
                    if (visibleControlsHeights.TryGetValue(textBlock, out double value))
                    {
                        // Decrease window height by the current height of the TextBlock
                        Height -= value;
                        // Remove the TextBlock from the dictionary
                        visibleControlsHeights.Remove(textBlock);
                    }
                }
            }
        }
    }
}
