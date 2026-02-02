using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Base
{
    public class DomainPattern
    {

        public bool Sucesso { get; private set; }
        public List<string> Erros { get; private set; } = [];


        private DomainPattern()
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

        public static DomainPattern ErroBuilder(string erro)
        {
            if (string.IsNullOrWhiteSpace(erro) is true)
                throw new ArgumentException("Deve ser passado um erro");
            return new DomainPattern() { Erros = [erro] };
        }
        public static DomainPattern ErroBuilder(List<string> erros)
        {
            if (erros.Count == 0)
                throw new ArgumentException("Deve ser passado ao menos um erro dentro da lista");

            if (erros.Any(erro => string.IsNullOrWhiteSpace(erro)))
                throw new ArgumentException("Deve ser passado um erro");
            return new DomainPattern() { Erros = erros };
        }
        public static DomainPattern SucessoBuilder()
        {
            return new DomainPattern { Sucesso = true };
        }
    }
}