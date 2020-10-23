Imports System.Text.RegularExpressions


Namespace DnsRedirector.Updater.Text

    ''' <summary>
    ''' A list of strings and regular expression that can be compared against a string
    ''' to determine of that string contains any of the keywords
    ''' </summary>
    ''' <remarks></remarks>
    Public Class KeywordsList

        Public Keywords As New List(Of String)
        Public Regexes As New List(Of Regex)

        ''' <summary>
        ''' Number of keywords in the list
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Count() As Integer
            Get
                Return Keywords.Count + Regexes.Count
            End Get
        End Property

        ''' <summary>
        ''' Add a keyword to the list
        ''' </summary>
        ''' <param name="keyword"></param>
        ''' <remarks>Keywords containing "*" will be treated as a regualr expression</remarks>
        Public Sub AddKeyword(ByVal keyword As String)
            If keyword.Contains(" ") Then Exit Sub
            If keyword.Contains("//") Then Exit Sub
            If keyword.Contains(";") Then Exit Sub
            If keyword.Contains("@") Then Exit Sub
            If keyword.Contains("#") Then Exit Sub
            If keyword.Contains("%") Then Exit Sub

            If keyword.StartsWith("^", StringComparison.OrdinalIgnoreCase) Then
                AddRegex(keyword)
            ElseIf keyword.IndexOf("*", StringComparison.OrdinalIgnoreCase) > -1 Then
                AddRegex(keyword.Replace("*", ".*?"))
            Else
                If Not Keywords.Contains(keyword) Then
                    Keywords.Add(keyword)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Add a regular expression object
        ''' </summary>
        ''' <param name="regex"></param>
        ''' <remarks></remarks>
        Public Sub AddRegex(ByVal regex As Regex)
            Dim HasRegex As Boolean = False
            For Each Pattern As Regex In Regexes
                If Pattern.ToString.Equals(regex.ToString, StringComparison.InvariantCulture) Then
                    HasRegex = True
                    Exit For
                End If
            Next
            If Not HasRegex Then Regexes.Add(regex)
        End Sub

        ''' <summary>
        ''' Converts the string to a regular expression
        ''' </summary>
        ''' <param name="pattern"></param>
        ''' <remarks></remarks>
        Public Sub AddRegex(ByVal pattern As String)
            Dim Regex As New Regex(pattern, RegexOptions.Compiled Or RegexOptions.Singleline)
            AddRegex(Regex)
        End Sub

        Public Sub Sort()
            Keywords.Sort(AddressOf CompareKeywordsByLength)
        End Sub

        ''' <summary>
        ''' Method used to sort the keywords list (acending) by the length of the keyword
        ''' </summary>
        ''' <param name="x"></param>
        ''' <param name="y"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function CompareKeywordsByLength(ByVal x As String, ByVal y As String) As Integer

            If x Is Nothing Then
                If y Is Nothing Then
                    ' If x is Nothing and y is Nothing, they're
                    ' equal. 
                    Return 0
                Else
                    ' If x is Nothing and y is not Nothing, y
                    ' is greater. 
                    Return -1
                End If
            Else
                ' If x is not Nothing...
                '
                If y Is Nothing Then
                    ' ...and y is Nothing, x is greater.
                    Return 1
                Else
                    ' ...and y is not Nothing, compare the 
                    ' lengths of the two strings.
                    '
                    Dim retval As Integer = _
                        x.Length.CompareTo(y.Length)

                    If retval <> 0 Then
                        ' If the strings are not of equal length,
                        ' the longer string is greater.
                        '
                        Return retval
                    Else
                        ' If the strings are of equal length,
                        ' sort them with ordinary string comparison.
                        '
                        Return x.CompareTo(y)
                    End If
                End If
            End If

        End Function


    End Class
End Namespace
