using Desafio.Api.Controllers;
using Desafio.Api.Infraestrutura;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Api.Tests;

public class HealthControllerTests
{
    [Fact]
    public async Task Health_deve_devolver_503_quando_banco_estiver_indisponivel()
    {
        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=indisponivel;Username=teste;Password=teste;Timeout=1")
            .Options;
        await using var db = new AppDbContext(opcoes);

        var resultado = await new HealthController(db).Obter(CancellationToken.None);

        var resposta = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, resposta.StatusCode);
    }
}
