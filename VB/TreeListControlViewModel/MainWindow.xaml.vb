Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Threading
Imports System.Windows.Threading

Namespace TreeListControlViewModel

    ''' <summary>
    ''' Interaction logic for MainWindow.xaml
    ''' </summary>
    Public Partial Class MainWindow
        Inherits Window

        Private view As ListCollectionView

        Public Sub New()
            Me.InitializeComponent()
            Dim list As List(Of TestData) = TestData.CreateTestData()
            view = New ListCollectionView(list)
            DataContext = view
            AddHandler Me.filterComboBox.SelectionChanged, AddressOf Me.OnComboBoxSelectionChanged
        End Sub

        Private Sub OnComboBoxSelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs)
            Dispatcher.BeginInvoke(New ThreadStart(AddressOf UpdateFilter), DispatcherPriority.Background)
        End Sub

        Private Sub UpdateFilter()
            If Me.filterComboBox.SelectedIndex = 1 Then
                view.Filter = Function(obj) CType(obj, TestData).ParentId = -1
            Else
                view.Filter = Nothing
            End If
        End Sub
    End Class

    Public Class TestData

        Public Shared Function CreateTestData() As List(Of TestData)
            Dim list As List(Of TestData) = New List(Of TestData)()
            list.Add(New TestData() With {.Id = 0, .ParentId = -1, .Text1 = "Item1", .Text2 = "Item1"})
            list.Add(New TestData() With {.Id = 1, .ParentId = 0, .Text1 = "Item2", .Text2 = "Item2"})
            list.Add(New TestData() With {.Id = 2, .ParentId = -1, .Text1 = "Item3", .Text2 = "Item3"})
            list.Add(New TestData() With {.Id = 3, .ParentId = 2, .Text1 = "Item4", .Text2 = "Item4"})
            list.Add(New TestData() With {.Id = 4, .ParentId = -1, .Text1 = "Item5", .Text2 = "Item5"})
            Return list
        End Function

        Public Property Id As Integer

        Public Property ParentId As Integer

        Public Property Text1 As String

        Public Property Text2 As String
    End Class
End Namespace
