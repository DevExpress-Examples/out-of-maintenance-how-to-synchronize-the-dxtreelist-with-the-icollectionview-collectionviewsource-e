using System;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Specialized;
using DevExpress.Data.Filtering;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;

namespace TreeListControlViewModel {
    public enum ModelFilterMode {
        CollectionViewFilterPredicate,
        FilterCriteria,
    }
    public class TreeListControlDataModel {
        #region static
        public static readonly DependencyProperty TreeListControlDataModelProperty;
        public static readonly DependencyProperty IsSynchronizedWithCurrentItemProperty;
        public static readonly DependencyProperty CollectionViewProperty;

        public static TreeListControlDataModel GetTreeListControlDataModel(TreeListControl treeList) {
            return (TreeListControlDataModel)treeList.GetValue(TreeListControlDataModelProperty);
        }
        public static void SetTreeListControlDataModel(TreeListControl treeListControl, TreeListControlDataModel value) {
            treeListControl.SetValue(TreeListControlDataModelProperty, value);
        }
        public static bool GetIsSynchronizedWithCurrentItem(TreeListControl treeList) {
            return (bool)treeList.GetValue(IsSynchronizedWithCurrentItemProperty);
        }
        public static void SetIsSynchronizedWithCurrentItem(TreeListControl treeList, bool value) {
            treeList.SetValue(IsSynchronizedWithCurrentItemProperty, value);
        }
        public static ICollectionView GetCollectionView(TreeListControl treeList) {
            return (ICollectionView)treeList.GetValue(CollectionViewProperty);
        }
        public static void SetCollectionView(TreeListControl treeList, ICollectionView value) {
            treeList.SetValue(CollectionViewProperty, value);
        }

        static TreeListControlDataModel() {
            TreeListControlDataModelProperty = DependencyProperty.RegisterAttached("TreeListControlDataModel", typeof(TreeListControlDataModel), typeof(TreeListControlDataModel), new UIPropertyMetadata(null, OnTreeListControlDataModelChanged));
            IsSynchronizedWithCurrentItemProperty = DependencyProperty.RegisterAttached("IsSynchronizedWithCurrentItem", typeof(bool), typeof(TreeListControlDataModel), new UIPropertyMetadata(true));
            CollectionViewProperty = DependencyProperty.RegisterAttached("CollectionView", typeof(ICollectionView), typeof(TreeListControlDataModel), new UIPropertyMetadata(null, OnCollectionViewChanged));
        }
        static void OnTreeListControlDataModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            TreeListControl treeListControl = (TreeListControl)d;
            TreeListControlDataModel model = (TreeListControlDataModel)e.NewValue;
            if(model != null)
                model.AttachToTreeListControl(treeListControl);
        }
        static void OnCollectionViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            TreeListControl treeListControl = (TreeListControl)d;
            ICollectionView view = (ICollectionView)e.NewValue;
            SetTreeListControlDataModel(treeListControl, new TreeListControlDataModel() { CollectionView = view });
        }
        #endregion
        ICollectionView collectionView;
        TreeListControl treeListControl;
        Locker syncGroupSortLocker = new Locker();
        ModelFilterMode filterMode;
        CriteriaOperator filterCriteria;
        public TreeListControlDataModel() {
            AutoPopulateColumns = true;
        }
        public bool AutoPopulateColumns { get; set; }
        public ModelFilterMode FilterMode {
            get { return filterMode; }
            set {
                if(filterMode == value)
                    return;
                filterMode = value;
                SyncFilter();
                if(treeListControl != null)
                    treeListControl.RefreshData();
            }
        }
        public CriteriaOperator FilterCriteria {
            get { return filterCriteria; }
            set {
                if(object.ReferenceEquals(filterCriteria, value))
                    return;
                filterCriteria = value;
                SyncFilter();
            }
        }
        public ICollectionView CollectionView {
            get { return collectionView; }
            set {
                if(collectionView == value)
                    return;
                if(collectionView != null) {
                    INotifyPropertyChanged notifyPropertyChanged = collectionView as INotifyPropertyChanged;
                    if(notifyPropertyChanged != null)
                        notifyPropertyChanged.PropertyChanged -= new PropertyChangedEventHandler(OnCollectionViewPropertyChanged);
                    if(collectionView.SortDescriptions != null)
                        ((INotifyCollectionChanged)collectionView.SortDescriptions).CollectionChanged -= new NotifyCollectionChangedEventHandler(OnSortDescriptionsCollectionChanged);
                    collectionView.CurrentChanged -= new EventHandler(OnCollectionViewCurrentChanged);
                }
                collectionView = value;
                if(collectionView != null) {
                    INotifyPropertyChanged notifyPropertyChanged = collectionView as INotifyPropertyChanged;
                    if(notifyPropertyChanged != null)
                        notifyPropertyChanged.PropertyChanged += new PropertyChangedEventHandler(OnCollectionViewPropertyChanged);
                    if(collectionView.SortDescriptions != null)
                        ((INotifyCollectionChanged)collectionView.SortDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(OnSortDescriptionsCollectionChanged);
                    collectionView.CurrentChanged += new EventHandler(OnCollectionViewCurrentChanged);
                }
            }
        }

        void OnCollectionViewCurrentChanged(object sender, EventArgs e) {
            SyncFocusedRowHandle();
        }
        void SyncFocusedRowHandle() {
            if(CanSyncCurrentRow())
                treeListControl.View.FocusedRow = collectionView.CurrentItem;
        }
        void OnTreeListControlFocusedRowChanged(object sender, FocusedRowChangedEventArgs e) {
            if(CanSyncCurrentRow() && treeListControl.View.FocusedRowHandle != GridControl.InvalidRowHandle)
                collectionView.MoveCurrentTo(treeListControl.View.FocusedRow);
        }
        bool CanSyncCurrentRow() {
            return treeListControl != null && GetIsSynchronizedWithCurrentItem(treeListControl);
        }
        void OnSortDescriptionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if(treeListControl == null)
                return;
            SyncSorting();
        }
        void OnCollectionViewPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName == "Count")
                SyncFilter();
        }
        void SyncFilter() {
            if(treeListControl == null)
                return;
            if(FilterMode == ModelFilterMode.FilterCriteria) {
                treeListControl.FilterCriteria = FilterCriteria;
                treeListControl.View.AllowColumnFiltering = true;
            }
            else {
                treeListControl.FilterCriteria = null;
                treeListControl.RefreshData();
                treeListControl.View.AllowColumnFiltering = false;
            }
        }
        void AttachToTreeListControl(TreeListControl treeListControl) {
            if(this.treeListControl != null) {
                this.treeListControl.View.CustomNodeFilter -= new TreeListNodeFilterEventHandler(OnTreeListCustomNodeFilter);
                this.treeListControl.SortInfo.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnSortInfoCollectionChanged);
                this.treeListControl.View.FocusedRowChanged -= new FocusedRowChangedEventHandler(OnTreeListControlFocusedRowChanged);
                TypeDescriptor.GetProperties(typeof(GridControl))[GridControl.FilterCriteriaProperty.Name].RemoveValueChanged(treeListControl, OnTreeListControlFilterCriteriaChanged);
            }
            this.treeListControl = treeListControl;
            if(treeListControl == null)
                return;
            treeListControl.AutoPopulateColumns = AutoPopulateColumns;
            if(CollectionView == null)
                return;
            treeListControl.ItemsSource = CollectionView.SourceCollection;
            treeListControl.BeginInit();
            try {
                treeListControl.View.CustomNodeFilter += new TreeListNodeFilterEventHandler(OnTreeListCustomNodeFilter);
                treeListControl.SortInfo.CollectionChanged += new NotifyCollectionChangedEventHandler(OnSortInfoCollectionChanged);
                treeListControl.View.FocusedRowChanged += new FocusedRowChangedEventHandler(OnTreeListControlFocusedRowChanged);
                TypeDescriptor.GetProperties(typeof(GridControl))[GridControl.FilterCriteriaProperty.Name].AddValueChanged(treeListControl, OnTreeListControlFilterCriteriaChanged);
                SyncSorting();
                SyncFocusedRowHandle();
                SyncFilter();
                treeListControl.View.AllowSorting = collectionView.CanSort;
            }
            finally {
                treeListControl.EndInit();
            }
        }
        void OnTreeListControlFilterCriteriaChanged(object sender, EventArgs e) {
            FilterCriteria = treeListControl.FilterCriteria;
        }
        void OnSortInfoCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if(syncGroupSortLocker.IsLocked)
                return;
            syncGroupSortLocker.DoLockedAction(SyncSortInfo);
        }
        void SyncSortInfo() {
            if(CollectionView == null)
                return;
            if(CollectionView.SortDescriptions != null) {
                CollectionView.SortDescriptions.Clear();
                for(int i = 0; i < treeListControl.SortInfo.Count; i++) {
                    GridSortInfo info = treeListControl.SortInfo[i];
                    CollectionView.SortDescriptions.Add(new SortDescription(info.FieldName, info.SortOrder));
                }
            }
        }
        void SyncSorting() {
            if(syncGroupSortLocker.IsLocked)
                return;
            syncGroupSortLocker.DoLockedAction(delegate() {
                if(CollectionView.SortDescriptions != null) {
                    treeListControl.SortInfo.BeginUpdate();
                    try {
                        treeListControl.ClearSorting();
                        foreach(SortDescription sortDescription in CollectionView.SortDescriptions) {
                            treeListControl.SortInfo.Add(new GridSortInfo() { FieldName = sortDescription.PropertyName, SortOrder = sortDescription.Direction });
                        }
                    }
                    finally {
                        treeListControl.SortInfo.EndUpdate();
                    }
                }
            });
        }
        void OnTreeListCustomNodeFilter(object sender, TreeListNodeFilterEventArgs e) {
            if(CollectionView.Filter == null || FilterMode == ModelFilterMode.FilterCriteria)
                return;
            e.Visible = CollectionView.Filter(e.Node.Content);
            e.Handled = true;
        }
    }

    
}

