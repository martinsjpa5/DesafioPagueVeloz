
namespace Application.Dtos.Base
{
    public class ResultPatternGeneric<T>
    {
        
        public bool Sucesso { get; private set; }
        public List<string> Erros { get; private set; } = [];
        public T Data { get; set; }


        private ResultPatternGeneric()
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

        public static ResultPatternGeneric<T> ErroBuilder(string erro)
        {
            if (string.IsNullOrWhiteSpace(erro) is true)
                throw new ArgumentException("Deve ser passado um erro");
            return new ResultPatternGeneric<T>() { Erros = [erro] };
        }
        public static ResultPatternGeneric<T> ErroBuilder(List<string> erros)
        {
            if (erros.Count == 0)
                throw new ArgumentException("Deve ser passado ao menos um erro dentro da lista");

            if (erros.Any(erro => string.IsNullOrWhiteSpace(erro)))
                throw new ArgumentException("Deve ser passado um erro");
            return new ResultPatternGeneric<T>() { Erros = erros };
        }
        public static ResultPatternGeneric<T> SucessoBuilder(T Data)
        {
            return new ResultPatternGeneric<T> { Sucesso = true, Data = Data };
        }
    }
}
