﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace TextFileExport.UI_Tools
{
    public static class GridColumn
    {
        public static readonly DependencyProperty MinWidthProperty =
        DependencyProperty.RegisterAttached("MinWidth", typeof(double), typeof(GridColumn), new PropertyMetadata(75d, (s, e) => {
            if (s is GridViewColumn gridColumn)
            {
                SetMinWidth(gridColumn);
                ((System.ComponentModel.INotifyPropertyChanged)gridColumn).PropertyChanged += (cs, ce) => {
                    if (ce.PropertyName == nameof(GridViewColumn.ActualWidth))
                    {
                        SetMinWidth(gridColumn);
                    }
                };
            }
        }));

        private static void SetMinWidth(GridViewColumn column)
        {
            double minWidth = (double)column.GetValue(MinWidthProperty);

            if (column.Width < minWidth)
                column.Width = minWidth;
        }

        public static double GetMinWidth(DependencyObject obj) => (double)obj.GetValue(MinWidthProperty);

        public static void SetMinWidth(DependencyObject obj, double value) => obj.SetValue(MinWidthProperty, value);
    }
}
