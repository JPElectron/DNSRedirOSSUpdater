Imports System.IO
Imports System.Threading

Namespace DnsRedirector.Updater
    Partial Public Class SettingsForm

        Private _Settings As UpdaterSettings
        Private _Log As StreamWriter

        Public ReadOnly Property Settings() As UpdaterSettings
            Get
                Return _Settings
            End Get
        End Property

        Public Sub New(ByVal log As StreamWriter, Optional threadPriority As ThreadPriority = ThreadPriority.BelowNormal)
            Me.InitializeComponent()
            Me.ComboBox1.SelectedIndex = 0
            _Log = log
            _Settings = New UpdaterSettings(AppDomain.CurrentDomain.BaseDirectory & "updater.ini", Me, threadPriority)

            If Not _Settings.CanForced Then
                TabsSettings.TabPages.Remove(TabNxdForce)
            End If

            If Not _Settings.CanAllowed Then
                TabsSettings.TabPages.Remove(TabAllowed)
            End If
        End Sub

        Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
            _Log.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " Pressed OK, saving updater.ini")
            _Settings.SaveSettingsIni(Me)
            Me.Close()
        End Sub

        Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
            _Log.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " Pressed Cancel")
            Me.Close()
        End Sub

    End Class
End Namespace
