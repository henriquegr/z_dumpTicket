Public Module uteis

    Function fDater(ByVal nome As String) As String

        Dim sNome As String
        sNome = nome & fDater()
        Return sNome
    End Function

    Function fDater() As String

        Dim sNome As String
        sNome = "-" + _
        Format(Now.Year, "0000") + "-" + _
        Format(Now.Month, "00") + "-" + _
        Format(Now.Day, "00") + "--" + _
        Format(Now.Hour, "00") + "-" + _
        Format(Now.Minute, "00") + "-" + _
        Format(Now.Second, "00") + "-" + _
        Format(Now.Millisecond, "0000")

        Return sNome
    End Function



End Module
