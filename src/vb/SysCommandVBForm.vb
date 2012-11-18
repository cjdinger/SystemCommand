Imports SAS.Tasks.Toolkit

Public Class SysCommandVBForm : Inherits SAS.Tasks.Toolkit.Controls.TaskForm

    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Public Property Cmds() As String
        Get
            Return txtCommands.Text
        End Get
        Set(ByVal value As String)
            txtCommands.Text = value
        End Set
    End Property


End Class