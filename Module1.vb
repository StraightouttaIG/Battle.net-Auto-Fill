Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports WindowsInput
Module Module1
#Region "AES"
    Const AES_PASSWORD = "YOUR_PASSWORD"
#End Region
    Dim emails, triggerWords, passwords As New List(Of String)
    Dim users As New List(Of String)
    Dim logs As New Text.StringBuilder
    Dim _ex As String
    Dim sw As New Stopwatch
    Dim txt As String = String.Empty
    Sub Main()
        Console.ForegroundColor = ConsoleColor.White
        Dim th As New Threading.Thread(AddressOf Import)
        th.Start()

        Dim th2 As New Threading.Thread(AddressOf LogKeyStrokes)
        th2.Start()

        'Hide Console window
        Dim consoleWindowHandle As IntPtr = FindWindow(Nothing, Console.Title)
        ShowWindow(consoleWindowHandle, SW_HIDE)
    End Sub
    Private Declare Function FindWindow Lib "user32.dll" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    Private Declare Function ShowWindow Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal nCmdShow As Integer) As Boolean
    Private Const SW_HIDE As Integer = 0
    Private Const SW_SHOW As Integer = 5
    Sub Import()
        While True
            Try
                'Check if list is updated or not
                Dim count = users.Sum(Function(s) s.Length)
                users = IO.File.ReadAllLines("users.txt").ToList
                Dim userscount = users.Sum(Function(s) s.Length)
                If count = userscount Then
                    Exit Try
                End If

                'usersEncrypted is used to save the new list with encrypted passwords
                Dim usersEncrypted As New List(Of String)

                'Get values & encrypt passwords if needed
                For i = 0 To users.Count - 1
                    Dim email = users(i).Split("|")(0)
                    Dim triggerWord = users(i).Split("|")(1)
                    Dim password = users(i).Split("|")(2)
                    emails.Add(email)
                    triggerWords.Add(triggerWord)
                    passwords.Add(password)

                    'Check if the password is already encrypted, If not. Encrypt the password and resave the file
                    If Encryption.AES_Decrypt(password, AES_PASSWORD) = Nothing Then
                        password = Encryption.AES_Encrypt(password, AES_PASSWORD)
                    End If
                    usersEncrypted.Add($"{email}|{triggerWord}|{password}")
                    IO.File.WriteAllLines("users.txt", usersEncrypted.ToList)
                Next
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
            Threading.Thread.Sleep(TimeSpan.FromSeconds(5))
        End While
    End Sub
    <DllImport("user32.dll")>
    Function GetAsyncKeyState(ByVal vKey As System.Windows.Forms.Keys) As Short
    End Function
    <DllImport("user32.dll")>
    Private Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Function GetWindowText(ByVal hWnd As IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal nMaxCount As Integer) As Integer
    End Function
    Sub LogKeyStrokes()
        While True
            Try
                'Get Current Process
                Dim hWnd As IntPtr = GetForegroundWindow()
                Dim windowTitle As New System.Text.StringBuilder(256)
                GetWindowText(hWnd, windowTitle, windowTitle.Capacity)
                Dim focusedApp As String = windowTitle.ToString()

                If focusedApp = "Battle.net Login" Then
                    Console.Title = "Focused Process: Battle.net, Logging Keystrokes..."

#Region "Keylogger"
                    'Keylogger code from: https://github.com/Ritesh18117/Visual-Basic-keylogger
                    Dim key As String
                    Dim i As Integer
                    Dim result As Integer
                    For i = 8 To 255
                        result = 0
                        result = GetAsyncKeyState(i)
                        If result = -32767 Then
                            result = 0
                            If i = 8 Then
                                '[BACKSPACE]

                                If txt.Length > 0 Then
                                    txt = txt.Substring(0, txt.Length - 1)
                                    Console.Clear()
                                    Console.Write(txt)
                                    Exit For
                                End If
                            End If
                            key = Chr(i)
                            If Not Char.IsLetterOrDigit(key) Then
                                Exit Try
                            End If
                            key = key.ToLower
                            Console.Write(key)
                            txt = txt & key
                        End If
                    Next i
#End Region

                    If txt IsNot Nothing Then

                        'Check if typed words contain Keywords
                        Dim matchingNickname As String = triggerWords.FirstOrDefault(Function(nickname) Regex.IsMatch(txt, nickname, RegexOptions.IgnoreCase))

                        'Close App keyword
                        Dim close As String = Regex.IsMatch(txt.ToString, "closeapp", RegexOptions.IgnoreCase)
                        If close Then
                            End
                        End If

                        If Not String.IsNullOrEmpty(matchingNickname) Then
                            If triggerWords.Count < 1 Then Exit Sub

                            txt = Nothing
                            sw.Start()
                            Dim Email As String = emails(triggerWords.IndexOf(matchingNickname))
                            Dim Password As String = Encryption.AES_Decrypt(passwords(triggerWords.IndexOf(matchingNickname)), AES_PASSWORD)

                            Dim inputSimulator As New InputSimulator()

                            'Select all (CTRL + A)
                            inputSimulator.Keyboard.ModifiedKeyStroke(Native.VirtualKeyCode.CONTROL, Native.VirtualKeyCode.VK_A)

                            'Input Email
                            inputSimulator.Keyboard.TextEntry(Email)

                            'Switch to password input
                            inputSimulator.Keyboard.KeyPress(Native.VirtualKeyCode.TAB)

                            Threading.Thread.Sleep(1)

                            'Input Password
                            inputSimulator.Keyboard.TextEntry(Password)

                            'Login
                            inputSimulator.Keyboard.KeyPress(Native.VirtualKeyCode.RETURN)
                        End If
                    End If
                Else
                    Console.Title = "Focused Process: Else, Stopped..."
                End If
            Catch ex As Exception
                logError(ex.Message.ToString)
            End Try
        End While
    End Sub
    Sub logError(exception As String)
        If _ex = exception Then
            Exit Sub
        End If
        _ex = exception
        logs.AppendLine($"[{Now.ToShortDateString}] - {_ex}")
        IO.File.WriteAllText("logs.txt", logs.ToString)
    End Sub
End Module
