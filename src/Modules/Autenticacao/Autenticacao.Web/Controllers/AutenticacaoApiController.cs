using Autenticacao.Adapters.Controllers;
using Autenticacao.Adapters.Models.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autenticacao.Web.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/auth")]
public class AutenticacaoApiController : ControllerBase
{
    private readonly AutenticacaoController _caController;

    public AutenticacaoApiController(AutenticacaoController caController) => _caController = caController;

    /// <summary>
    /// Autentica o usuário administrador e retorna um token JWT válido por 1 hora.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _caController.Login(request);

        if (result.IsFailure)
            return Unauthorized(result.Error);

        return Ok(result.Value);
    }
}
