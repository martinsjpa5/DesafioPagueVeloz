using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Base
{
    public class DomainPatternGeneric<T>
    {

        public bool Sucesso { get; private set; }
        public List<string> Erros { get; private set; } = [];
        public T Data { get; set; }


        private DomainPatternGeneric()
        {
        }


        public void AdicionarErro(string erro)
        {
            if (string.IsNullOrWhiteSpace(erro)) return;

            Erros.Add(erro);
            Sucesso = false;
        }

        public void AdicionarErros(List<string> erros)
        {
            if (Erros.Count == 0) return;

            Erros.AddRange(erros);
            Sucesso = false;
        }

        public static DomainPatternGeneric<T> ErroBuilder(string erro)
        {
            if (string.IsNullOrWhiteSpace(erro) is true)
                throw new ArgumentException("Deve ser passado um erro");
            return new DomainPatternGeneric<T>() { Erros = [erro] };
        }
        public static DomainPatternGeneric<T> ErroBuilder(List<string> erros)
        {
            if (erros.Count == 0)
                throw new ArgumentException("Deve ser passado ao menos um erro dentro da lista");

            if (erros.Any(erro => string.IsNullOrWhiteSpace(erro)))
                throw new ArgumentException("Deve ser passado um erro");
            return new DomainPatternGeneric<T>() { Erros = erros };
        }
        public static DomainPatternGeneric<T> SucessoBuilder(T Data)
        {
            return new DomainPatternGeneric<T> { Sucesso = true, Data = Data };
        }
    }
}
