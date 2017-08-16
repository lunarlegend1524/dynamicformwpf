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
using System.Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for TargetCreator.xaml
    /// </summary>
    public partial class TargetCreator : Window
    {
        MainWindow mainWindow;
        public TargetCreator(MainWindow root)
        {
            mainWindow = root;
            InitializeComponent();
        }

        private string targetLV1 = string.Empty;
        private string targetLV2 = string.Empty;
        private string targetLV3 = string.Empty;
        private string targetLV4 = string.Empty;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadTreeView();
            loadComboboxes();
        }

        // reload target tree in main window
        private void loadMainTreeView()
        {
            mainWindow.loadTreeView();
        }

        private void loadComboboxes() 
        {
            DataTable dt = new DataTable();
            dt = DB.getTargetList(0);

            _cbbLV1.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            _cbbLV1.DisplayMemberPath = "TargetName";
            _cbbLV1.SelectedValuePath = "TargetID";
        }

        private void loadTreeView()
        {
            _treeViewTarget.Items.Clear();

            DataTable parentNodes = DB.getTargetList(-1);

            Dictionary<int, TreeViewItem> flattenedTree = new Dictionary<int, TreeViewItem>();

            foreach (DataRow node in parentNodes.Rows)
            {
                TreeViewItem treeNode = new TreeViewItem();
                treeNode.Header = node["TargetName"];
                treeNode.Tag = node["TargetID"];
                flattenedTree.Add(Convert.ToInt32(node["TargetID"]), treeNode);

                if (flattenedTree.ContainsKey(Convert.ToInt32(node["ParentID"]))) // create child nodes
                {
                    flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
                }
                else // create new parent node
                {
                    _treeViewTarget.Items.Add(treeNode);
                }
                treeNode.IsExpanded = true;
            }
        }

        private void _btnAddLV1_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV1.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu");
            }
            targetLV1 = _cbbLV1.Text;
            if (DB.isExistedParentandChild(targetLV1, "null"))
            {
                MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu");
            }
            else
            {
                DB.addNewTarget(targetLV1, "");
                loadTreeView();
                loadMainTreeView();
                loadTargetComboboxes(_cbbLV1, 0);
            }
        }

        void _btnAddLV2_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV2.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu");
            }
            targetLV1 = _cbbLV1.Text;
            targetLV2 = _cbbLV2.Text;
            if (DB.isExistedParentandChild(targetLV2, targetLV1))
            {
                MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu");
            }
            else
            {
                DB.addNewTarget(targetLV2, targetLV1);
                loadTreeView();
                loadMainTreeView();
            }
        }

        private void _btnAddLV3_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV3.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu");
            }
            targetLV2 = _cbbLV2.Text;
            targetLV3 = _cbbLV3.Text;
            if (DB.isExistedParentandChild(targetLV3, targetLV2))
            {
                MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu");
            }
            else
            {
                DB.addNewTarget(targetLV3, targetLV2);
                loadTreeView();
                loadMainTreeView();
            }
        }

        private void _btnAddLV4_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV4.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu");
            }
            targetLV3 = _cbbLV3.Text;
            targetLV4 = _cbbLV4.Text;
            if (DB.isExistedParentandChild(targetLV4, targetLV3))
            {
                MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu");
            }
            else
            {
                DB.addNewTarget(targetLV4, targetLV3);
                loadTreeView();
                loadMainTreeView();
            }
        }

        private void _treeViewTarget_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem tvi = _treeViewTarget.ItemContainerGenerator.ContainerFromItem(_treeViewTarget.SelectedItem) as TreeViewItem;
            if (tvi != null)
            {
                tvi.IsSelected = true;
            }
        }

        private void loadTargetComboboxes(ComboBox cbb,int id) 
        {
            DataTable dt = new DataTable();
            dt = DB.getTargetList(id);

            cbb.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            cbb.DisplayMemberPath = "TargetName";
            cbb.SelectedValuePath = "TargetID";
        }

        // load children when select parent in combobox
        private void _cbbLV1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV2.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV1.SelectedValue);

            loadTargetComboboxes(_cbbLV2, id);
        }

        private void _cbbLV2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV3.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV2.SelectedValue);

            loadTargetComboboxes(_cbbLV3, id);
        }

        private void _cbbLV3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV4.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV3.SelectedValue);

            loadTargetComboboxes(_cbbLV4, id);
        }
    }
}
