using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Interfaces.Services;
using Domain.Entities;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.EntityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly IEfBaseRepository _efBaseRepository;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config, IEfBaseRepository efBaseRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _efBaseRepository = efBaseRepository;
        }

        public async Task<ResultPatternGeneric<string>> LogarAsync(LogarRequest request)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);

            const string msgErroCredenciais = "Credenciais não encontradas";

            if (user == null)
            {
                return ResultPatternGeneric<string>.ErroBuilder(msgErroCredenciais);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Senha, false);

            if (!result.Succeeded)
            {
                return ResultPatternGeneric<string>.ErroBuilder(msgErroCredenciais);
            }

            var token = CriarToken(user);

            return ResultPatternGeneric<string>.SucessoBuilder(token);
        }

        public async Task<ResultPattern> RegistrarAsync(RegistrarUsuarioRequest request)
        {
            Cliente cliente = new();
            await _efBaseRepository.AdicionarEntidadeBaseAsync(cliente);
            await _efBaseRepository.SalvarAlteracoesAsync();

            ApplicationUser user = new()
            {
                UserName = request.Email,
                Email = request.Email,
                 EmailConfirmed = true,
                 ClienteId = cliente.Id
            };

            IdentityResult result = await _userManager.CreateAsync(user, request.Senha);

            if (!result.Succeeded)
            {
                List<string> erros = result.Errors.Select(x => x.Description).ToList();
                _efBaseRepository.DeletarEntidadeBase(cliente);
                await _efBaseRepository.SalvarAlteracoesAsync();
                return ResultPattern.ErroBuilder(erros);
            }

            return ResultPattern.SucessoBuilder();
        }

        private string CriarToken(ApplicationUser user)
        {
            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("clienteId", user.ClienteId.ToString())
            ];

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(
                _config["Jwt:Key"]
            ));

            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: DateTime.UtcNow.AddHours(6),
                claims: claims,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
