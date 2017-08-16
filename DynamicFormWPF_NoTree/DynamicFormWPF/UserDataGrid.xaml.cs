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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for UserDataGrid.xaml
    /// </summary>
    public partial class UserDataGrid : DataGrid
    {
        public UserDataGrid()
        {
            InitializeComponent();
        }

        public override void DataSource(DataTable dt)
        {
            this.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
        }
    }
}
