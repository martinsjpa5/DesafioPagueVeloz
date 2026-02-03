using Application.Dtos.Requests;
using Application.Services;
using Domain.Entities;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.EntityFramework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Tests.Application.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IEfBaseRepository> _efBaseRepositoryMock;
        private readonly IConfiguration _config;

        public AuthServiceTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _signInManagerMock = CreateSignInManagerMock(_userManagerMock.Object);
            _efBaseRepositoryMock = new Mock<IEfBaseRepository>();

            var inMemory = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-test-key-1234567890-super-secret-test-key",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();
        }

        private AuthService CreateSut()
            => new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _config,
                _efBaseRepositoryMock.Object
            );

        [Fact]
        public async Task RegistrarAsync_QuandoCreateFalhar_DeveDeletarCliente_E_RetornarErros()
        {
            // Arrange
            var sut = CreateSut();

            var request = new RegistrarUsuarioRequest
            {
                Nome = "Matheus",
                Email = "teste@teste.com",
                Senha = "Senha@123"
            };

            Cliente? clienteCriado = null;

            _efBaseRepositoryMock
                .Setup(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Cliente>()))
                .Callback<object>(obj =>
                {
                    clienteCriado = (Cliente)obj;
                    clienteCriado.Id = clienteCriado.Id != 1 ? 1 : clienteCriado.Id;
                })
                .Returns(Task.FromResult(true));

            _efBaseRepositoryMock
                .Setup(x => x.SalvarAlteracoesAsync())
                .Returns(Task.FromResult(1));

            var identityErrors = new[]
            {
                new IdentityError { Description = "Erro 1" },
                new IdentityError { Description = "Erro 2" },
            };

            _userManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Senha))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var response = await sut.RegistrarAsync(request);

            // Assert
            Assert.False(response.Sucesso);
            Assert.NotNull(response.Erros);
            Assert.Equal(2, response.Erros.Count);
            Assert.Contains("Erro 1", response.Erros);
            Assert.Contains("Erro 2", response.Erros);

            _efBaseRepositoryMock.Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Cliente>()), Times.Once);
            _efBaseRepositoryMock.Verify(x => x.SalvarAlteracoesAsync(), Times.Exactly(2)); 

            _efBaseRepositoryMock.Verify(
                x => x.DeletarEntidadeBase(It.Is<Cliente>(c => clienteCriado != null && c == clienteCriado)),
                Times.Once
            );

            _userManagerMock.Verify(
                x => x.CreateAsync(It.Is<ApplicationUser>(u =>
                    u.Email == request.Email &&
                    u.UserName == request.Email &&
                    u.EmailConfirmed == true &&
                    clienteCriado != null && u.ClienteId == clienteCriado.Id
                ), request.Senha),
                Times.Once
            );
        }

        [Fact]
        public async Task RegistrarAsync_QuandoCreateOk_NaoDeveDeletarCliente_E_DeveRetornarSucesso()
        {
            // Arrange
            var sut = CreateSut();

            var request = new RegistrarUsuarioRequest
            {
                Nome = "Matheus",
                Email = "cliente@teste.com",
                Senha = "Senha@123"
            };

            Cliente? clienteCriado = null;

            _efBaseRepositoryMock
                .Setup(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Cliente>()))
                .Callback<object>(obj =>
                {
                    clienteCriado = (Cliente)obj;
                    clienteCriado.Id = clienteCriado.Id != null ? 1 : clienteCriado.Id;
                })
                .Returns(Task.FromResult(true));

            _efBaseRepositoryMock
                .Setup(x => x.SalvarAlteracoesAsync())
                .Returns(Task.FromResult(1));

            _userManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Senha))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var response = await sut.RegistrarAsync(request);

            // Assert
            Assert.True(response.Sucesso);
            Assert.Empty(response.Erros);

            _efBaseRepositoryMock.Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Cliente>()), Times.Once);
            _efBaseRepositoryMock.Verify(x => x.SalvarAlteracoesAsync(), Times.Once);

            _efBaseRepositoryMock.Verify(x => x.DeletarEntidadeBase(It.IsAny<Cliente>()), Times.Never);

            _userManagerMock.Verify(
                x => x.CreateAsync(It.Is<ApplicationUser>(u =>
                    u.Email == request.Email &&
                    u.UserName == request.Email &&
                    u.EmailConfirmed == true &&
                    clienteCriado != null && u.ClienteId == clienteCriado.Id
                ), request.Senha),
                Times.Once
            );
        }


        [Fact]
        public async Task LogarAsync_QuandoUsuarioNaoExiste_DeveRetornarErroCredenciaisNaoEncontradas()
        {
            // Arrange
            var sut = CreateSut();

            var request = new LogarRequest { Email = "naoexiste@teste.com", Senha = "Senha@123" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var response = await sut.LogarAsync(request);

            // Assert
            Assert.False(response.Sucesso);
            Assert.Single(response.Erros);
            Assert.Equal("Credenciais não encontradas", response.Erros[0]);
            Assert.Equal(default, response.Data);

            _signInManagerMock.Verify(
                x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), false),
                Times.Never
            );
        }

        [Fact]
        public async Task LogarAsync_QuandoSenhaInvalida_DeveRetornarErroCredenciaisNaoEncontradas()
        {
            // Arrange
            var sut = CreateSut();

            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "user@teste.com",
                UserName = "user@teste.com",
                ClienteId = 1
            };

            var request = new LogarRequest { Email = user.Email!, Senha = "SenhaErrada" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(user, request.Senha, false))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var response = await sut.LogarAsync(request);

            // Assert
            Assert.False(response.Sucesso);
            Assert.Single(response.Erros);
            Assert.Equal("Credenciais não encontradas", response.Erros[0]);
            Assert.Equal(default, response.Data);
        }

        [Fact]
        public async Task LogarAsync_QuandoOk_DeveRetornarTokenJWT_ComClaimsEsperadas()
        {
            // Arrange
            var sut = CreateSut();

            var clienteId = 1;

            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "user@teste.com",
                UserName = "user@teste.com",
                ClienteId = clienteId
            };

            var request = new LogarRequest { Email = user.Email!, Senha = "Senha@123" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(user, request.Senha, false))
                .ReturnsAsync(SignInResult.Success);

            // Act
            var response = await sut.LogarAsync(request);

            // Assert
            Assert.True(response.Sucesso);
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(response.Data);

            Assert.Equal("test-issuer", jwt.Issuer);
            Assert.Contains("test-audience", jwt.Audiences);

            Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);

            Assert.Equal(clienteId.ToString(), jwt.Claims.First(c => c.Type == "clienteId").Value);

            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            Assert.False(string.IsNullOrWhiteSpace(jti));

            Assert.True(jwt.ValidTo > DateTime.UtcNow.AddHours(5.9));
            Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddHours(6.1));
        }

        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApplicationUser>>().Object,
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );
        }

        private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(UserManager<ApplicationUser> userManager)
        {
            return new Mock<SignInManager<ApplicationUser>>(
                userManager,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<ApplicationUser>>().Object
            );
        }
    }
}
