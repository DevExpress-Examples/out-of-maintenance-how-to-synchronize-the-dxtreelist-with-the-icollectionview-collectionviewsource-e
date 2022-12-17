Imports System
Imports System.Windows
Imports System.ComponentModel
Imports System.Collections.Specialized
Imports DevExpress.Data.Filtering
Imports DevExpress.Xpf.Core
Imports DevExpress.Xpf.Grid
Imports DevExpress.Xpf.Grid.TreeList

Namespace TreeListControlViewModel

    Public Enum ModelFilterMode
        CollectionViewFilterPredicate
        FilterCriteria
    End Enum

    Public Class TreeListControlDataModel

'#Region "static"
        Public Shared ReadOnly TreeListControlDataModelProperty As DependencyProperty

        Public Shared ReadOnly IsSynchronizedWithCurrentItemProperty As DependencyProperty

        Public Shared ReadOnly CollectionViewProperty As DependencyProperty

        Public Shared Function GetTreeListControlDataModel(ByVal treeList As TreeListControl) As TreeListControlDataModel
            Return CType(treeList.GetValue(TreeListControlDataModelProperty), TreeListControlDataModel)
        End Function

        Public Shared Sub SetTreeListControlDataModel(ByVal treeListControl As TreeListControl, ByVal value As TreeListControlDataModel)
            treeListControl.SetValue(TreeListControlDataModelProperty, value)
        End Sub

        Public Shared Function GetIsSynchronizedWithCurrentItem(ByVal treeList As TreeListControl) As Boolean
            Return CBool(treeList.GetValue(IsSynchronizedWithCurrentItemProperty))
        End Function

        Public Shared Sub SetIsSynchronizedWithCurrentItem(ByVal treeList As TreeListControl, ByVal value As Boolean)
            treeList.SetValue(IsSynchronizedWithCurrentItemProperty, value)
        End Sub

        Public Shared Function GetCollectionView(ByVal treeList As TreeListControl) As ICollectionView
            Return CType(treeList.GetValue(CollectionViewProperty), ICollectionView)
        End Function

        Public Shared Sub SetCollectionView(ByVal treeList As TreeListControl, ByVal value As ICollectionView)
            treeList.SetValue(CollectionViewProperty, value)
        End Sub

        Shared Sub New()
            TreeListControlDataModelProperty = DependencyProperty.RegisterAttached("TreeListControlDataModel", GetType(TreeListControlDataModel), GetType(TreeListControlDataModel), New UIPropertyMetadata(Nothing, AddressOf OnTreeListControlDataModelChanged))
            IsSynchronizedWithCurrentItemProperty = DependencyProperty.RegisterAttached("IsSynchronizedWithCurrentItem", GetType(Boolean), GetType(TreeListControlDataModel), New UIPropertyMetadata(True))
            CollectionViewProperty = DependencyProperty.RegisterAttached("CollectionView", GetType(ICollectionView), GetType(TreeListControlDataModel), New UIPropertyMetadata(Nothing, AddressOf OnCollectionViewChanged))
        End Sub

        Private Shared Sub OnTreeListControlDataModelChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
            Dim treeListControl As TreeListControl = CType(d, TreeListControl)
            Dim model As TreeListControlDataModel = CType(e.NewValue, TreeListControlDataModel)
            If model IsNot Nothing Then model.AttachToTreeListControl(treeListControl)
        End Sub

        Private Shared Sub OnCollectionViewChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
            Dim treeListControl As TreeListControl = CType(d, TreeListControl)
            Dim view As ICollectionView = CType(e.NewValue, ICollectionView)
            Call SetTreeListControlDataModel(treeListControl, New TreeListControlDataModel() With {.CollectionView = view})
        End Sub

'#End Region
        Private collectionViewField As ICollectionView

        Private treeListControl As TreeListControl

        Private syncGroupSortLocker As Locker = New Locker()

        Private filterModeField As ModelFilterMode

        Private filterCriteriaField As CriteriaOperator

        Public Sub New()
            AutoPopulateColumns = True
        End Sub

        Public Property AutoPopulateColumns As Boolean

        Public Property FilterMode As ModelFilterMode
            Get
                Return filterModeField
            End Get

            Set(ByVal value As ModelFilterMode)
                If filterModeField = value Then Return
                filterModeField = value
                SyncFilter()
                If treeListControl IsNot Nothing Then treeListControl.RefreshData()
            End Set
        End Property

        Public Property FilterCriteria As CriteriaOperator
            Get
                Return filterCriteriaField
            End Get

            Set(ByVal value As CriteriaOperator)
                If ReferenceEquals(filterCriteriaField, value) Then Return
                filterCriteriaField = value
                SyncFilter()
            End Set
        End Property

        Public Property CollectionView As ICollectionView
            Get
                Return collectionViewField
            End Get

            Set(ByVal value As ICollectionView)
                If collectionViewField Is value Then Return
                If collectionViewField IsNot Nothing Then
                    Dim notifyPropertyChanged As INotifyPropertyChanged = TryCast(collectionViewField, INotifyPropertyChanged)
                    If notifyPropertyChanged IsNot Nothing Then RemoveHandler notifyPropertyChanged.PropertyChanged, New PropertyChangedEventHandler(AddressOf OnCollectionViewPropertyChanged)
                    If collectionViewField.SortDescriptions IsNot Nothing Then RemoveHandler CType(collectionViewField.SortDescriptions, INotifyCollectionChanged).CollectionChanged, New NotifyCollectionChangedEventHandler(AddressOf OnSortDescriptionsCollectionChanged)
                    RemoveHandler collectionViewField.CurrentChanged, New EventHandler(AddressOf OnCollectionViewCurrentChanged)
                End If

                collectionViewField = value
                If collectionViewField IsNot Nothing Then
                    Dim notifyPropertyChanged As INotifyPropertyChanged = TryCast(collectionViewField, INotifyPropertyChanged)
                    If notifyPropertyChanged IsNot Nothing Then AddHandler notifyPropertyChanged.PropertyChanged, New PropertyChangedEventHandler(AddressOf OnCollectionViewPropertyChanged)
                    If collectionViewField.SortDescriptions IsNot Nothing Then AddHandler CType(collectionViewField.SortDescriptions, INotifyCollectionChanged).CollectionChanged, New NotifyCollectionChangedEventHandler(AddressOf OnSortDescriptionsCollectionChanged)
                    AddHandler collectionViewField.CurrentChanged, New EventHandler(AddressOf OnCollectionViewCurrentChanged)
                End If
            End Set
        End Property

        Private Sub OnCollectionViewCurrentChanged(ByVal sender As Object, ByVal e As EventArgs)
            SyncFocusedRowHandle()
        End Sub

        Private Sub SyncFocusedRowHandle()
            If CanSyncCurrentRow() Then treeListControl.View.FocusedRow = collectionViewField.CurrentItem
        End Sub

        Private Sub OnTreeListControlFocusedRowChanged(ByVal sender As Object, ByVal e As FocusedRowChangedEventArgs)
            If CanSyncCurrentRow() AndAlso treeListControl.View.FocusedRowHandle <> DataControlBase.InvalidRowHandle Then collectionViewField.MoveCurrentTo(treeListControl.View.FocusedRow)
        End Sub

        Private Function CanSyncCurrentRow() As Boolean
            Return treeListControl IsNot Nothing AndAlso GetIsSynchronizedWithCurrentItem(treeListControl)
        End Function

        Private Sub OnSortDescriptionsCollectionChanged(ByVal sender As Object, ByVal e As NotifyCollectionChangedEventArgs)
            If treeListControl Is Nothing Then Return
            SyncSorting()
        End Sub

        Private Sub OnCollectionViewPropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs)
            If Equals(e.PropertyName, "Count") Then SyncFilter()
        End Sub

        Private Sub SyncFilter()
            If treeListControl Is Nothing Then Return
            If FilterMode = ModelFilterMode.FilterCriteria Then
                treeListControl.FilterCriteria = FilterCriteria
                treeListControl.View.AllowColumnFiltering = True
            Else
                treeListControl.FilterCriteria = Nothing
                treeListControl.RefreshData()
                treeListControl.View.AllowColumnFiltering = False
            End If
        End Sub

        Private Sub AttachToTreeListControl(ByVal treeListControl As TreeListControl)
            If Me.treeListControl IsNot Nothing Then
                RemoveHandler Me.treeListControl.View.CustomNodeFilter, New TreeListNodeFilterEventHandler(AddressOf OnTreeListCustomNodeFilter)
                RemoveHandler Me.treeListControl.SortInfo.CollectionChanged, New NotifyCollectionChangedEventHandler(AddressOf OnSortInfoCollectionChanged)
                RemoveHandler Me.treeListControl.View.FocusedRowChanged, New FocusedRowChangedEventHandler(AddressOf OnTreeListControlFocusedRowChanged)
                Call TypeDescriptor.GetProperties(GetType(GridControl))(DataControlBase.FilterCriteriaProperty.Name).RemoveValueChanged(treeListControl, New EventHandler(AddressOf OnTreeListControlFilterCriteriaChanged))
            End If

            Me.treeListControl = treeListControl
            If treeListControl Is Nothing Then Return
            treeListControl.AutoPopulateColumns = AutoPopulateColumns
            If CollectionView Is Nothing Then Return
            treeListControl.ItemsSource = CollectionView.SourceCollection
            treeListControl.BeginInit()
            Try
                AddHandler treeListControl.View.CustomNodeFilter, New TreeListNodeFilterEventHandler(AddressOf OnTreeListCustomNodeFilter)
                AddHandler treeListControl.SortInfo.CollectionChanged, New NotifyCollectionChangedEventHandler(AddressOf OnSortInfoCollectionChanged)
                AddHandler treeListControl.View.FocusedRowChanged, New FocusedRowChangedEventHandler(AddressOf OnTreeListControlFocusedRowChanged)
                Call TypeDescriptor.GetProperties(GetType(GridControl))(DataControlBase.FilterCriteriaProperty.Name).AddValueChanged(treeListControl, New EventHandler(AddressOf OnTreeListControlFilterCriteriaChanged))
                SyncSorting()
                SyncFocusedRowHandle()
                SyncFilter()
                treeListControl.View.AllowSorting = collectionViewField.CanSort
            Finally
                treeListControl.EndInit()
            End Try
        End Sub

        Private Sub OnTreeListControlFilterCriteriaChanged(ByVal sender As Object, ByVal e As EventArgs)
            FilterCriteria = treeListControl.FilterCriteria
        End Sub

        Private Sub OnSortInfoCollectionChanged(ByVal sender As Object, ByVal e As NotifyCollectionChangedEventArgs)
            If syncGroupSortLocker.IsLocked Then Return
            syncGroupSortLocker.DoLockedAction(New Action(AddressOf SyncSortInfo))
        End Sub

        Private Sub SyncSortInfo()
            If CollectionView Is Nothing Then Return
            If CollectionView.SortDescriptions IsNot Nothing Then
                CollectionView.SortDescriptions.Clear()
                For i As Integer = 0 To treeListControl.SortInfo.Count - 1
                    Dim info As GridSortInfo = treeListControl.SortInfo(i)
                    CollectionView.SortDescriptions.Add(New SortDescription(info.FieldName, info.SortOrder))
                Next
            End If
        End Sub

        Private Sub SyncSorting()
            If syncGroupSortLocker.IsLocked Then Return
            syncGroupSortLocker.DoLockedAction(Sub()
                If CollectionView.SortDescriptions IsNot Nothing Then
                    treeListControl.SortInfo.BeginUpdate()
                    Try
                        treeListControl.ClearSorting()
                        For Each sortDescription As SortDescription In CollectionView.SortDescriptions
                            treeListControl.SortInfo.Add(New GridSortInfo() With {.FieldName = sortDescription.PropertyName, .SortOrder = sortDescription.Direction})
                        Next
                    Finally
                        treeListControl.SortInfo.EndUpdate()
                    End Try
                End If
            End Sub)
        End Sub

        Private Sub OnTreeListCustomNodeFilter(ByVal sender As Object, ByVal e As TreeListNodeFilterEventArgs)
            If CollectionView.Filter Is Nothing OrElse FilterMode = ModelFilterMode.FilterCriteria Then Return
            e.Visible = CollectionView.Filter(e.Node.Content)
            e.Handled = True
        End Sub
    End Class
End Namespace
