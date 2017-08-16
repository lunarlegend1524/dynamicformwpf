namespace DynamicFormWPF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for TargetEditor.xaml
    /// </summary>
    public partial class TargetEditor : Window
    {
        private static int targetID = 0;
        TargetCreator parentForm;

        public TargetEditor(TargetCreator parent, int ID)
        {
            targetID = ID;
            parentForm = parent;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _txtTargetNameEdit.Text = DB.getTargetNameByID(targetID);
        }

        private void _btnSaveTargetName_Click(object sender, RoutedEventArgs e)
        {
            string info = string.Empty;

            if (_txtTargetNameEdit.Text == DB.getTargetNameByID(targetID))
            {
                MessageBox.Show("Xin thay đổi tên chỉ tiêu", "Thông báo");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Sửa tên chỉ tiêu ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK) 
            {
                info = DB.editTargetName(targetID, _txtTargetNameEdit.Text);
                parentForm.loadTreeView();
                MessageBox.Show(info, "Thông báo");
                this.Close();
            }
        }
    }
}
