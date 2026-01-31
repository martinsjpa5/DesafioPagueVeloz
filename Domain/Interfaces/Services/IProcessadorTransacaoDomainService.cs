using Domain.Base;
using Domain.Entities;

namespace Domain.Interfaces.Services
{
    public interface IProcessadorTransacaoDomainService
    {
        DomainPatternGeneric<Transacao?> Processar(Transacao transacao);
    }
}
