using System;
using System.Windows;
using System.Windows.Controls;

namespace RTCSharp
{
    /// <summary>
    /// Interaction logic for TextBoxLabel.xaml
    /// </summary>
    public partial class TextBoxLabel : UserControl
    {
        #region LabelText DP

        /// <summary>
        /// Gets or sets the Label which is displayed next to the field
        /// </summary>
        public String LabelText
        {
            get { return (String)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register("LabelText", typeof(string), typeof(TextBoxLabel), new PropertyMetadata(""));

        #endregion

        #region Value DP

        /// <summary>
        /// Gets or sets the Value which is being displayed
        /// </summary>
        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(TextBoxLabel), new PropertyMetadata(null));

        #endregion

        public TextBoxLabel()
        {
            InitializeComponent();
        }
    }
}
