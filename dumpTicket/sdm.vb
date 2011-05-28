Imports Microsoft.VisualBasic
Imports System.Xml
Imports dumpTicket

Public Class sdm

    Private intSid As Integer = 0
    Private sd As sdws.USD_WebService
    Private sErro As String = ""

    'O construtur instancia a classe e já conecta ao webservice
    Sub New(ByVal sSDM_WSDL As String, ByVal usuario As String, ByVal senha As String)

        Try
            sd = New sdws.USD_WebService()
            intSid = sd.Login(usuario, senha)
        Catch ex As Exception
            log.logErro(ex.Message)
            sErro = ex.Message
        End Try

    End Sub

    'Um destruidor aqui também
    Sub dispose()

        If intSid > 0 Then
            sd.Logout(intSid)
        End If
        sd.Dispose()

    End Sub

    'Determina se o login foi ou não efetuado com sucesso
    Public ReadOnly Property conectado() As Integer
        Get
            If intSid > 0 Then
                conectado = 1
            Else
                conectado = 0
            End If
        End Get
    End Property

    'Session ID, apenas para conhecimento
    Public ReadOnly Property sessionID() As Integer
        Get
            sessionID = intSid
        End Get
    End Property

    'Mensagen de erro na criação do objeto, caso haja
    Public ReadOnly Property erro() As String
        Get
            erro = sErro
        End Get
    End Property

    Public Function getObject(ByVal persidObjeto As String, ByVal atributos() As String) As Hashtable
        Dim sRetorno As String = ""

        Try
            sRetorno = sd.GetObjectValues(intSid, persidObjeto, atributos).OuterXml
        Catch ex As Exception
            log.logErro("Nao foi possível efetuar a consulta! - " & ex.Message)
        End Try
        Return retornaValores(sRetorno)

    End Function

    'essa funcao recupera do XML vindo do SD os valores dos atributos do registro
    'serve para quando retornar apenas um registro.
    Private Function retornaValores(ByVal xml_retorn_sdm As String) As Hashtable
        Dim docXml As New XmlDocument
        Dim valores As New Hashtable
        Dim sChave, sValor As String

        Try
            docXml.LoadXml(xml_retorn_sdm)

            'criando o iterador para varrer o conteudo do XML
            Dim Navegador As XPath.XPathNavigator
            Dim Iterador As XPath.XPathNodeIterator

            Navegador = docXml.CreateNavigator()

            Iterador = Navegador.Select("/UDSObject/Attributes/*")


            'Como o SDM usa aquele modelo nojento de pares com nome do atributo x valor, vamos iterar
            'nesses pares e criar uma coleção
            While Iterador.MoveNext

                'Carrega o nome do atributo
                sChave = Iterador.Current.Value
                sValor = Iterador.Current.Name
                valores.Add(sValor, sChave)

            End While

        Catch ex As Exception
            log.logErro("Não foi possível iterar pelo XML de resposta do SDM - " & ex.Message)
        End Try



        Return valores
    End Function

    'Otem uma lista de nomes de objetos associados a um persid de uma LREL
    Public Function retornaListaNomesIcs(ByVal persidObjetoPai As String, ByVal attributoLrel As String, ByVal atributoParaRecuperar As String) As Array
        Dim oListRet As sdws.ListReturn
        Dim xNode As System.Xml.XmlNode
        Dim iListHandle As Integer
        Dim iListLength As Integer
        Dim iLoop As Integer = 0
        Dim aHandles(0) As Integer
        Dim aNomes As Array
        Dim aRetorno(0) As String
        Dim aAttributos(0) As String
        Dim sRpid As String
        Dim wc As String

        aRetorno(0) = "vazio"
        wc = "lpid = '" & persidObjetoPai & "' and lattr = '" & attributoLrel & "'"
        aAttributos(0) = atributoParaRecuperar

        Try
            oListRet = sd.DoQuery(intSid, "lrel1", wc)
            iListLength = oListRet.ListLength
            iListHandle = oListRet.ListHandle

            If iListLength > 0 Then
                aNomes = retornaRpidsQuery(iListHandle, iListLength)
                ReDim aRetorno(aNomes.Length - 1)
                For iLoop = 0 To aNomes.Length - 1
                    sRpid = aNomes(iLoop)
                    xNode = sd.GetObjectValues(intSid, sRpid, aAttributos)
                    aRetorno(iLoop) = xNode.ChildNodes(1).InnerText
                Next
            End If

            retornaListaNomesIcs = aRetorno
            aHandles(0) = iListHandle
            sd.FreeListHandles(intSid, aHandles)

        Catch ex As Exception

            log.logErro("Não foi possível buscar lista de ICs: " & ex.Message)

        End Try


    End Function

    'Retorna uma array com uma lista de rpids de uma ListHandle gerada por um DoQuery de uma LREL
    Function retornaRpidsQuery(ByVal iListHandle As Integer, ByVal iListLength As Integer) As Array

        Dim iPaginas, i, iValores, iInicio, iFim As Integer
        Dim asValores(), aValoresTemp(), asAtributos(0) As String
        Dim sRetorno As System.Xml.XmlElement
        Dim docXmllListaValores As New XmlDocument

        ReDim asValores(iListLength - 1)
        iValores = 0
        asAtributos(0) = "rpid"

        'entramos no laco apenas se existem itens na lista
        If iListLength > 0 Then

            'paginando em 250 registros, limite maximo de retorno do SDM
            iPaginas = (iListLength / 250)
            For i = 0 To iPaginas

                'identificando limite inferior e superior da lista
                iInicio = (i * 250)
                If iInicio + 250 >= iListLength Then
                    iFim = iListLength - 1
                Else
                    iFim = iInicio + 250
                End If

                If iInicio < iListLength Then

                    'retornando o objeto XML com a lista de elementos
                    sRetorno = sd.GetListValues(intSid, iListHandle, iInicio, iFim, asAtributos)

                    aValoresTemp = retornaValoresQuery(sRetorno)
                    aValoresTemp.CopyTo(asValores, i * 250)

                End If

            Next

        End If

        Return asValores

    End Function

    'essa funcao processa o XML oriundo de um doQuery, para retornar o valor de um atributo
    Private Function retornaValoresQuery(ByVal conteudo As System.Xml.XmlElement) As Array
        Dim retornoXml As New Xml.XmlDocument
        Dim docXml As New XmlDocument
        Dim valores() As String

        'criando o iterador para varrer o conteudo do XML
        Dim Navegador As XPath.XPathNavigator
        Dim Iterador As XPath.XPathNodeIterator

        Navegador = conteudo.CreateNavigator()

        Iterador = Navegador.Select("/UDSObject/Attributes/*")



        Dim contador As Integer
        contador = 0

        While Iterador.MoveNext
            Iterador.Current.MoveToFirstChild()
            Iterador.Current.MoveToNext()
            ReDim Preserve valores(contador)

            valores(contador) = Iterador.Current.Value
            contador = contador + 1
        End While


        Return valores
    End Function



End Class


