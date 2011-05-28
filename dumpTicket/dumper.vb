'////////////////////////////////////////////////////////////////////////
'// Author:      Henrique Grammelsbacher
'// Created:     sexta-feira, março 18, 2011 1:56:03 
'//
'// Description:    Esta classe cria faz o dump de um ticket do CA Service Desk
'//                 a partir de um arquivo de notificação
'//
'////////////////////////////////////////////////////////////////////////

Public Class dumper

    Private sNomeArquivo As String
    Private sNomeTemplate As String
    Private sNomeObjetoSDM As String
    Private sPersidObejtoSDM As String
    Private sIDObjetoSDM As Integer
    Private sPath As String
    Private fsoArquivoMensagemSDM As System.IO.StreamReader
    Private sCorpoArquivoModelo As String
    Private iArquivoAberto As Integer = 0
    Private iTemplateAberto As Integer = 0
    Private usd As sdm
    Private bDebug As Boolean = False
    Private sAtribLoop As String = ""
    Private sAttribLoopCommonName As String = ""

    'Arquivo de mensagem do SDM aberto
    Public ReadOnly Property arquivoAberto() As Integer
        Get
            arquivoAberto = iArquivoAberto
        End Get

    End Property

    'Arquivo template carregado
    Public ReadOnly Property templateAberto() As Integer
        Get
            templateAberto = iTemplateAberto
        End Get
    End Property

    'Este é o atributo que será iterado, para gerar diversos outputs no arquivo destino, sempre uma LREL de ICs
    Public Property atributoLoop() As String
        Get
            atributoLoop = sAtribLoop
        End Get
        Set(ByVal value As String)
            If value.IndexOf(".") < 0 Then
                Throw New Exception("O nome do atributo de loop não atende ao modelo <nome_do_atributo_no_objeto>.<nome_do_atributo_no_IC>")
                Exit Property
            End If
            sAtribLoop = value.Substring(0, value.IndexOf("."))
            sAttribLoopCommonName = value.Substring(value.IndexOf(".") + 1, value.Length - value.IndexOf(".") - 1)

        End Set
    End Property

    'Inicializa a classe abrindo o arquvio de mensagem e processando seu ID e Producer
    Public Sub New(ByVal nome_do_arquivo_de_mensagem_do_SDM As String, ByVal depurar As Boolean)

        Dim sPath As String = My.Application.Info.DirectoryPath
        Dim iLoopOpenF As Integer = 0

        'Este flag determina se iremos gerar logs de depuração ou não
        bDebug = depurar

        log.logLine(bDebug, "Construindo classe dumper")

        'Iremos tentar por 11 vezes abrir o arquivo, com intervalos de 1 sec
        While iLoopOpenF < 11
            iLoopOpenF = iLoopOpenF + 1

            'Abrindo arquivo
            log.logLine(bDebug, "Abrindo arquivo de mensagem do SDM '" & nome_do_arquivo_de_mensagem_do_SDM & "'")
            Try
                fsoArquivoMensagemSDM = New System.IO.StreamReader(nome_do_arquivo_de_mensagem_do_SDM)
                iLoopOpenF = 11
            Catch ex As Exception
                log.logLine(bDebug, "Não consegui abrir o arquivo '" & nome_do_arquivo_de_mensagem_do_SDM & "'. Tentando novamente em 1 segundo.")
                Threading.Thread.Sleep(1000)
                If iLoopOpenF = 10 Then
                    log.logErro(ex.Message)
                    Exit Sub
                End If

            End Try
        End While

        'Caso tenha aberto o arquivo, iremos iterar por ele para idenciticar o ID e o Producer,
        'e caso o processamento tenha sido feito com erro, ou seja, não encontramos um ID, uma excessão será lançada
        If Me.processaArquivoMail() Then
            Dim sLogFile As String
            sLogFile = fDater(sNomeObjetoSDM & "-" & sIDObjetoSDM)
            log.logLine(bDebug, "Arquivo de mail processado. Alternando arquivo de log para '" & sLogFile & "'")
            log.arquivo = sPath & "\log\" & sLogFile
        Else
            log.logErro("Não foi possível identificar um ID de objeto no arquivo '" & nome_do_arquivo_de_mensagem_do_SDM & "'")
            log.dispose()
            Throw New Exception("Não foi possível identificar um ID de objeto no arquivo '" & nome_do_arquivo_de_mensagem_do_SDM & "'")
        End If

        'Se abriu e identificou o ID, podemos continuar a brincadeira, populando iArquivoAberto com 1, o que
        'servira de base para decidirmos se iremos ou nao continuar processando o codigo
        If sPersidObejtoSDM.Length > 0 Then
            iArquivoAberto = 1
        Else
            iArquivoAberto = 0
        End If


    End Sub

    'Oh, não! O destruidor!!!
    Public Sub dispose()
        log.logLine(bDebug, "Destruindo classe dumper")
        If iArquivoAberto = 1 Then
            fsoArquivoMensagemSDM.Dispose()
        End If
        log.dispose()

    End Sub

    'Carrega o arquivo modelo a ser utilizado para gerar o arquivo final
    Public Function carregaTemplate(ByVal nome_do_arquivo As String) As Boolean

        If System.IO.File.Exists(nome_do_arquivo) Then

            'Para garantir que não haja erro de compartilhamento, tenta abrir o arquivo de template 10 vezes
            'Com intervalos de 500msec
            Dim iRetorno As String = 0
            For i = 0 To 9
                Try
                    Dim s As New System.IO.StreamReader(nome_do_arquivo)
                    sCorpoArquivoModelo = s.ReadToEnd
                    s.Close()
                    s.Dispose()
                    iTemplateAberto = 1
                    Exit For
                    iRetorno = 1
                Catch ex As Exception
                    iRetorno = 0
                End Try
                System.Threading.Thread.Sleep(500)
            Next
            Return iRetorno

        Else
            log.logErro("Não foi possível encontrar o arquivo modelo '" & nome_do_arquivo & "'")
            Return 0
        End If

    End Function

    'Salva o arquivo template com as vars substituidas no path final
    Public Function salvaArquivo(ByVal path As String, ByVal arquivo As String) As Boolean

        Dim iTentativas As Integer = 0
        Dim iArquivoSalvo As Integer = 0

        'Tenta salvar por 10 vezes
        For iTentativas = 0 To 9
            If System.IO.Directory.Exists(path) Then
                Try
                    Dim s As New System.IO.StreamWriter(path & "\" & arquivo)
                    s.Write(sCorpoArquivoModelo)
                    s.Flush()
                    s.Close()
                    s.Dispose()
                    iArquivoSalvo = 1
                    Exit For
                Catch ex As Exception
                    log.logErro("Não foi possível criar o arquivo '" & arquivo & "' caminho '" & path & "' - Tentativa: " & iTentativas.ToString)
                End Try
            Else
                log.logErro("Não foi possível encontrar o caminho '" & path & "'")
            End If
        Next

        'Caso não tenha conseguido salvar o arquivo no destino, salva na pasta arquivos_nao_enviados
        If iArquivoSalvo = 0 Then
            Try
                Dim s As New System.IO.StreamWriter(My.Application.Info.DirectoryPath & "\arquivos_nao_enviados\" & arquivo)
                s.Write(sCorpoArquivoModelo)
                s.Flush()
                s.Close()
                s.Dispose()
                iArquivoSalvo = 1
            Catch ex As Exception
                log.logErro(My.Application.Info.DirectoryPath & "\arquivos_nao_enviados\" & arquivo)
            End Try
        End If


    End Function

    'Esta função sobrecarrega a próxima, aceitando como entrada um arquivo de mapppings ao invés de uma hashtable
    Public Function substituiValores(ByVal arquivo_de_mapeamentos As String) As Boolean

        Dim aMapeamentos As New Hashtable

        'Abre o arquivo de mapeamentos e itera por todas as linhas
        If System.IO.File.Exists(arquivo_de_mapeamentos) Then
            Dim s As New System.IO.StreamReader(arquivo_de_mapeamentos)
            Dim sLinha As String
            While s.Peek <> -1

                sLinha = s.ReadLine
                If sLinha.IndexOf("=") > 0 Then
                    aMapeamentos.Add(sLinha.Split("=")(0), sLinha.Split("=")(1))
                End If

            End While

            'Aqui chamamos a funçao que processa a coleçao de vars x atributos do SD no arquivo de template
            substituiValores(aMapeamentos)
        Else
            log.logErro("Não foi possível encontrar o arquivo de mapeamentos '" & arquivo_de_mapeamentos & "'")
        End If
    End Function

    'Efetua a substituição de valores no arquivo de template usando a hashtable com vars do template x atributos do SDM
    Private Function substituiValores(ByVal aMapeamentos As Hashtable) As Boolean

        Dim x As New cripto
        usd = New sdm(My.Settings.dumpTicket_sdws_USD_WebService, My.Settings.sUsuarioSDM, x.decripta(My.Settings.sSenhaSDM))
        Dim aAtributos As String()
        Dim aIcs As String()
        Dim aAtributosLrel(3) As String
        Dim colRetorno As Hashtable
        Dim map As DictionaryEntry
        Dim sCorpoTemp As String = ""
        Dim sCorpoCorrente As String = ""
        Dim i As Integer = 0

        If usd.conectado > 0 Then

            'itera por todos os atributos mapeados e envia a consulta do SDM
            ReDim aAtributos(aMapeamentos.Count - 1)
            For Each map In aMapeamentos
                aAtributos(i) = map.Value
                i = i + 1
            Next
            colRetorno = usd.getObject(sPersidObejtoSDM, aAtributos)

            If colRetorno.Count > 0 Then

                'Caso tenha sido definido um atributo de loop, iremos iterar por todos os itens da lista, gerando
                'um arquivo final com o dump para cada IC
                If sAtribLoop.Length > 0 Then
                    aIcs = usd.retornaListaNomesIcs(sPersidObejtoSDM, sAtribLoop, sAttribLoopCommonName)
                    'Caso o iterador tenha gerado erro, isso quer dizer que não ha ICs associados, neste caso, iremos gerar apenas um XML com IC vazio
                    Try
                        For Each IC In aIcs
                            sCorpoCorrente = sCorpoArquivoModelo
                            sCorpoCorrente = sCorpoCorrente.Replace("$" + sAttribLoopCommonName.ToUpper, IC)
                            For Each map In aMapeamentos
                                sCorpoCorrente = sCorpoCorrente.Replace((map.Key).ToString.ToUpper, colRetorno(map.Value))
                            Next
                            sCorpoTemp = sCorpoTemp & vbCrLf & sCorpoCorrente

                        Next
                        sCorpoArquivoModelo = sCorpoTemp
                    Catch ex As Exception
                        'Caso não tenha sido definido um atributo de loop, faremos apenas uma sub e deu pra bola
                        For Each map In aMapeamentos
                            sCorpoArquivoModelo = sCorpoArquivoModelo.Replace((map.Key).ToString.ToUpper, colRetorno(map.Value))
                        Next
                    End Try

                Else
                    'Caso não tenha sido definido um atributo de loop, faremos apenas uma sub e deu pra bola
                    For Each map In aMapeamentos
                        sCorpoArquivoModelo = sCorpoArquivoModelo.Replace((map.Key).ToString.ToUpper, colRetorno(map.Value))
                    Next
                End If

            End If

            Return 1
        Else
            Return 0
        End If



    End Function

    'Processa o arquivo de mail do SDM, identificando o producer e ID do objeto
    Private Function processaArquivoMail() As Boolean

        Dim itens As New hashtable
        Dim bDebug As Boolean

        'Inicio dos logs
        log.logLine(bDebug, "Processando arquivo de mail")

        'processando o arquivo
        Dim linha As Object
        Dim intLoop As Integer

        'Iterando pelo conteudo para montar a linha de comando
        While fsoArquivoMensagemSDM.Peek <> -1
            linha = fsoArquivoMensagemSDM.ReadLine
            intLoop = 1

            If linha.ToString.Contains("NX_NTF_PERSISTENT_ID=") Then
                sPersidObejtoSDM = linha.split("=")(1)
                'como existem duas sessões no arquivo de mensagem, ao econtrar o primeiro persid, sai do loop
                Exit While
            End If


        End While

        If sPersidObejtoSDM.Length > 0 Then
            sIDObjetoSDM = sPersidObejtoSDM.Split(":")(1)
            sNomeObjetoSDM = sPersidObejtoSDM.Split(":")(0)
            Return True
        Else
            Return False
        End If

    End Function


End Class
