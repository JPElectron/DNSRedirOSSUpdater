Imports System.IO
Imports System.Threading

Namespace DnsRedirector.Updater


    Public Class UpdaterApp
        ''' <summary>
        ''' This method is the main entry point when the application starts
        ''' </summary>
        <STAThread()>
        Shared Sub Main(ByVal args As String())

            Dim Log As FileStream
            Dim LogWriter As StreamWriter

            'Init the log file to write to
            Log = File.Open("updaterlog.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)
            LogWriter = New StreamWriter(Log)
            Log.Seek(Log.Length, SeekOrigin.Begin)

            Dim Setup As Boolean = False
            Dim ThreadPriority As ThreadPriority = ThreadPriority.BelowNormal

            For Each Arg As String In args
                If Arg.Equals("/setup", StringComparison.OrdinalIgnoreCase) Then
                    Setup = True
                ElseIf Arg.Equals("/Lowest", StringComparison.OrdinalIgnoreCase) Then
                    ThreadPriority = ThreadPriority.Lowest
                ElseIf Arg.Equals("/BelowNormal", StringComparison.OrdinalIgnoreCase) Then
                    ThreadPriority = ThreadPriority.BelowNormal
                ElseIf Arg.Equals("/Normal", StringComparison.OrdinalIgnoreCase) Then
                    ThreadPriority = ThreadPriority.Normal
                ElseIf Arg.Equals("/AboveNormal", StringComparison.OrdinalIgnoreCase) Then
                    ThreadPriority = ThreadPriority.AboveNormal
                ElseIf Arg.Equals("/Highest", StringComparison.OrdinalIgnoreCase) Then
                    ThreadPriority = ThreadPriority.Highest
                End If
            Next

            Dim SettingsForm As New SettingsForm(LogWriter, ThreadPriority)

            'If the setup file does not exist set setup to true
            If Not Setup Then Setup = Not SettingsForm.Settings.SettingsIniExists

            LogWriter.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " ")
            LogWriter.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " " & "**********")

            If Setup Then
                LogWriter.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " " & "Updater v" & My.Application.Info.Version.Major.ToString & "." & My.Application.Info.Version.Minor.ToString & "." & My.Application.Info.Version.Revision.ToString & " setup")
                If File.Exists(UpdateForm._CacheWorkingPath & UpdateForm._DatFileName) Then
                    File.Delete(UpdateForm._CacheWorkingPath & UpdateForm._DatFileName)
                End If
                Application.Run(SettingsForm)
                Else
                    Dim UpdateForm As New UpdateForm(SettingsForm.Settings, LogWriter)
                Application.Run(UpdateForm)
            End If

            LogWriter.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " " & "Exiting")

            'Close the log
            LogWriter.Close()
        End Sub
    End Class
End Namespace
