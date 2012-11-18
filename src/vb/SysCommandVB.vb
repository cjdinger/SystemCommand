Imports SAS.Shared.AddIns
Imports SAS.Tasks.Toolkit
Imports SAS.Tasks.Toolkit.Helpers
Imports System.Windows.Forms
Imports System.IO
Imports System.Xml

<ClassId("f5917eb4-410a-43a5-8f6d-eb02033db083")> _
<IconLocation("SAS.Tasks.Examples.SysCommandVB.SysCommandVB.ico")> _
<InputRequired(InputResourceType.None)> _
<Version(4.2)> _
Public Class SysCommandVB : Inherits SAS.Tasks.Toolkit.SasTask
    Implements ISASTaskExecution

#Region "Initialization"
    Private cmds As String = "REM Add commands to execute"

    Sub New()
        InitializeComponent()
    End Sub

    Sub InitializeComponent()
        '
        'VBTask
        '
        Me.ProductsRequired = "BASE"
        Me.TaskCategory = "SAS Examples"
        Me.TaskDescription = "System Command (VB)"
        Me.TaskName = "System Command (VB)"

        Me.RequiresData = False
        Me.GeneratesReportOutput = False
        Me.GeneratesSasCode = False

    End Sub

#End Region

#Region "Save/restore state"
    Public Overrides Sub RestoreStateFromXml(ByVal xmlState As String, ByVal fromTemplate As Boolean)
        MyBase.RestoreStateFromXml(xmlState, fromTemplate)

        If (Not xmlState Is Nothing And xmlState.Length > 0) Then
            Try
                Dim sr As StringReader = New StringReader(xmlState)
                With sr
                    Dim reader As XmlTextReader = New XmlTextReader(sr)
                    reader.ReadStartElement("SysCommands")
                    cmds = reader.ReadElementString("Commands")
                    reader.ReadEndElement()
                    reader.Close()
                End With
            Catch

            End Try
        End If
    End Sub

    Public Overrides Function GetXmlState() As String
        Dim sw As StringWriter = New StringWriter()

        With sw
            Dim writer As XmlTextWriter = New XmlTextWriter(sw)
            writer.WriteStartElement("SysCommands")
            writer.WriteElementString("Commands", cmds)
            writer.WriteEndElement()
            writer.Close()
            Return sw.ToString()
        End With
    End Function


#End Region

#Region "Show the task form"

    ' This function is called when it's time to show the task window
    Public Overrides Function Show(ByVal Owner As IWin32Window) As SAS.Shared.AddIns.ShowResult
        Dim dlg As SysCommandVBForm = New SysCommandVBForm()
        dlg.Consumer = Me.Consumer

        ' populate the form with the saved commands, if any
        dlg.Cmds = cmds
        dlg.Text = Me.Label
        If (dlg.ShowDialog(Owner) = DialogResult.OK) Then
            ' get the updated commands
            cmds = dlg.Cmds
            Return ShowResult.RunNow
        Else
            Return ShowResult.Canceled
        End If

    End Function

#End Region

#Region "Execute commands in a batch file"
    ''' <summary>
    ''' This method writes out the system commands to 
    ''' a CMD file to run as a batch process.
    ''' It then defines and starts a process to
    ''' run the CMD file.
    ''' 
    ''' File operations and Process operations can
    ''' throw exceptions, so be sure to wrap the call
    ''' to this routine within a try/catch if 
    ''' you want to handle the errors.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ExecuteCommands() As String
        Dim stdout As String

        ' create a temp batch file to run the commands
        Dim fn As String = Path.GetTempFileName()
        File.Move(fn, fn & ".cmd")
        fn += ".cmd"

        Dim sw As StreamWriter = New StreamWriter(fn)
        sw.Write(cmds)
        sw.Dispose()

        ' launch a process to run the batch file
        ' record the stdout so we can report in the log for the task
        Dim psi As System.Diagnostics.ProcessStartInfo = _
          New System.Diagnostics.ProcessStartInfo()
        psi.FileName = fn
        psi.RedirectStandardOutput = True
        psi.UseShellExecute = False
        psi.CreateNoWindow = True


        Dim p As System.Diagnostics.Process = _
          System.Diagnostics.Process.Start(psi)
        stdout = p.StandardOutput.ReadToEnd()

        ' does not return control until the job is done
        p.WaitForExit()

        ' cleanup the temp file
        File.Delete(fn)

        ' return the stdout
        Return stdout

    End Function
#End Region

#Region "ISASTaskExecution Members"
    ''' <summary>
    ''' We do not support Cancel for this task, so return false.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Cancel() As Boolean Implements ISASTaskExecution.Cancel
        Return False
    End Function

    ''' <summary>
    ''' This task returns nothing but a log, so we don't
    ''' expect this method to be called
    ''' </summary>
    ''' <param name="Index"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function OpenResultStream(ByVal Index As Integer) As ISASTaskStream Implements ISASTaskExecution.OpenResultStream
        Throw New NotImplementedException()
    End Function

    ''' <summary>
    ''' No results returned except for the log.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ResultCount() As Integer Implements ISASTaskExecution.ResultCount
        Get
            Return 0
        End Get
    End Property

    ''' <summary>
    ''' Note: In the Run method it's a good idea to put *anything*
    ''' that might throw an exception inside a try-catch block.
    ''' And do not throw/rethrow any exceptions!
    ''' 
    ''' The host application might run this routine on a different
    ''' thread other than the main application thread, and 
    ''' it might not be able to handle an exception thrown
    ''' within here.
    ''' 
    ''' Best practice: catch any potential exceptions and then
    ''' write the appropriate information to the log using the 
    ''' SAS.Tasks.Toolkit.Helpers.FormattedLogWriter class.  If you
    ''' encounter an error, return RunStatus.Error.
    ''' </summary>
    ''' <param name="LogWriter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Run(ByVal LogWriter As ISASTaskTextWriter) _
      As RunStatus Implements ISASTaskExecution.Run
        Dim rc As RunStatus = RunStatus.Success

        ' to keep track of elapsed time for the system commands
        Dim start As DateTime = Now

        ' seed the machine name for use in the log
        ' we need to make it clear that the system commands
        ' are executed on the local machine, not on a
        ' remote SAS server machine.
        Dim machineName As String = "local machine"
        Try
            ' Environment.MachineName can throw an InvalidOperationException
            machineName = Environment.MachineName
        Catch ex As Exception
            ' couldn't get the machine name
        End Try

        ' the "FormattedLogWriter" helps color-code your log output
        ' for NOTE, ERROR, WARNING lines.
        FormattedLogWriter.WriteNoteLine(LogWriter, _
          String.Format("NOTE: Running system commands on {0}." & _
            vbNewLine & "Output:", machineName))
        Try
            Dim log As String = ExecuteCommands()

            ' write the output collected from stdout
            FormattedLogWriter.WriteNormalLine(LogWriter, log)
        Catch ex As Exception
            ' if there is an error, place it in the log
            FormattedLogWriter.WriteErrorLine(LogWriter, _
              String.Format("ERROR: Could not run commands " & _
               vbNewLine & "{0}", ex.Message))

            ' return error status so that it gets the "red X" treatment
            rc = RunStatus.Error
        End Try

        Dim elapsedTime As TimeSpan = TimeSpan.FromTicks(Now.Ticks - start.Ticks)

        FormattedLogWriter.WriteNoteLine(LogWriter, _
          String.Format("NOTE: System commands completed." & _
           vbNewLine & vbTab & "Real time: {0:F2} seconds", _
           elapsedTime.TotalSeconds))

        Return rc

    End Function
#End Region

End Class
