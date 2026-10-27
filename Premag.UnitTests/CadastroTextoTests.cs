using FluentAssertions;
using Premag.Core;
using Premag.Core.Exceptions;

namespace Premag.UnitTests;

public class CadastroTextoTests
{
    [Fact]
    public void NomeMotivo_TrimEObrigatorio()
    {
        CadastroTexto.NomeMotivo("  Falta de material  ").Should().Be("Falta de material");
        var vazio = () => CadastroTexto.NomeMotivo("  ");
        vazio.Should().Throw<RegraNegocioException>().Where(e => e.Codigo == "DADOS_INVALIDOS");
    }

    [Fact]
    public void CodigoExterno_VazioViraNulo_ERespeita40()
    {
        CadastroTexto.CodigoExterno("  ").Should().BeNull();
        CadastroTexto.CodigoExterno(" 12345678901 ").Should().Be("12345678901");
        var longo = () => CadastroTexto.CodigoExterno(new string('1', 41));
        longo.Should().Throw<RegraNegocioException>().Where(e => e.Codigo == "CODIGO_INVALIDO");
    }

    [Fact]
    public void SenhaNova_ExigeOitoCaracteres()
    {
        CadastroTexto.SenhaNova("  abcdefgh  ").Should().Be("abcdefgh");
        var curta = () => CadastroTexto.SenhaNova("1234567");
        curta.Should().Throw<RegraNegocioException>().Where(e => e.Codigo == "SENHA_FRACA");
    }
}
