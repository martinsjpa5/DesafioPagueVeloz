
namespace Application.Dtos.Base
{
    public class ResultPattern
    {
        public bool Sucesso { get; private set; }
        public List<string> Erros { get; private set; } = [];


        private ResultPattern()
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

        public static ResultPattern ErroBuilder(string erro)
        {
            if (string.IsNullOrWhiteSpace(erro) is true)
                throw new ArgumentException("Deve ser passado um erro");
            return new ResultPattern() { Erros = [erro] };
        }
        public static ResultPattern ErroBuilder(List<string> erros)
        {
            if (erros.Count == 0)
                throw new ArgumentException("Deve ser passado ao menos um erro dentro da lista");

            if (erros.Any(erro => string.IsNullOrWhiteSpace(erro)))
                throw new ArgumentException("Deve ser passado um erro");
            return new ResultPattern() { Erros = erros };
        }
        public static ResultPattern SucessoBuilder()
        {
            return new ResultPattern { Sucesso = true, };
        }
    }
}
