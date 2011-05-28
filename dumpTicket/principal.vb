Module principal

    Public bDebug As Boolean
    Public log As logger

    Sub Main()

        Dim iLoop As Integer
        Dim sItem As String
        Dim sArquivoModelo As String = ""
        Dim sArquivoDestino As String = ""
        Dim sPathFinal As String = ""
        Dim sFile As String = ""
        Dim sFileSDM As String = ""
        Dim sArquivoMappings As String = ""
        Dim sAtributoLoop As String = ""
        Dim oDumper As dumper
        Dim iRemove As Integer = 0
        Dim iSai As Integer = 0

        'Iniciando o objeto de log, lembrando que não podemos usar o mesmo nome de arquivo sempre
        'para evitar erros de compartilhamento, uma vez que o executável pode ser acionado multiplas vezes
        'simultâneamente
        log = New logger(My.Application.Info.DirectoryPath & "\log\" & fDater("dumpTicket.log"))

        'Iternado pelos parametros e carregando as vars
        If My.Application.CommandLineArgs.Count > 0 Then
            If My.Application.CommandLineArgs.Item(0).ToString = "-?" Then
                fHelp()
                Exit Sub
            ElseIf My.Application.CommandLineArgs.Item(0).ToString = "-C" Then
                Dim c As New configForm
                c.ShowDialog()
                Exit Sub
            Else
                For iLoop = 0 To My.Application.CommandLineArgs.Count - 1
                    sItem = My.Application.CommandLineArgs.Item(iLoop).ToString.ToUpper
                    If sItem = "-D" Then
                        bDebug = True
                    End If
                    If sItem = "-A" Then
                        sArquivoDestino = My.Application.CommandLineArgs.Item(iLoop + 1).ToString.ToUpper
                    End If
                    If sItem = "-T" Then
                        sArquivoModelo = My.Application.CommandLineArgs.Item(iLoop + 1).ToString.ToUpper
                    End If
                    If sItem = "-M" Then
                        sArquivoMappings = My.Application.CommandLineArgs.Item(iLoop + 1).ToString.ToUpper
                    End If
                    If sItem = "-P" Then
                        sPathFinal = My.Application.CommandLineArgs.Item(iLoop + 1).ToString.ToUpper
                    End If
                    If sItem = "-F" Then
                        sFile = My.Application.CommandLineArgs.Item(iLoop + 1).ToString.ToUpper
                    End If
                    If sItem = "-S" Then
                        sFileSDM = My.Application.CommandLineArgs.Item(iLoop + 1).ToString.ToUpper
                    End If
                    If sItem = "-L" Then
                        sAtributoLoop = My.Application.CommandLineArgs.Item(iLoop + 1).ToString
                    End If
                Next

            End If
        Else
            fHelp()
            Exit Sub
        End If

        'Validando se todas os parametros essencias foram informados
        If sArquivoDestino.Length = 0 Then
            log.logErro("Voce precisa informar um arquivo de destino com o parametro -A")
            iSai = 1
        End If
        If sArquivoModelo.Length = 0 Then
            log.logErro("Voce precisa informar um arquivo de modelo com o parametro -T")
            iSai = 1
        End If
        If sArquivoMappings.Length = 0 Then
            log.logErro("Voce precisa informar um arquivo de mapeamentos com o parametro -M")
            iSai = 1
        End If
        If sPathFinal.Length = 0 Then
            log.logErro("Voce precisa informar um path de destino com o parametro -P")
            iSai = 1
        End If
        If sFileSDM.Length = 0 Then
            log.logErro("Voce precisa informar o arquivo de mail do Service Desk")
            iSai = 1
        End If

        If iSai = 1 Then
            Exit Sub
        End If

        'coloca uma data no nome do arquivo de destino, evitando substituições
        If sArquivoDestino.IndexOf(".") > 0 Then
            sArquivoDestino = sArquivoDestino.Insert(sArquivoDestino.IndexOf("."), fDater())
        Else
            sArquivoDestino = sArquivoDestino & fDater()
        End If

        'dumper é a classe que efetua o dump do ticket em um arquivo texto, aqui o instanciamos e indicamos
        'qual é o arquivo de mensagem do SDM que iremos processar para tomar como base
        log.logLine(bDebug, "Abrindo arquivo de mensagem '" & sFileSDM & "'")
        oDumper = New dumper(sFileSDM, bDebug)

        'Caso o arquivo do SDM tenha sido aberto com sucesso, inicia a brincadeira
        If oDumper.arquivoAberto() Then

            'Carrega o template do arquivo a ser gerado
            log.logLine(bDebug, "Abrindo arquivo de modelo'" & sArquivoModelo & "'")
            oDumper.carregaTemplate(sArquivoModelo)

            'Caso consiga abrir o template com sucesso, dá continuidade
            If oDumper.templateAberto() Then

                'Associa o atributo de Loop da classe ao atributo passado via linha de comando
                Try
                    oDumper.atributoLoop = sAtributoLoop
                Catch ex As Exception
                    log.logErro(ex.Message)
                    Exit Sub
                End Try


                'Agora, a partir do arquivo de mapeamentos, efetua as substituições e retorna e grava os valores no
                'arquivo de destino
                log.logLine(bDebug, "Abrindo arquivo de mapeamento'" & sArquivoMappings & "'")
                If oDumper.substituiValores(sArquivoMappings) Then

                    'Salva o arquivo de destino no path informado
                    log.logLine(bDebug, "Salvando arquivo '" & sArquivoDestino & "' em '" & sPathFinal & "'")
                    oDumper.salvaArquivo(sPathFinal, sArquivoDestino)
                    iRemove = 1
                Else
                    log.logErro("Não foi possível efetuar a substituição de valores no arquivo " & sArquivoDestino)
                End If
            Else
                log.logErro("Não foi possível abrir o arquivo de template")
            End If
            Else
                log.logErro("Não foi possível processar o arquivo de mail do Service Desk")
            End If

        oDumper.dispose()

        'Ao final, removemos o arquivo de mail do SDM, conforme manda o figurino
        If iRemove Then
            'System.IO.File.Delete(sFileSDM)
        End If


    End Sub


    Sub fHelp()
        Dim s As String = ""
        Console.WriteLine("Aplicativo em linha de comando para criacao de arquivo texto com dump de um objeto do Service Desk manager")
        Console.WriteLine("")
        Console.WriteLine("Utilize:")
        Console.WriteLine("")
        Console.WriteLine("z_dumpTicket <-M 'modelo'> <-A 'Nome'> [-D] [-C]")
        Console.WriteLine("")
        Console.WriteLine("-? = Exibe este Help")
        Console.WriteLine("-C = Exibe tela de configuracao - Esta opcao deve ser usada individualmente")
        Console.WriteLine("-D = Debug Mode")
        Console.WriteLine("-A = Prefixo do arquivo a ser criado")
        Console.WriteLine("-T = Caminho e Arquivo TXT com o modelo para criacao do arquivo")
        Console.WriteLine("-M = Caminho e Arquivo TXT com os mapeamentos vars X campos do SDM")
        Console.WriteLine("-P = Path onde deve ser criado o TXT de destino")
        Console.WriteLine("-L = Atributo que contem a lista de ICs no modelo <atributo_do_ticket>.<atributo_a_obter_no_IC>. P. ex. asset.name")
        Console.WriteLine("")
        Console.WriteLine("")
        Console.WriteLine("Exemplo de uso:")
        Console.WriteLine("")
        Console.WriteLine("z_dumpTicket -M arquivo.xml -P C:\temp -A arquivo_final.xml ")
        Console.WriteLine("")
        Console.WriteLine("z_dumpTicket -C")
        Console.WriteLine("")

    End Sub



End Module
