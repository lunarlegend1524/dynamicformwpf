using System.Windows;
using DynamicFormWPF.Classes_Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for DatabaseCreator.xaml
    /// </summary>
    public partial class DatabaseCreator : Window
    {
        private MainWindow mainForm;

        public DatabaseCreator(MainWindow main)
        {
            mainForm = main;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void _btnSaveTargetName_Click(object sender, RoutedEventArgs e)
        {
            string fileName = _txtDBName.Text;
            string info = DB.createNewDB(fileName);
            if (info == "NotOK")
            {
                MessageBox.Show("Tên cơ sở dữ liệu không được chứa dấu cách, xin chọn tên khác", "Thông báo");
                return;
            }

            else
            {
                MessageBox.Show(info, "Thông báo");
            }
            mainForm.loadTreeList();
            this.Close();
        }
    }
}