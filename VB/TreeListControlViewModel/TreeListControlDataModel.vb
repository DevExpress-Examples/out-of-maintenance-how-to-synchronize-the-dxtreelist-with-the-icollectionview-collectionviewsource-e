Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Data
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
		#Region "static"
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
			If model IsNot Nothing Then
				model.AttachToTreeListControl(treeListControl)
			End If
		End Sub
		Private Shared Sub OnCollectionViewChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
			Dim treeListControl As TreeListControl = CType(d, TreeListControl)
			Dim view As ICollectionView = CType(e.NewValue, ICollectionView)
			SetTreeListControlDataModel(treeListControl, New TreeListControlDataModel() With {.CollectionView = view})
		End Sub
		#End Region
		Private collectionView_Renamed As ICollectionView
		Private treeListControl As TreeListControl
		Private syncGroupSortLocker As New Locker()
		Private filterMode_Renamed As ModelFilterMode
		Private filterCriteria_Renamed As CriteriaOperator
		Public Sub New()
			AutoPopulateColumns = True
		End Sub
		Private privateAutoPopulateColumns As Boolean
		Public Property AutoPopulateColumns() As Boolean
			Get
				Return privateAutoPopulateColumns
			End Get
			Set(ByVal value As Boolean)
				privateAutoPopulateColumns = value
			End Set
		End Property
		Public Property FilterMode() As ModelFilterMode
			Get
				Return filterMode_Renamed
			End Get
			Set(ByVal value As ModelFilterMode)
				If filterMode_Renamed = value Then
					Return
				End If
				filterMode_Renamed = value
				SyncFilter()
				If treeListControl IsNot Nothing Then
					treeListControl.RefreshData()
				End If
			End Set
		End Property
		Public Property FilterCriteria() As CriteriaOperator
			Get
				Return filterCriteria_Renamed
			End Get
			Set(ByVal value As CriteriaOperator)
				If Object.ReferenceEquals(filterCriteria_Renamed, value) Then
					Return
				End If
				filterCriteria_Renamed = value
				SyncFilter()
			End Set
		End Property
		Public Property CollectionView() As ICollectionView
			Get
				Return collectionView_Renamed
			End Get
			Set(ByVal value As ICollectionView)
				If collectionView_Renamed Is value Then
					Return
				End If
				If collectionView_Renamed IsNot Nothing Then
					Dim notifyPropertyChanged As INotifyPropertyChanged = TryCast(collectionView_Renamed, INotifyPropertyChanged)
					If notifyPropertyChanged IsNot Nothing Then
						RemoveHandler notifyPropertyChanged.PropertyChanged, AddressOf OnCollectionViewPropertyChanged
					End If
					If collectionView_Renamed.SortDescriptions IsNot Nothing Then
						RemoveHandler (CType(collectionView_Renamed.SortDescriptions, INotifyCollectionChanged)).CollectionChanged, AddressOf OnSortDescriptionsCollectionChanged
					End If
					RemoveHandler collectionView_Renamed.CurrentChanged, AddressOf OnCollectionViewCurrentChanged
				End If
				collectionView_Renamed = value
				If collectionView_Renamed IsNot Nothing Then
					Dim notifyPropertyChanged As INotifyPropertyChanged = TryCast(collectionView_Renamed, INotifyPropertyChanged)
					If notifyPropertyChanged IsNot Nothing Then
						AddHandler notifyPropertyChanged.PropertyChanged, AddressOf OnCollectionViewPropertyChanged
					End If
					If collectionView_Renamed.SortDescriptions IsNot Nothing Then
						AddHandler (CType(collectionView_Renamed.SortDescriptions, INotifyCollectionChanged)).CollectionChanged, AddressOf OnSortDescriptionsCollectionChanged
					End If
					AddHandler collectionView_Renamed.CurrentChanged, AddressOf OnCollectionViewCurrentChanged
				End If
			End Set
		End Property

		Private Sub OnCollectionViewCurrentChanged(ByVal sender As Object, ByVal e As EventArgs)
			SyncFocusedRowHandle()
		End Sub
		Private Sub SyncFocusedRowHandle()
			If CanSyncCurrentRow() Then
				treeListControl.View.FocusedRow = collectionView_Renamed.CurrentItem
			End If
		End Sub
		Private Sub OnTreeListControlFocusedRowChanged(ByVal sender As Object, ByVal e As FocusedRowChangedEventArgs)
			If CanSyncCurrentRow() AndAlso treeListControl.View.FocusedRowHandle <> GridControl.InvalidRowHandle Then
				collectionView_Renamed.MoveCurrentTo(treeListControl.View.FocusedRow)
			End If
		End Sub
		Private Function CanSyncCurrentRow() As Boolean
			Return treeListControl IsNot Nothing AndAlso GetIsSynchronizedWithCurrentItem(treeListControl)
		End Function
		Private Sub OnSortDescriptionsCollectionChanged(ByVal sender As Object, ByVal e As NotifyCollectionChangedEventArgs)
			If treeListControl Is Nothing Then
				Return
			End If
			SyncSorting()
		End Sub
		Private Sub OnCollectionViewPropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs)
			If e.PropertyName = "Count" Then
				SyncFilter()
			End If
		End Sub
		Private Sub SyncFilter()
			If treeListControl Is Nothing Then
				Return
			End If
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
				RemoveHandler treeListControl.View.CustomNodeFilter, AddressOf OnTreeListCustomNodeFilter
				RemoveHandler treeListControl.SortInfo.CollectionChanged, AddressOf OnSortInfoCollectionChanged
				RemoveHandler treeListControl.View.FocusedRowChanged, AddressOf OnTreeListControlFocusedRowChanged
				TypeDescriptor.GetProperties(GetType(GridControl))(GridControl.FilterCriteriaProperty.Name).RemoveValueChanged(treeListControl, AddressOf OnTreeListControlFilterCriteriaChanged)
			End If
			Me.treeListControl = treeListControl
			If treeListControl Is Nothing Then
				Return
			End If
			treeListControl.AutoPopulateColumns = AutoPopulateColumns
			If CollectionView Is Nothing Then
				Return
			End If
			treeListControl.ItemsSource = CollectionView.SourceCollection
			treeListControl.BeginInit()
			Try
				AddHandler treeListControl.View.CustomNodeFilter, AddressOf OnTreeListCustomNodeFilter
				AddHandler treeListControl.SortInfo.CollectionChanged, AddressOf OnSortInfoCollectionChanged
				AddHandler treeListControl.View.FocusedRowChanged, AddressOf OnTreeListControlFocusedRowChanged
				TypeDescriptor.GetProperties(GetType(GridControl))(GridControl.FilterCriteriaProperty.Name).AddValueChanged(treeListControl, AddressOf OnTreeListControlFilterCriteriaChanged)
				SyncSorting()
				SyncFocusedRowHandle()
				SyncFilter()
				treeListControl.View.AllowSorting = collectionView_Renamed.CanSort
			Finally
				treeListControl.EndInit()
			End Try
		End Sub
		Private Sub OnTreeListControlFilterCriteriaChanged(ByVal sender As Object, ByVal e As EventArgs)
			FilterCriteria = treeListControl.FilterCriteria
		End Sub
		Private Sub OnSortInfoCollectionChanged(ByVal sender As Object, ByVal e As NotifyCollectionChangedEventArgs)
			If syncGroupSortLocker.IsLocked Then
				Return
			End If
			syncGroupSortLocker.DoLockedAction(AddressOf SyncSortInfo)
		End Sub
		Private Sub SyncSortInfo()
			If CollectionView Is Nothing Then
				Return
			End If
			If CollectionView.SortDescriptions IsNot Nothing Then
				CollectionView.SortDescriptions.Clear()
				For i As Integer = 0 To treeListControl.SortInfo.Count - 1
					Dim info As GridSortInfo = treeListControl.SortInfo(i)
					CollectionView.SortDescriptions.Add(New SortDescription(info.FieldName, info.SortOrder))
				Next i
			End If
		End Sub
		Private Sub SyncSorting()
			If syncGroupSortLocker.IsLocked Then
				Return
			End If
			syncGroupSortLocker.DoLockedAction(Function() AnonymousMethod1())
		End Sub
		
		Private Function AnonymousMethod1() As Boolean
			If CollectionView.SortDescriptions IsNot Nothing Then
				treeListControl.SortInfo.BeginUpdate()
				Try
					treeListControl.ClearSorting()
					For Each sortDescription As SortDescription In CollectionView.SortDescriptions
						treeListControl.SortInfo.Add(New GridSortInfo() With {.FieldName = sortDescription.PropertyName, .SortOrder = sortDescription.Direction})
					Next sortDescription
				Finally
					treeListControl.SortInfo.EndUpdate()
				End Try
			End If
			Return True
		End Function
		Private Sub OnTreeListCustomNodeFilter(ByVal sender As Object, ByVal e As TreeListNodeFilterEventArgs)
			If CollectionView.Filter Is Nothing OrElse FilterMode = ModelFilterMode.FilterCriteria Then
				Return
			End If
			e.Visible = CollectionView.Filter(e.Node.Content)
			e.Handled = True
		End Sub
	End Class


End Namespace

