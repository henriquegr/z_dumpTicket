Public Class configForm

    Private c As New cripto

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSalvar.Click

        Dim res As String

        My.Settings.dumpTicket_sdws_USD_WebService = txtWSDL.Text
        My.Settings.sSenhaSDM = c.encripta(txtSenha.Text)
        My.Settings.sUsuarioSDM = txtUsuario.Text

        My.Settings.Save()
        res = MsgBox("Configurações salvas com sucesso. Deseja sair?", MsgBoxStyle.YesNo, "Salvamento efetuado")
        If res = MsgBoxResult.Yes Then
            Me.Dispose()
        End If

    End Sub

    Private Sub config_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        txtWSDL.Text = My.Settings.dumpTicket_sdws_USD_WebService
        txtUsuario.Text = My.Settings.sUsuarioSDM
        txtSenha.Text = c.decripta(My.Settings.sSenhaSDM)


    End Sub

    Private Sub btnCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancelar.Click
        Me.Dispose()
    End Sub

    Private Sub btnTeste_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTeste.Click
        Dim sd As New sdm(txtWSDL.Text, txtUsuario.Text, txtSenha.Text)
        If sd.conectado Then
            MsgBox("Conexão com SDM efetuada com sucesso! Session ID='" & sd.sessionID & "'", MsgBoxStyle.Information, "Teste de Conexão")
        Else
            MsgBox("Não foi possível conectar ao SDM." & vbCrLf & sd.erro, MsgBoxStyle.Critical, "Teste de Conexão")
        End If
        sd.dispose()
    End Sub
End Class