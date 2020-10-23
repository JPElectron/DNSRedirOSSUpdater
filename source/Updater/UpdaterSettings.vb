Imports System.IO
Imports System.Collections.ObjectModel
Imports System.Threading

Namespace DnsRedirector.Updater

    Public Class UpdaterSettings


        Public Const UpdaterVersionCheckPath As String = "updater/verchk.txt"

        Private _ThreadPriority As ThreadPriority = ThreadPriority.BelowNormal

        Private _CanBlocked As Boolean
        Private _CanForced As Boolean
        Private _CanAllowed As Boolean

        Private _SettingsIniPath As String
        Private _SettingsIniExists As Boolean

        Private _DownloadUrl As String
        'This is no longer optional
        'Private _ConsolidateKeywords As Boolean

        Private _BlockedKeywordLists As New List(Of String)
        Private _BlockedStaticKeywords As New List(Of String)
        Private _BlockedCustomKeywordUrls As New List(Of CustomKeywordListSetting)
        Private _BlockedCutsomKeywordPaths As New List(Of CustomKeywordListSetting)

        Private _ForcedKeywordLists As New List(Of String)
        Private _ForcedCustomKeywordUrls As New List(Of CustomKeywordListSetting)
        Private _ForcedCutsomKeywordPaths As New List(Of CustomKeywordListSetting)

        Private _AllowedCustomKeywordUrls As New List(Of CustomKeywordListSetting)
        Private _AllowedCutsomKeywordPaths As New List(Of CustomKeywordListSetting)

        Private _Actions As New List(Of String)

        Private _SettingSeperator() As Char = {"="c}

        Public ReadOnly Property ThreadPriority() As ThreadPriority
            Get
                Return _ThreadPriority
            End Get
        End Property

        Public ReadOnly Property CanBlocked() As Boolean
            Get
                Return _CanBlocked
            End Get
        End Property

        Public ReadOnly Property CanForced() As Boolean
            Get
                Return _CanForced
            End Get
        End Property

        Public ReadOnly Property CanAllowed() As Boolean
            Get
                Return _CanAllowed
            End Get
        End Property

        Public ReadOnly Property DownloadUrl() As String
            Get
                Return _DownloadUrl
            End Get
        End Property

        Public ReadOnly Property ConsolidateKeywords() As Boolean
            Get
                Return True
                'This is no longer optional
                'Return _ConsolidateKeywords
            End Get
        End Property

        Public ReadOnly Property SettingsIniExists() As Boolean
            Get
                Return _SettingsIniExists
            End Get
        End Property

        Public ReadOnly Property BlockedKeywordLists() As ReadOnlyCollection(Of String)
            Get
                Return _BlockedKeywordLists.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property BlockedStaticKeywords() As ReadOnlyCollection(Of String)
            Get
                Return _BlockedStaticKeywords.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property BlockedCustomKeywordListUrls() As ReadOnlyCollection(Of CustomKeywordListSetting)
            Get
                Return _BlockedCustomKeywordUrls.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property CustomKeywordListPaths() As ReadOnlyCollection(Of CustomKeywordListSetting)
            Get
                Return _BlockedCutsomKeywordPaths.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property ForcedKeywordLists() As ReadOnlyCollection(Of String)
            Get
                Return _ForcedKeywordLists.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property ForcedCustomKeywordListUrls() As ReadOnlyCollection(Of CustomKeywordListSetting)
            Get
                Return _ForcedCustomKeywordUrls.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property ForcedKeywordListPaths() As ReadOnlyCollection(Of CustomKeywordListSetting)
            Get
                Return _ForcedCutsomKeywordPaths.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property AllowedCustomKeywordListUrls() As ReadOnlyCollection(Of CustomKeywordListSetting)
            Get
                Return _AllowedCustomKeywordUrls.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property AllowedKeywordListPaths() As ReadOnlyCollection(Of CustomKeywordListSetting)
            Get
                Return _AllowedCutsomKeywordPaths.AsReadOnly
            End Get
        End Property

        Public ReadOnly Property Actions() As ReadOnlyCollection(Of String)
            Get
                Return _Actions.AsReadOnly
            End Get
        End Property

        ''' <summary>
        ''' Loads the settings ini parameters
        ''' </summary>
        ''' <param name="settingsIniPath"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal settingsIniPath As String, ByVal settingsForm As SettingsForm, Optional threadPriority As ThreadPriority = ThreadPriority.BelowNormal)
            _ThreadPriority = threadPriority
            _CanBlocked = True
            _CanForced = True
            _CanAllowed = True
            _SettingsIniPath = settingsIniPath
            ReadSettingsIni(settingsForm)
        End Sub


        ''' <summary>
        ''' Parses the updater ini file
        ''' </summary>
        ''' <remarks></remarks>
        Protected Friend Sub ReadSettingsIni(ByVal settingsForm As SettingsForm)
            'Check if the file exists
            If Not File.Exists(_SettingsIniPath) Then
                _SettingsIniExists = False
                Exit Sub
            Else
                _SettingsIniExists = True
            End If

            Dim BlockedOnCustomKeywordLists As New List(Of String)
            Dim BlockedVerifyCustomKeywordLists As New List(Of String)

            Dim ForcedOnCustomKeywordLists As New List(Of String)
            Dim ForcedVerifyCustomKeywordLists As New List(Of String)

            Dim AllowedOnCustomKeywordLists As New List(Of String)
            Dim AllowedVerifyCustomKeywordLists As New List(Of String)

            Using SettingsFile As StreamReader = File.OpenText(_SettingsIniPath)
                While Not SettingsFile.EndOfStream

                    Dim Setting() As String = SettingsFile.ReadLine.Split(_SettingSeperator)
                    If Setting.Length = 2 Then
                        'For backwards compatibility
                        If Setting(0).Equals("Action", StringComparison.OrdinalIgnoreCase) AndAlso Setting(1).Equals("6", StringComparison.OrdinalIgnoreCase) Then
                            Setting(0) = "updaterdone.bat"
                            Setting(1) = "On"
                        End If

                        'Get the control for the setting
                        Dim SettingControl As Control = FindControlByTag(Setting(0), settingsForm)
                        If SettingControl IsNot Nothing Then

                            Select Case SettingControl.Tag
                                'Handle individual controls
                                Case "URL"
                                    Dim SelectedUrlIndex As Integer
                                    Dim CmbUrl = TryCast(SettingControl, ComboBox)
                                    If CmbUrl IsNot Nothing AndAlso Integer.TryParse(Setting(1), SelectedUrlIndex) Then
                                        _DownloadUrl = CmbUrl.SelectedItem
                                    End If
                                'This is no longer optional, so reading it is ignored
                                'Case "Consolidate"
                                '    Dim ConsolKeywordsCheckbox As CheckBox = TryCast(SettingControl, CheckBox)
                                '    If ConsolKeywordsCheckbox IsNot Nothing Then
                                '        ConsolKeywordsCheckbox.Checked = Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase)
                                '        _ConsolidateKeywords = ConsolKeywordsCheckbox.Checked
                                '    End If
                                Case "Action"
                                    If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                        _Actions.Add(SettingControl.Tag)
                                        DirectCast(SettingControl, CheckBox).Checked = True
                                    End If
                                Case Else
                                    'Handle groups of controls

                                    Select Case SettingControl.Parent.Name
                                        'Blocked
                                        Case "panKeywordsLists" 'keyword list
                                            'It is, we are only concerned with On keyword lists
                                            If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                _BlockedKeywordLists.Add(SettingControl.Tag)
                                                DirectCast(SettingControl, CheckBox).Checked = True
                                            End If

                                        Case "panKeywords" ' additional keyword
                                            'It is, we are only concerned with on keywords
                                            If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                _BlockedStaticKeywords.Add(SettingControl.Tag)
                                                DirectCast(SettingControl, CheckBox).Checked = True
                                            End If

                                        Case "panCustomFiles"
                                            'Is this for the checkbox or associated textbox
                                            If Setting(0).EndsWith("Path", StringComparison.OrdinalIgnoreCase) Then
                                                'Textbox
                                                DirectCast(SettingControl, TextBox).Text = Setting(1)
                                            ElseIf Not Setting(0).EndsWith("Verify") Then
                                                'Checkbox
                                                If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                    DirectCast(SettingControl, CheckBox).Checked = True
                                                    BlockedOnCustomKeywordLists.Add(SettingControl.Tag)
                                                End If
                                            Else
                                                'Checkbox, verify
                                                If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                    DirectCast(SettingControl, CheckBox).Checked = True
                                                    BlockedVerifyCustomKeywordLists.Add(SettingControl.Tag)
                                                End If
                                            End If

                                        Case "panActions"
                                            If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                _Actions.Add(SettingControl.Tag)
                                                DirectCast(SettingControl, CheckBox).Checked = True
                                            End If

                                        'Forced
                                        Case "panForceKeywordLists"
                                            If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                _ForcedKeywordLists.Add(SettingControl.Tag)
                                                DirectCast(SettingControl, CheckBox).Checked = True
                                            End If

                                        Case "panForceCustomFiles"
                                            If Setting(0).EndsWith("Path", StringComparison.OrdinalIgnoreCase) Then
                                                'Textbox
                                                DirectCast(SettingControl, TextBox).Text = Setting(1)
                                            ElseIf Not Setting(0).EndsWith("Verify") Then
                                                'Checkbox
                                                If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                    DirectCast(SettingControl, CheckBox).Checked = True
                                                    ForcedOnCustomKeywordLists.Add(SettingControl.Tag)
                                                End If
                                            Else
                                                'Checkbox, verify
                                                If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                    DirectCast(SettingControl, CheckBox).Checked = True
                                                    ForcedVerifyCustomKeywordLists.Add(SettingControl.Tag)
                                                End If
                                            End If

                                       'Allowed
                                        Case "panAllowedCustomFiles"
                                            If Setting(0).EndsWith("Path", StringComparison.OrdinalIgnoreCase) Then
                                                'Textbox
                                                DirectCast(SettingControl, TextBox).Text = Setting(1)
                                            ElseIf Not Setting(0).EndsWith("Verify") Then
                                                'Checkbox
                                                If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                    DirectCast(SettingControl, CheckBox).Checked = True
                                                    AllowedOnCustomKeywordLists.Add(SettingControl.Tag)
                                                End If
                                            Else
                                                'Checkbox, verify
                                                If Setting(1).Equals("On", StringComparison.OrdinalIgnoreCase) Then
                                                    DirectCast(SettingControl, CheckBox).Checked = True
                                                    AllowedVerifyCustomKeywordLists.Add(SettingControl.Tag)
                                                End If
                                            End If

                                    End Select
                            End Select
                        End If
                    End If


                End While
            End Using


            'Now that the file has been parsed associate the on keyword lists with the path
            'This way they can be out of order and it won't matter since we have parsed the entire file
            'For Each OnCheckBoxTag As String In BlockedOnCustomKeywordLists
            '    Dim FoundControl As Control = FindControlByTag(OnCheckBoxTag & "Path", settingsForm.panCustomFiles)
            '    If FoundControl IsNot Nothing Then
            '        Dim CustKeywordPathBox As TextBox = TryCast(FoundControl, TextBox)
            '        If CustKeywordPathBox IsNot Nothing Then
            '            'We found the control
            '            'Is the value a URL
            '            Dim CustomUri As Uri = Nothing
            '            If Uri.TryCreate(CustKeywordPathBox.Text, UriKind.Absolute, CustomUri) Then
            '                If Not CustomUri.IsFile Then
            '                    _BlockedCustomKeywordUrls.Add(CustomUri.ToString)
            '                Else
            '                    _BlockedCutsomKeywordPaths.Add(CustomUri.PathAndQuery)
            '                End If
            '            Else
            '                If Not String.IsNullOrEmpty(CustKeywordPathBox.Text) Then
            '                    'Assume it is a path
            '                    _BlockedCutsomKeywordPaths.Add(CustKeywordPathBox.Text)
            '                End If
            '            End If
            '        End If
            '    End If
            'Next


            ReadCustomKeywordListSettings(BlockedOnCustomKeywordLists, settingsForm.panCustomFiles, _BlockedCustomKeywordUrls, _BlockedCutsomKeywordPaths)
            ReadCustomKeywordListSettings(ForcedOnCustomKeywordLists, settingsForm.panForceCustomFiles, _ForcedCustomKeywordUrls, _ForcedCutsomKeywordPaths)
            ReadCustomKeywordListSettings(AllowedOnCustomKeywordLists, settingsForm.panAllowedCustomFiles, _AllowedCustomKeywordUrls, _AllowedCutsomKeywordPaths)

        End Sub

        Private Sub ReadCustomKeywordListSettings(tagNames As IEnumerable(Of String), parentControl As Control, customUrlSettingsList As IList(Of CustomKeywordListSetting), customPathSettingsList As IList(Of CustomKeywordListSetting))
            'this only populates lists set to 'On' in settings
            For Each OnCheckBoxTag As String In tagNames
                Dim keywordListSetting = New CustomKeywordListSetting()
                keywordListSetting.Tag = OnCheckBoxTag
                keywordListSetting.KeywordStatus = KeywordStatus.On

                Dim IsUrl = False

                Dim PathControl As Control = FindControlByTag(OnCheckBoxTag & "Path", parentControl)
                If PathControl IsNot Nothing Then
                    Dim CustKeywordPathBox As TextBox = TryCast(PathControl, TextBox)
                    If CustKeywordPathBox IsNot Nothing Then
                        'We found the control
                        'Is the value a URL
                        Dim CustomUri As Uri = Nothing
                        If Uri.TryCreate(CustKeywordPathBox.Text, UriKind.Absolute, CustomUri) Then
                            If Not CustomUri.IsFile Then
                                IsUrl = True
                                keywordListSetting.Location = CustomUri.ToString
                            Else
                                keywordListSetting.Location = CustomUri.PathAndQuery
                            End If
                        Else
                            keywordListSetting.Location = CustKeywordPathBox.Text
                        End If
                    End If
                End If

                Dim VerifyControl As CheckBox = FindControlByTag(OnCheckBoxTag & "Verify", parentControl)
                If VerifyControl IsNot Nothing Then
                    keywordListSetting.Verify = VerifyControl.Checked
                End If

                If IsUrl Then
                    customUrlSettingsList.Add(keywordListSetting)
                Else
                    customPathSettingsList.Add(keywordListSetting)
                End If
            Next
        End Sub


        Public Function SaveSettingsIni(ByVal SettingsForm As SettingsForm) As Boolean

            Using SettingsFile As FileStream = File.Open(_SettingsIniPath, IIf(_SettingsIniExists, FileMode.Truncate, FileMode.CreateNew), FileAccess.ReadWrite, FileShare.None)
                Using SettingsWriter As New StreamWriter(SettingsFile)

                    'Blocked
                    For Each KeywordListControl As Control In SettingsForm.panKeywordsLists.Controls
                        If TypeOf (KeywordListControl) Is CheckBox Then
                            Dim KeywordCheckbox As CheckBox = DirectCast(KeywordListControl, CheckBox)
                            If Not String.IsNullOrEmpty(KeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(KeywordCheckbox.Tag & "=" & IIf(KeywordCheckbox.Checked, "On", "Off"))
                            End If
                        End If
                    Next

                    For Each KeywordControl As Control In SettingsForm.panKeywordsLists.Controls("panKeywords").Controls
                        If TypeOf (KeywordControl) Is CheckBox Then
                            Dim KeywordCheckbox As CheckBox = DirectCast(KeywordControl, CheckBox)
                            If Not String.IsNullOrEmpty(KeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(KeywordCheckbox.Tag & "=" & IIf(KeywordCheckbox.Checked, "On", "Off"))
                            End If
                        End If
                    Next

                    For Each CustomKeywordControl As Control In SettingsForm.panCustomFiles.Controls
                        If TypeOf (CustomKeywordControl) Is CheckBox Then
                            Dim CustomKeywordCheckbox As CheckBox = DirectCast(CustomKeywordControl, CheckBox)
                            If Not String.IsNullOrEmpty(CustomKeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(CustomKeywordCheckbox.Tag & "=" & IIf(CustomKeywordCheckbox.Checked, "On", "Off"))
                            End If
                        ElseIf TypeOf (CustomKeywordControl) Is TextBox Then
                            Dim CustomKeywordTextBox As TextBox = DirectCast(CustomKeywordControl, TextBox)
                            If Not String.IsNullOrEmpty(CustomKeywordTextBox.Tag) Then
                                SettingsWriter.WriteLine(CustomKeywordTextBox.Tag & "=" & CustomKeywordTextBox.Text)
                            End If
                        End If
                    Next

                    'Forced
                    For Each KeywordListControl As Control In SettingsForm.panForceKeywordLists.Controls
                        If TypeOf (KeywordListControl) Is CheckBox Then
                            Dim KeywordCheckbox As CheckBox = DirectCast(KeywordListControl, CheckBox)
                            If Not String.IsNullOrEmpty(KeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(KeywordCheckbox.Tag & "=" & IIf(KeywordCheckbox.Checked, "On", "Off"))
                            End If
                        End If
                    Next

                    For Each CustomKeywordControl As Control In SettingsForm.panForceCustomFiles.Controls
                        If TypeOf (CustomKeywordControl) Is CheckBox Then
                            Dim CustomKeywordCheckbox As CheckBox = DirectCast(CustomKeywordControl, CheckBox)
                            If Not String.IsNullOrEmpty(CustomKeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(CustomKeywordCheckbox.Tag & "=" & IIf(CustomKeywordCheckbox.Checked, "On", "Off"))
                            End If
                        ElseIf TypeOf (CustomKeywordControl) Is TextBox Then
                            Dim CustomKeywordTextBox As TextBox = DirectCast(CustomKeywordControl, TextBox)
                            If Not String.IsNullOrEmpty(CustomKeywordTextBox.Tag) Then
                                SettingsWriter.WriteLine(CustomKeywordTextBox.Tag & "=" & CustomKeywordTextBox.Text)
                            End If
                        End If
                    Next

                    'Allowed
                    For Each CustomKeywordControl As Control In SettingsForm.panAllowedCustomFiles.Controls
                        If TypeOf (CustomKeywordControl) Is CheckBox Then
                            Dim CustomKeywordCheckbox As CheckBox = DirectCast(CustomKeywordControl, CheckBox)
                            If Not String.IsNullOrEmpty(CustomKeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(CustomKeywordCheckbox.Tag & "=" & IIf(CustomKeywordCheckbox.Checked, "On", "Off"))
                            End If
                        ElseIf TypeOf (CustomKeywordControl) Is TextBox Then
                            Dim CustomKeywordTextBox As TextBox = DirectCast(CustomKeywordControl, TextBox)
                            If Not String.IsNullOrEmpty(CustomKeywordTextBox.Tag) Then
                                SettingsWriter.WriteLine(CustomKeywordTextBox.Tag & "=" & CustomKeywordTextBox.Text)
                            End If
                        End If
                    Next

                    Dim ComboUrl As ComboBox = TryCast(FindControlByTag("URL", SettingsForm), ComboBox)
                    If ComboUrl IsNot Nothing Then
                        SettingsWriter.WriteLine("URL=" & ComboUrl.SelectedIndex + 1) 'Add 1 for compatibility with older ini files
                    End If

                    'This is no longer optional
                    'Dim ConsolKeywordsCheckbox As CheckBox = TryCast(FindControlByTag("Consolidate", SettingsForm), CheckBox)
                    'If ConsolKeywordsCheckbox IsNot Nothing Then
                    '    SettingsWriter.WriteLine("Consolidate=" & IIf(ConsolKeywordsCheckbox.Checked, "On", "Off"))
                    'End If

                    For Each ActionKeywordControl As Control In SettingsForm.panActions.Controls
                        If TypeOf (ActionKeywordControl) Is CheckBox Then
                            Dim ActionKeywordCheckbox As CheckBox = DirectCast(ActionKeywordControl, CheckBox)
                            If ActionKeywordCheckbox.Checked AndAlso Not String.IsNullOrEmpty(ActionKeywordCheckbox.Tag) Then
                                SettingsWriter.WriteLine(ActionKeywordCheckbox.Tag & "=" & IIf(ActionKeywordCheckbox.Checked, "On", "Off"))
                            End If
                        End If
                    Next


                End Using 'Closes the writer
            End Using 'Closes the file stream
        End Function


        ''' <summary>
        ''' Given a tag name returns the first control with that tag or Nothing if the control is not found
        ''' </summary>
        ''' <param name="Tag"></param>
        ''' <param name="CurrentControl"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function FindControlByTag(ByVal Tag As String, ByVal CurrentControl As Control) As Control
            For Each ctr As Control In CurrentControl.Controls
                If ctr.Tag = Tag Then
                    Return ctr
                Else
                    ctr = FindControlByTag(Tag, ctr)
                    If Not ctr Is Nothing Then
                        Return ctr
                    End If
                End If
            Next ctr

            Return Nothing
        End Function



    End Class


    Public Class CustomKeywordListSetting
        Public Tag As String
        Public KeywordStatus As KeywordStatus
        Public Verify As Boolean
        Public Location As String

    End Class


    Public Enum KeywordStatus
        Off = 0
        [On] = 1
    End Enum
End Namespace

