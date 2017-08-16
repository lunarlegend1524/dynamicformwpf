namespace DynamicFormWPF
{
    using System.Windows;
    using DynamicFormWPF.Classes_Data;

    /// <summary>
    /// Interaction logic for TargetEditor.xaml
    /// </summary>
    public partial class TargetEditor : Window
    {
        private static int targetID = 0;
        private TargetCreator parentForm;

        public TargetEditor(TargetCreator parent, int ID)
        {
            targetID = ID;
            parentForm = parent;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _txtTargetNameEdit.Text = DB.getNameByID(targetID, "Target");
        }

        private void _btnSaveTargetName_Click(object sender, RoutedEventArgs e)
        {
            string info = string.Empty;

            if (_txtTargetNameEdit.Text == DB.getNameByID(targetID, "Target"))
            {
                MessageBox.Show("Xin thay đổi tên chỉ tiêu", "Thông báo");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Sửa tên chỉ tiêu ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                info = DB.editTargetName(targetID, _txtTargetNameEdit.Text);
                parentForm.loadTreeList();
                MessageBox.Show(info, "Thông báo");
                this.Close();
            }
        }
    }
}