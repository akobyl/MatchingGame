using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


// From: http://stackoverflow.com/questions/4479532/adding-and-removing-wpf-uielements-at-runtime


namespace MatchMe
{
    public static class UIElementExtensions
    {
        public static Int32 GetGroupID(DependencyObject obj)
        {
            return (Int32)obj.GetValue(GroupIDProperty);
        }

        public static void SetGroupID(DependencyObject obj, Int32 value)
        {
            obj.SetValue(GroupIDProperty, value);
        }

        public static readonly DependencyProperty GroupIDProperty =
            DependencyProperty.RegisterAttached("GroupID", typeof(Int32), typeof(UIElementExtensions), new UIPropertyMetadata(null));
    }
}
