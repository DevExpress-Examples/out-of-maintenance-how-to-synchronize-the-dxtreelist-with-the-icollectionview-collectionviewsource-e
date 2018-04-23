Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation
Imports System.Windows.Shapes
Imports System.Threading
Imports System.Windows.Threading

Namespace TreeListControlViewModel
	''' <summary>
	''' Interaction logic for MainWindow.xaml
	''' </summary>
	Partial Public Class MainWindow
		Inherits Window
		Private view As ListCollectionView
		Public Sub New()
			InitializeComponent()
			Dim list As List(Of TestData) = TestData.CreateTestData()
			view = New ListCollectionView(list)
			DataContext = view
			AddHandler filterComboBox.SelectionChanged, AddressOf OnComboBoxSelectionChanged
		End Sub
		Private Sub OnComboBoxSelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs)
			Dispatcher.BeginInvoke(New ThreadStart(AddressOf UpdateFilter), DispatcherPriority.Background)
		End Sub
			Private Sub UpdateFilter()
            If filterComboBox.SelectedIndex = 1 Then

                view.Filter = AddressOf RootFilter

            Else
                view.Filter = Nothing
            End If
        End Sub
        Private Function RootFilter(ByVal obj As Object) As Boolean
            Dim testData As TestData = CType(obj, TestData)
            Return testData.ParentId = -1
        End Function
	End Class
	Public Class TestData
		Public Shared Function CreateTestData() As List(Of TestData)
			Dim list As New List(Of TestData)()
			list.Add(New TestData() With {.Id = 0, .ParentId = -1, .Text1 = "Item1", .Text2 = "Item1"})
			list.Add(New TestData() With {.Id = 1, .ParentId = 0, .Text1 = "Item2", .Text2 = "Item2"})
			list.Add(New TestData() With {.Id = 2, .ParentId = -1, .Text1 = "Item3", .Text2 = "Item3"})
			list.Add(New TestData() With {.Id = 3, .ParentId = 2, .Text1 = "Item4", .Text2 = "Item4"})
			list.Add(New TestData() With {.Id = 4, .ParentId = -1, .Text1 = "Item5", .Text2 = "Item5"})
			Return list
		End Function

		Private privateId As Integer
		Public Property Id() As Integer
			Get
				Return privateId
			End Get
			Set(ByVal value As Integer)
				privateId = value
			End Set
		End Property
		Private privateParentId As Integer
		Public Property ParentId() As Integer
			Get
				Return privateParentId
			End Get
			Set(ByVal value As Integer)
				privateParentId = value
			End Set
		End Property
		Private privateText1 As String
		Public Property Text1() As String
			Get
				Return privateText1
			End Get
			Set(ByVal value As String)
				privateText1 = value
			End Set
		End Property
		Private privateText2 As String
		Public Property Text2() As String
			Get
				Return privateText2
			End Get
			Set(ByVal value As String)
				privateText2 = value
			End Set
		End Property
	End Class
End Namespace
