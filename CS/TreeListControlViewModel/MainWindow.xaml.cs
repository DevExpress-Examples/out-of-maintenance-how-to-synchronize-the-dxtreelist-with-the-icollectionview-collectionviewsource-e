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
using System.Threading;
using System.Windows.Threading;

namespace TreeListControlViewModel {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        ListCollectionView view;
        public MainWindow() {
            InitializeComponent();
            List<TestData> list = TestData.CreateTestData();
            view = new ListCollectionView(list);
            DataContext = view;
            filterComboBox.SelectionChanged += OnComboBoxSelectionChanged;
        }
        void OnComboBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Dispatcher.BeginInvoke(new ThreadStart(UpdateFilter), DispatcherPriority.Background);
        }
        void UpdateFilter() {
            if(filterComboBox.SelectedIndex == 1)
                view.Filter = (obj) => { return ((TestData)obj).ParentId == -1; };

            else
                view.Filter = null;
        }
    }
    public class TestData {
        public static List<TestData> CreateTestData() {
            List<TestData> list = new List<TestData>();
            list.Add(new TestData() { Id = 0, ParentId = -1, Text1 = "Item1", Text2 = "Item1" });
            list.Add(new TestData() { Id = 1, ParentId = 0, Text1 = "Item2", Text2 = "Item2" });
            list.Add(new TestData() { Id = 2, ParentId = -1, Text1 = "Item3", Text2 = "Item3" });
            list.Add(new TestData() { Id = 3, ParentId = 2, Text1 = "Item4", Text2 = "Item4" });
            list.Add(new TestData() { Id = 4, ParentId = -1, Text1 = "Item5", Text2 = "Item5" });
            return list;
        }

        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
    }
}
