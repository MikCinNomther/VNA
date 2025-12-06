using System;
using System.Windows;
using System.Windows.Controls;

namespace VNA.Controls
{
    /// <summary>
    /// AlertTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class AlertTextBox : UserControl
    {
        // Text 依赖属性
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(AlertTextBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextPropertyChanged));

        // DialogText 依赖属性
        public static readonly DependencyProperty DialogTextProperty =
            DependencyProperty.Register(
                nameof(DialogText),
                typeof(string),
                typeof(AlertTextBox),
                new PropertyMetadata(string.Empty, OnDialogTextPropertyChanged));

        public AlertTextBox()
        {
            InitializeComponent();
            // 绑定 TextBox 的文本变化事件
            TextBox.TextChanged += TextBox_TextChanged;
        }

        /// <summary>
        /// 文本框内容
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// 提示文本
        /// </summary>
        public string DialogText
        {
            get { return (string)GetValue(DialogTextProperty); }
            set { SetValue(DialogTextProperty, value); }
        }

        // Text 属性变更时的处理
        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as AlertTextBox;
            if (control != null && control.TextBox != null)
            {
                control.TextBox.Text = e.NewValue as string ?? string.Empty;
            }
        }

        // DialogText 属性变更时的处理
        private static void OnDialogTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as AlertTextBox;
            if (control != null && control.Dialog != null)
            {
                control.Dialog.Text = e.NewValue as string ?? string.Empty;
            }
        }

        // TextBox 文本变化时更新依赖属性
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 避免无限循环
            if (Text != TextBox.Text)
            {
                Text = TextBox.Text;
            }
        }
        public Boolean IsReadOnly
        {
            get
            {
                return TextBox.IsReadOnly;
            }
            set
            {
                TextBox.IsReadOnly = value;
            }
        }
    }
}