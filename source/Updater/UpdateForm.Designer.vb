
Namespace DnsRedirector.Updater
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class UpdateForm
        Inherits System.Windows.Forms.Form

        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(UpdateForm))
            Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
            Me.Label1 = New System.Windows.Forms.Label()
            Me.SuspendLayout()
            '
            'ProgressBar1
            '
            Me.ProgressBar1.Location = New System.Drawing.Point(18, 58)
            Me.ProgressBar1.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
            Me.ProgressBar1.Name = "ProgressBar1"
            Me.ProgressBar1.Size = New System.Drawing.Size(759, 35)
            Me.ProgressBar1.TabIndex = 0
            '
            'Label1
            '
            Me.Label1.AutoSize = True
            Me.Label1.Location = New System.Drawing.Point(20, 29)
            Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
            Me.Label1.Name = "Label1"
            Me.Label1.Size = New System.Drawing.Size(86, 20)
            Me.Label1.TabIndex = 1
            Me.Label1.Text = "Updating..."
            '
            'UpdateForm
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(795, 115)
            Me.Controls.Add(Me.Label1)
            Me.Controls.Add(Me.ProgressBar1)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
            Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
            Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "UpdateForm"
            Me.Text = "DNS Redirector OSS Updater"
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
        Friend WithEvents Label1 As System.Windows.Forms.Label
    End Class
End Namespace
