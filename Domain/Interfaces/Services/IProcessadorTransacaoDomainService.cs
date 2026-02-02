using Domain.Base;
using Domain.Entities;

namespace Domain.Interfaces.Services
{
    public interface IProcessadorTransacaoDomainService
    {
        DomainPattern Processar(Transacao transacao);
    }
}
