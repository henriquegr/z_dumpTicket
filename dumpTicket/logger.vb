'////////////////////////////////////////////////////////////////////////
'// Author:      Henrique Grammelsbacher
'// Created:     sexta-feira, março 18, 2011 1:56:03 
'//
'// Description: Esta classe cria um arquivo de log e registra entradas neste arquivo a partir da funções
'// logErro e logLine
'//
'////////////////////////////////////////////////////////////////////////

Public Class logger

    Private swArquivoLog As System.IO.StreamWriter
    Private iArquivoCriado As Integer
    Private sArquivo As String

    Public Property arquivo() As String
        Get
            arquivo = sArquivo
        End Get
        Set(ByVal value As String)
            If iArquivoCriado = 1 Then
                swArquivoLog.Flush()
                swArquivoLog.Close()
                iArquivoCriado = 0
                sArquivo = arquivo
            Else
                sArquivo = arquivo
            End If
        End Set
    End Property

    Public Sub New(ByVal arquivo As String)
        sArquivo = arquivo
    End Sub

    'Cria o arquivo de log
    Private Sub criaArquivo()
        Try
            If Not System.IO.File.Exists(sArquivo & ".log") Then
                swArquivoLog = New System.IO.StreamWriter(sArquivo & ".log")
            End If
            iArquivoCriado = 1
        Catch ex As System.IO.DirectoryNotFoundException
            System.IO.Directory.CreateDirectory(sArquivo.Substring(0, sArquivo.LastIndexOf("\")))
            swArquivoLog = New System.IO.StreamWriter(sArquivo & ".log")
        Catch ex As Exception
            iArquivoCriado = 0
        End Try

    End Sub

    'Registra uma linha de erro no log
    Public Function logErro(ByVal mensagem As String) As Boolean
        Dim sMsg As String

        sMsg = " - ERR - " & mensagem
        Return log(True, mensagem)

    End Function

    'Registra uma linha de comentário no log
    Public Function logLine(ByVal armazena_em_arquivo_log As Boolean, ByVal mensagem As String) As Boolean
        Dim sMsg As String

        sMsg = " - log - " & mensagem
        Return log(armazena_em_arquivo_log, mensagem)

    End Function

    'Excreve no log
    Private Function log(ByVal armazenaEmArquivo As Boolean, ByVal mensagem As String) As Boolean
        Dim sMsg As String

        'Como só queremos criar o arquivo se hover mensagem, controlamos a criação do arquivo aqui
        If iArquivoCriado = 0 And armazenaEmArquivo Then
            criaArquivo()
        End If

        'Concatenando a data à mensagem de log
        sMsg = Now.ToString() & mensagem

        'A var debug armazena um booleano que informa se devemos ou não registrar em LOG. Caso esteja em modo de 
        'depuração, iremos armazenar em log.
        If armazenaEmArquivo Then
            Try
                swArquivoLog.WriteLine(mensagem)
                swArquivoLog.Flush()
                Console.WriteLine(mensagem)
            Catch ex As Exception
                Console.WriteLine(mensagem)
            End Try
        Else
            Console.WriteLine(mensagem)
        End If

        Return 1

    End Function

    'Destruidor!
    Public Sub dispose()
        If iArquivoCriado > 0 Then
            swArquivoLog.Flush()
            swArquivoLog.Close()
            swArquivoLog.Dispose()
        End If
    End Sub

End Class
