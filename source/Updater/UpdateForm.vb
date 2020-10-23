Imports System
Imports System.Net
Imports System.Security.Cryptography.X509Certificates
Imports System.IO
Imports System.IO.Compression
Imports System.ComponentModel
Imports System.Threading
Imports System.Text.RegularExpressions
Imports System.Reflection

Namespace DnsRedirector.Updater


    Public Class UpdateForm

        Private Shared _WorkingPath As String = AppDomain.CurrentDomain.BaseDirectory
        Public Shared _CacheWorkingPath As String = _WorkingPath & "UpdaterCache" & Path.DirectorySeparatorChar
        Private _Settings As UpdaterSettings
        Private Const _DownloadRetries As Integer = 3
        Public Const _DatFileName As String = "updater.dat"
        Private Shared _DatDelimiter() As Char = {ControlChars.Tab}
        Private _CsvDelimiter() As Char = {","c}
        Private Shared _DatFileLines As New List(Of String())
        Private Shared _AllDownloadedKeywordsFiles As New List(Of KeywordFileState)
        Private _LogWriter As StreamWriter

        Public Function AcceptAllCertifications(ByVal sender As Object, ByVal certification As System.Security.Cryptography.X509Certificates.X509Certificate, ByVal chain As System.Security.Cryptography.X509Certificates.X509Chain, ByVal sslPolicyErrors As System.Net.Security.SslPolicyErrors) As Boolean
            Return True
        End Function

        Public Sub New(ByVal settings As UpdaterSettings, ByVal log As StreamWriter)
            InitializeComponent()
            _Settings = settings
            _LogWriter = log

            'Create the cache directory if it doesn't exist
            If Not Directory.Exists(_CacheWorkingPath) Then Directory.CreateDirectory(_CacheWorkingPath)

        End Sub

        Private Sub UpdateForm_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
            'Make sure the form controls are drawn
            Me.Refresh()

            'This is running on the GUI thread, we want to run the actual update processing on another thread so the GUI can be updated
            Dim Worker As New BackgroundWorker
            AddHandler Worker.DoWork, AddressOf DoUpdating
            AddHandler Worker.RunWorkerCompleted, AddressOf UpdatingComeplete
            Worker.RunWorkerAsync()

        End Sub

        Private Sub UpdatingComeplete(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)
            'Pause the app for a second
            Me.Refresh() 'Allow the UI to catchup and draw itself
            Thread.Sleep(1000)
            Me.Close()
        End Sub

        ''' <summary>
        ''' Performs the update work
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub DoUpdating(ByVal sender As Object, ByVal e As DoWorkEventArgs)

            Thread.CurrentThread.Priority = _Settings.ThreadPriority

            LogIt("Updater v" & My.Application.Info.Version.Major.ToString & "." & My.Application.Info.Version.Minor.ToString & "." & My.Application.Info.Version.Revision.ToString & " launched")

            'Check the app version
            Dim WebClient As New WebClient
            ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptAllCertifications
            WebClient.Headers("CacheControl") = "no-cache"
            WebClient.Headers("User-Agent") = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:80.0) Gecko/20100101 Firefox/80.0"
            Dim LatestUpdaterVersion As String = WebClient.DownloadString(_Settings.DownloadUrl & UpdaterSettings.UpdaterVersionCheckPath)
            If String.IsNullOrEmpty(LatestUpdaterVersion) Then
                LogIt("Error, could not retrieve latest updater version")
            Else
                If Not LatestUpdaterVersion.Equals(My.Application.Info.Version.Major.ToString & "." & My.Application.Info.Version.Minor.ToString & "." & My.Application.Info.Version.Revision.ToString, StringComparison.OrdinalIgnoreCase) Then
                    LogIt("UPDATER IS OUT OF DATE")
                    LogIt("Get the latest version from https://github.com/JPElectron/DNSRedirOSSUpdater")
                End If
            End If

            _AllDownloadedKeywordsFiles = New List(Of KeywordFileState)()
            If File.Exists(_CacheWorkingPath & _DatFileName) Then
                'Read the current dat file
                Using DatFile As StreamReader = File.OpenText(_CacheWorkingPath & _DatFileName)
                    While Not DatFile.EndOfStream
                        _DatFileLines.Add(DatFile.ReadLine.Split(_DatDelimiter))
                    End While
                End Using
            End If

            Dim allowedUpdated = False
            If _Settings.CanAllowed Then
                LogIt("Updating Allowed")
                allowedUpdated = DownloadAndProcessKeywords("allowed.txt", New List(Of String)(), _Settings.AllowedCustomKeywordListUrls, _Settings.AllowedKeywordListPaths, New List(Of String)())
                If (allowedUpdated) Then
                    'Thread.Sleep(8000)
                End If
            End If

            Dim forcedUpdated = False
            If _Settings.CanForced Then
                LogIt("Updating NXDForce")
                forcedUpdated = DownloadAndProcessKeywords("nxdforce.txt", _Settings.ForcedKeywordLists, _Settings.ForcedCustomKeywordListUrls, _Settings.ForcedKeywordListPaths, New List(Of String)())
                If (forcedUpdated) Then
                    'Thread.Sleep(8000)
                End If
            End If

            LogIt("Updating Blocked")
            Dim blockedUpdated = DownloadAndProcessKeywords("blocked.txt", _Settings.BlockedKeywordLists, _Settings.BlockedCustomKeywordListUrls, _Settings.CustomKeywordListPaths, _Settings.BlockedStaticKeywords)
            If allowedUpdated OrElse forcedUpdated OrElse blockedUpdated Then
                LogIt("Update complete")

                For Each Action As String In _Settings.Actions
                    LogIt("Starting " & Action)
                    Dim Start As New ProcessStartInfo
                    Start.UseShellExecute = True
                    Start.ErrorDialog = False
                    Start.FileName = Action
                    Try
                        Process.Start(Start)
                    Catch ex As Exception
                        LogIt("Error while starting " & Action & ": " & ex.Message)
                    End Try
                Next
            End If

            Using DatFile As FileStream = File.Open(_CacheWorkingPath & _DatFileName, FileMode.Create, FileAccess.Write, FileShare.None)
                Using DatWriter As New StreamWriter(DatFile)
                    'Create the dat file with the dates of all the files

                    'Files downloaded this update session
                    For Each Download As KeywordFileState In _AllDownloadedKeywordsFiles
                        DatWriter.WriteLine(Download.FileName & _DatDelimiter(0) & Download.CachedFileName & _DatDelimiter(0) & Download.LastModified.ToBinary.ToString)
                    Next

                    'Files from last update session that have not been updated this session
                    For Each DatLine() As String In _DatFileLines
                        Dim WasUpdated As Boolean = False
                        For Each Downlaod As KeywordFileState In _AllDownloadedKeywordsFiles
                            If Downlaod.FileName.Equals(DatLine(0), StringComparison.OrdinalIgnoreCase) Then
                                WasUpdated = True
                                Exit For
                            End If
                        Next

                        If Not WasUpdated Then
                            DatWriter.WriteLine(String.Join(_DatDelimiter(0), DatLine))
                        End If

                    Next
                End Using
            End Using

        End Sub

        Private Function DownloadAndProcessKeywords(outputFileName As String, keywordLists As IList(Of String), customKeywordListUrls As IList(Of CustomKeywordListSetting), customKeywordListPaths As IEnumerable(Of CustomKeywordListSetting), staticKeywords As IEnumerable(Of String)) As Boolean

            'Downloads will happen on their own thread so the UI can be updated with their status
            Dim DownloadMethod As New DelegateDownloadFile(AddressOf DownloadFile)

            Dim DownloadedKeywordsFiles As New List(Of KeywordFileState)(keywordLists.Count)
            Dim FailedDownloadedKeywordsFiles As New List(Of String)

            Dim Keywords As New Text.KeywordsList

            'Download files
            If keywordLists.Count > 0 Then
                LogIt("Starting download from " & _Settings.DownloadUrl)
            End If

            For Each FileName As String In keywordLists
                Dim State As KeywordFileState = Nothing

                For Attempt As Integer = 1 To _DownloadRetries
                    'The downloadfile method is asyncronous and returns a wait handle before the download is complete.
                    'The wait handle is signled when the download is complete so we need to wait for that
                    State = DownloadMethod.EndInvoke(DownloadMethod.BeginInvoke(_Settings.DownloadUrl & FileName, Nothing, Nothing))
                    State.WaitHandle.WaitOne()

                    If Not State.RefreshRequired Then
                        LogIt(State.FileName & " is up to date")
                        DownloadedKeywordsFiles.Add(State)
                        'The file was not modified since the last upload so contine to the next file
                        Exit For
                    End If

                    If Not String.IsNullOrEmpty(State.ErrorMessage) OrElse Not File.Exists(_CacheWorkingPath & State.CachedFileName) Then
                        'The download failed, try again
                        Continue For
                    End If

                    DownloadedKeywordsFiles.Add(State)
                    Exit For

                    'No longer providing md5 files after Oct/2020
                    'Now get the md5 files for the file
                    'Dim MD5FileName As String = Path.GetFileNameWithoutExtension(FileName) & ".md5"
                    'Download the md5 file to check against
                    'DownloadMethod.EndInvoke(DownloadMethod.BeginInvoke(_Settings.DownloadUrl & MD5FileName, Nothing, Nothing)).WaitHandle.WaitOne()

                    'If File.Exists(_CacheWorkingPath & MD5FileName) Then
                    '    Dim FileMD5 As String = MD5CalcFile(_CacheWorkingPath & FileName)
                    '    Dim CorrectMD5 As String
                    '    Using MD5File As StreamReader = File.OpenText(_CacheWorkingPath & MD5FileName)
                    '        'Get the first 32 charachters of the first line, this should be the MD5 hash, anything after that is usually the file name which we don't care about
                    '        CorrectMD5 = MD5File.ReadLine.Substring(0, 32)
                    '    End Using

                    '    If CorrectMD5 IsNot Nothing AndAlso CorrectMD5.Equals(FileMD5, StringComparison.OrdinalIgnoreCase) Then
                    '        'The file downloaded successfully
                    '        'Do not attempt another download
                    '        DownloadedKeywordsFiles.Add(State)
                    '        Exit For
                    '    Else
                    '        'The MD5s don't match so retry
                    '        'Start over
                    '        LogIt(State.FileName & " consistency error!")
                    '        Continue For
                    '    End If
                    'Else
                    '    'The file downloaded but we don't have an md5 file to check so don't reattempt
                    '    Exit For
                    'End If
                Next

                'Add the file to the keywords list
                If State IsNot Nothing AndAlso File.Exists(_CacheWorkingPath & State.CachedFileName) Then
                    Using KeywordsFile As StreamReader = File.OpenText(_CacheWorkingPath & State.CachedFileName)
                        While Not KeywordsFile.EndOfStream
                            Keywords.AddKeyword(KeywordsFile.ReadLine)
                        End While
                    End Using
                Else
                    FailedDownloadedKeywordsFiles.Add(FileName)
                End If
            Next

            'Download custom files
            If customKeywordListUrls.Count > 0 Then
                LogIt("Starting download from custom URLs")
            End If

            For Each CustomKeywordUrl As CustomKeywordListSetting In customKeywordListUrls

                Dim State As KeywordFileState = Nothing
                For Attempt As Integer = 1 To _DownloadRetries
                    'The downloadfile method is asyncronous and returns a wait handle before the download is complete.
                    'The wait handle is signled when the download is complete so we need to wait for that

                    State = DownloadMethod.EndInvoke(DownloadMethod.BeginInvoke(CustomKeywordUrl.Location, Nothing, Nothing))
                    State.WaitHandle.WaitOne()

                    If Not State.RefreshRequired Then
                        LogIt(State.FileName & " is up to date")
                        DownloadedKeywordsFiles.Add(State)
                        'The file was not modified since the last upload so contine to the next file
                        Exit For
                    End If

                    If Not String.IsNullOrEmpty(State.ErrorMessage) OrElse Not File.Exists(_CacheWorkingPath & State.CachedFileName) Then
                        'The download failed, try again
                        Continue For
                    End If

                    'If String.IsNullOrEmpty(State.ErrorMessage) AndAlso File.Exists(_CacheWorkingPath & State.CachedFileName) Then
                    '    'File downloaded successfully
                    '    DownloadedKeywordsFiles.Add(State)
                    '    Exit For
                    'Else
                    '    'The download failed, try again
                    '    Continue For
                    'End If

                    If CustomKeywordUrl.Verify Then
                        'Now get the md5 files for the file
                        Dim MD5FileName As String = Path.GetFileNameWithoutExtension(CustomKeywordUrl.Location) & ".md5"
                        'Download the md5 file to check against
                        DownloadMethod.EndInvoke(DownloadMethod.BeginInvoke(CustomKeywordUrl.Location.Substring(0, CustomKeywordUrl.Location.LastIndexOf(".")) & ".md5", Nothing, Nothing)).WaitHandle.WaitOne()

                        If File.Exists(_CacheWorkingPath & MD5FileName) Then
                            Dim FileMD5 As String = MD5CalcFile(_CacheWorkingPath & Path.GetFileName(CustomKeywordUrl.Location))
                            Dim CorrectMD5 As String
                            Using MD5File As StreamReader = File.OpenText(_CacheWorkingPath & MD5FileName)
                                'Get the first 32 charachters of the first line, this should be the MD5 hash, anything after that is usually the file name which we don't care about
                                CorrectMD5 = MD5File.ReadLine.Substring(0, 32)
                            End Using

                            If CorrectMD5 IsNot Nothing AndAlso CorrectMD5.Equals(FileMD5, StringComparison.OrdinalIgnoreCase) Then
                                'The file downloaded successfully
                                'Do not attempt another download
                                DownloadedKeywordsFiles.Add(State)
                                Exit For
                            Else
                                'The MD5s don't match so retry
                                'Start over
                                LogIt(State.FileName & " consistency error!")
                                Continue For
                            End If
                        Else
                            'The file downloaded but we don't have an md5 file to check so don't reattempt
                            Exit For
                        End If
                    End If

                Next

                'Add the file to the keywords list
                If State IsNot Nothing AndAlso File.Exists(_CacheWorkingPath & State.CachedFileName) Then
                    Using KeywordsFile As StreamReader = File.OpenText(_CacheWorkingPath & State.CachedFileName)
                        While Not KeywordsFile.EndOfStream
                            Keywords.AddKeyword(KeywordsFile.ReadLine)
                        End While
                    End Using
                Else
                    FailedDownloadedKeywordsFiles.Add(CustomKeywordUrl.Location)
                End If
            Next

            'Get local custom files
            If _Settings.CustomKeywordListPaths.Count > 0 Then
                LogIt("Starting download from custom paths")
            End If

            For Each CustomFilePath As CustomKeywordListSetting In customKeywordListPaths
                If File.Exists(CustomFilePath.Location) Then
                    Dim CustomFileState = New KeywordFileState(Path.GetFileName(CustomFilePath.Location))
                    CustomFileState.CachedFileName = Path.GetFileName(CustomFilePath.Location)

                    'Check the time the file was last written to
                    Dim CustomFileInfo As New FileInfo(CustomFilePath.Location)
                    If Not CustomFileState.LastModified.Equals(CustomFileInfo.LastWriteTimeUtc) Then
                        CustomFileState.RefreshRequired = True
                        CustomFileState.LastModified = CustomFileInfo.LastWriteTimeUtc
                    Else
                        CustomFileState.RefreshRequired = False
                        LogIt(Path.GetFileName(CustomFilePath.Location) & " is up to date")
                    End If

                    If CustomFileState.RefreshRequired Then
                        'Copy the file to the cache
                        If File.Exists(_CacheWorkingPath & Path.GetFileName(CustomFilePath.Location)) Then File.Delete(_CacheWorkingPath & Path.GetFileName(CustomFilePath.Location))
                        File.Copy(CustomFilePath.Location, _CacheWorkingPath & Path.GetFileName(CustomFilePath.Location))

                        LogIt(" Retrieved  " & Path.GetFileName(CustomFilePath.Location))

                        DownloadedKeywordsFiles.Add(CustomFileState)

                        If CustomFilePath.Verify Then
                            'Now get the md5 files for the file
                            Dim MD5FileName As String = Path.GetFileNameWithoutExtension(CustomFilePath.Location) & ".md5"
                            Dim MD5SourcePath = Path.Combine(Path.GetDirectoryName(CustomFilePath.Location), MD5FileName)
                            If File.Exists(MD5SourcePath) Then
                                'Download the md5 file to check against
                                If File.Exists(_CacheWorkingPath & MD5FileName) Then File.Delete(_CacheWorkingPath & MD5FileName)
                                File.Copy(MD5SourcePath, _CacheWorkingPath & MD5FileName)

                                If File.Exists(_CacheWorkingPath & MD5FileName) Then
                                    Dim FileMD5 As String = MD5CalcFile(_CacheWorkingPath & Path.GetFileName(CustomFilePath.Location))
                                    Dim CorrectMD5 As String
                                    Using MD5File As StreamReader = File.OpenText(_CacheWorkingPath & MD5FileName)
                                        'Get the first 32 charachters of the first line, this should be the MD5 hash, anything after that is usually the file name which we don't care about
                                        CorrectMD5 = MD5File.ReadLine.Substring(0, 32)
                                    End Using

                                    If CorrectMD5 IsNot Nothing AndAlso CorrectMD5.Equals(FileMD5, StringComparison.OrdinalIgnoreCase) Then
                                        'The file downloaded successfully
                                        'Do not attempt another download

                                        CustomFileState.CachedFileName = Path.GetFileName(CustomFilePath.Location)
                                        '
                                    Else
                                        'The MD5s don't match so retry
                                        'Start over
                                        LogIt(Path.GetFileName(CustomFilePath.Location) & " consistency error!")
                                        Continue For
                                    End If
                                Else
                                    'The file downloaded but we don't have an md5 file to check so don't reattempt
                                    Continue For
                                End If
                            End If
                        End If
                    End If


                    ''Add the keywords from the file
                    'Using KeywordsFile As StreamReader = File.OpenText(CustomFilePath.Location)
                    '    While Not KeywordsFile.EndOfStream
                    '        Keywords.AddKeyword(KeywordsFile.ReadLine)
                    '    End While
                    'End Using


                    'Add the file to the keywords list
                    If CustomFileState IsNot Nothing AndAlso File.Exists(_CacheWorkingPath & CustomFileState.CachedFileName) Then
                        Using KeywordsFile As StreamReader = File.OpenText(_CacheWorkingPath & CustomFileState.CachedFileName)
                            While Not KeywordsFile.EndOfStream
                                Keywords.AddKeyword(KeywordsFile.ReadLine)
                            End While
                        End Using
                    Else
                        FailedDownloadedKeywordsFiles.Add(CustomFilePath.Location)
                    End If

                Else
                    FailedDownloadedKeywordsFiles.Add(CustomFilePath.Location)
                    LogIt(CustomFilePath.Location & " was not found")
                End If
            Next

            'Add any static keywords
            For Each StaticKeyword As String In staticKeywords
                Keywords.AddKeyword(StaticKeyword)
            Next

            _AllDownloadedKeywordsFiles.AddRange(DownloadedKeywordsFiles)
            'Now that we have all the files, check to see if any have been updated
            Dim UpdateRequired As Boolean = False
            For Each KeywordFile As KeywordFileState In DownloadedKeywordsFiles
                If KeywordFile.RefreshRequired = True Then
                    UpdateRequired = True
                    Exit For
                End If
            Next

            If Not UpdateRequired Then
                SetProgress(100)
                LogIt("Update not required")
            Else 'Update is required

                If _Settings.ConsolidateKeywords Then
                    LogIt("Consolidating redundant keywords")
                    SetProgress(0)
                    Dim TotalSteps As Integer = Keywords.Count
                    Dim CurrentStep As Integer = 0
                    Dim KeywordsRemoved As Integer = 0
                    'Sort the keyword list by length because string of a longer length will not be contained in shorter strings
                    Keywords.Sort()

                    'Remove an keywords that are matched by regualr expression
                    For Each Pattern As Regex In Keywords.Regexes
                        Dim i As Integer = 0
                        While i < Keywords.Keywords.Count
                            If Pattern.Match(Keywords.Keywords(i)).Success Then
                                Keywords.Keywords.RemoveAt(i)
                                KeywordsRemoved += 1
                                CurrentStep += 1
                            Else
                                i += 1
                            End If
                        End While

                        CurrentStep += 1
                        SetProgress(Math.Min((CurrentStep / TotalSteps) * 100, 100))
                    Next


                    Dim c As Integer = 0
                    Dim n
                    While c < Keywords.Keywords.Count
                        n = c + 1 'start at the next keyword after this
                        While n < Keywords.Keywords.Count
                            If Keywords.Keywords(n).IndexOf(Keywords.Keywords(c), StringComparison.Ordinal) > -1 Then
                                'This (n) keyword will be matched by a smaller one (c) so remove it
                                Keywords.Keywords.RemoveAt(n)
                                KeywordsRemoved += 1
                                CurrentStep += 1
                            Else
                                n += 1
                            End If
                        End While
                        c += 1

                        CurrentStep += 1
                        SetProgress(Math.Min((CurrentStep / TotalSteps) * 100, 100))
                    End While

                    LogIt(KeywordsRemoved & " corrections done")
                End If

                Dim LastKeyword As String = Keywords.Keywords(Keywords.Keywords.Count - 1)
                Keywords.Keywords.RemoveAt(Keywords.Keywords.Count - 1)

                'Write the file
                LogIt("Saving to temp file")
                Using OutputFile As FileStream = File.Open(_WorkingPath & outputFileName & ".tmp", FileMode.Create, FileAccess.Write, FileShare.None)
                    Using OutputWriter As New StreamWriter(OutputFile)
                        For Each Pattern As Regex In Keywords.Regexes
                            OutputWriter.WriteLine(Pattern.ToString)
                        Next
                        For Each Keyword As String In Keywords.Keywords
                            OutputWriter.WriteLine(Keyword)
                        Next
                    End Using
                End Using


                If File.Exists(_WorkingPath & outputFileName) Then
                    Dim outputFileNameBackup = Path.GetFileNameWithoutExtension(outputFileName) & ".old"
                    LogIt("Saving previous " & outputFileName & " as " & outputFileNameBackup & " and saving new " & outputFileName)
                    File.Replace(_WorkingPath & outputFileName & ".tmp", _WorkingPath & outputFileName, _WorkingPath & outputFileNameBackup)
                Else
                    LogIt("Saving new " & outputFileName)
                    File.Copy(_WorkingPath & outputFileName & ".tmp", _WorkingPath & outputFileName)
                End If
                File.Delete(_WorkingPath & outputFileName & ".tmp")

                'The file change notification for the new output file will not have been generated yet so force it
                Using ForceFCN As FileStream = File.OpenWrite(_WorkingPath & outputFileName)
                    Using ForceFCNWriter As New StreamWriter(ForceFCN)
                        ForceFCN.Seek(ForceFCN.Length, SeekOrigin.Begin)
                        ForceFCNWriter.WriteLine(LastKeyword)
                    End Using
                End Using

            End If

            Return UpdateRequired
        End Function

        ''' <summary>
        ''' Type safe function pointer for DownloadFile so it can be invoked asyncrounously
        ''' </summary>
        ''' <param name="downloadPath"></param>
        ''' <remarks></remarks>
        Private Delegate Function DelegateDownloadFile(ByVal downloadPath As String) As KeywordFileState


        ''' <summary>
        ''' Downloads a file and associated md5 file
        ''' </summary>
        ''' <param name="downloadPath"></param>
        ''' <remarks></remarks>
        Private Function DownloadFile(ByVal downloadPath As String) As KeywordFileState
            Dim State As New KeywordFileState(Path.GetFileName(downloadPath))

            'Check if the list has been updated since the last download
            If Not State.LastModified.Equals(DateTime.MinValue) Then
                Dim HeadRequest As HttpWebRequest = WebRequest.Create(downloadPath)
                HeadRequest.Method = "HEAD" 'Do not download the whole file
                Dim HeadResponse As WebResponse = Nothing
                Try
                    HeadResponse = HeadRequest.GetResponse
                Catch ex As Exception

                Finally
                    If HeadResponse IsNot Nothing Then
                        'Make sure to close the response of the connection will stay open
                        HeadResponse.Close()
                    End If
                End Try

                If HeadResponse IsNot Nothing AndAlso HeadResponse.Headers("Last-Modified") IsNot Nothing Then
                    Dim LastModified As DateTime
                    If DateTime.TryParse(HeadResponse.Headers("Last-Modified"), LastModified) Then
                        If LastModified <= State.LastModified Then
                            State.RefreshRequired = False
                            'Allow waiting thread to continue
                            State.WaitHandle.Set()
                            Return State
                        End If
                    End If
                End If
            End If


            LogIt("Downloading " & Path.GetFileName(downloadPath))
            SetProgress(0)
            Dim webClient As New WebClient()
            AddHandler webClient.DownloadFileCompleted, New AsyncCompletedEventHandler(AddressOf Completed)
            AddHandler webClient.DownloadProgressChanged, New DownloadProgressChangedEventHandler(AddressOf ProgressChanged)

            'We do the async download to update the status bar, downloading more than 1 file at a time would not give us an advantage
            webClient.DownloadFileAsync(New Uri(downloadPath), _CacheWorkingPath & Path.GetFileName(downloadPath), State)
            Return State
        End Function

        ''' <summary>
        ''' Called when the progress of a file download is reported
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ProgressChanged(ByVal sender As Object, ByVal e As DownloadProgressChangedEventArgs)
            SetProgress(e.ProgressPercentage)
        End Sub

        ''' <summary>
        ''' Called when a file download is complete
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Completed(ByVal sender As Object, ByVal e As AsyncCompletedEventArgs)
            Dim State As KeywordFileState = DirectCast(e.UserState, KeywordFileState)
            If e.Error Is Nothing Then
                If Path.GetExtension(State.FileName).Equals(".gz", StringComparison.OrdinalIgnoreCase) Then
                    LogIt("Decompressing gzipped file " & State.FileName)
                    Using FileIn As FileStream = New FileStream(_CacheWorkingPath & State.FileName, FileMode.Open, FileAccess.Read)
                        Using ZipStream As GZipStream = New GZipStream(FileIn, CompressionMode.Decompress)
                            Using FileOut As FileStream = New FileStream(_CacheWorkingPath & State.FileName.Substring(0, State.FileName.Length - 3), FileMode.Create, FileAccess.Write)
                                Dim TempBytes(4096) As Byte
                                Dim i As Integer
                                Dim Done As Boolean = False
                                While Not Done
                                    i = ZipStream.Read(TempBytes, 0, TempBytes.Length)
                                    If i = 0 Then
                                        Done = True
                                    Else
                                        FileOut.Write(TempBytes, 0, i)
                                    End If
                                End While
                                State.CachedFileName = Path.GetFileName(FileOut.Name)
                            End Using
                        End Using
                    End Using

                    'Delete the gzipped file
                    Try
                        File.Delete(_CacheWorkingPath & State.FileName)
                    Catch ex As Exception
                        LogIt("Could not delete file " & State.FileName)
                    End Try

                End If

                Dim WebClient As WebClient = DirectCast(sender, WebClient)
                LogIt(" Retrieved  " & Path.GetFileName(State.FileName))
                DateTime.TryParse(WebClient.ResponseHeaders("Last-Modified"), State.LastModified)
                'If we have not changed the file name by now then the downloaded file name is the same as the requested file name
                If String.IsNullOrEmpty(State.CachedFileName) Then State.CachedFileName = State.FileName
            Else
                State.ErrorMessage = e.Error.Message
                LogIt(Path.GetFileName(State.FileName) & " download error: " & e.Error.Message)
            End If

            'Signal the waiting processing thread that the file is downloaded and it can continue
            State.WaitHandle.Set()
        End Sub

        ''' <summary>
        ''' Sets the progress label on the UI thread
        ''' </summary>
        ''' <param name="text"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function LogIt(ByVal text As String) As Object
            If Label1.InvokeRequired Then
                Label1.Invoke(Function() LogIt(text))
            Else
                Label1.Text = text
                _LogWriter.WriteLine(Now.ToString("MMddyy HH:mm:ss") & " " & text)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Sets the progress bar on the UI thread
        ''' </summary>
        ''' <param name="percent"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SetProgress(ByVal percent As Integer) As Object
            If ProgressBar1.InvokeRequired Then
                ProgressBar1.Invoke(Function() SetProgress(percent))
            Else
                ProgressBar1.Value = percent
            End If
            Return Nothing
        End Function


        ''' <summary>
        ''' Specify the path to a file and this routine will calculate your hash
        ''' </summary>
        ''' <param name="filepath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function MD5CalcFile(ByVal filepath As String) As String

            ' open file (as read-only)
            Using Reader As New System.IO.FileStream(filepath, IO.FileMode.Open, IO.FileAccess.Read)
                Using MD5 As New System.Security.Cryptography.MD5CryptoServiceProvider

                    ' hash contents of this stream
                    Dim hash() As Byte = MD5.ComputeHash(Reader)

                    ' return formatted hash
                    Return ByteArrayToString(hash)

                End Using
            End Using

        End Function

        ''' <summary>
        ''' Utility function to convert a byte array into a hex string
        ''' </summary>
        ''' <param name="arrInput"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ByteArrayToString(ByVal arrInput() As Byte) As String

            Dim SB As New System.Text.StringBuilder(arrInput.Length * 2)

            For i As Integer = 0 To arrInput.Length - 1
                SB.Append(arrInput(i).ToString("X2"))
            Next

            Return SB.ToString().ToLower

        End Function


        ''' <summary>
        ''' Tracks the state of a download
        ''' </summary>
        ''' <remarks></remarks>
        Private Class KeywordFileState
            ''' <summary>
            ''' The name of the requested file
            ''' </summary>
            ''' <remarks></remarks>
            Public FileName As String

            ''' <summary>
            ''' The name of the file after the download processor has made changes if any
            ''' </summary>
            ''' <remarks></remarks>
            Public CachedFileName As String

            Public LastModified As DateTime
            Public WaitHandle As New EventWaitHandle(False, EventResetMode.ManualReset)
            Public ErrorMessage As String = Nothing
            Public RefreshRequired As Boolean = True

            Public Sub New(ByVal f As String)
                FileName = f
                For Each DatLine() As String In _DatFileLines
                    If DatLine.Length >= 3 AndAlso DatLine(0).Equals(FileName, StringComparison.OrdinalIgnoreCase) AndAlso File.Exists(_CacheWorkingPath & DatLine(1)) Then
                        LastModified = DateTime.FromBinary(Convert.ToInt64(DatLine(2)))
                        CachedFileName = DatLine(1)
                        Exit For
                    End If
                Next


            End Sub
        End Class

    End Class


End Namespace