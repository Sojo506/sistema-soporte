namespace Abtracciones.Modelos
{
    public enum TipoErrorOperacion
    {
        Ninguno,
        NoEncontrado,
        ReglaNegocio,
        Invalido
    }

    public class ResultadoOperacion<T>
    {
        public bool Exito { get; private init; }
        public T? Valor { get; private init; }
        public TipoErrorOperacion TipoError { get; private init; }
        public string? Mensaje { get; private init; }

        public static ResultadoOperacion<T> Ok(T valor) =>
            new() { Exito = true, Valor = valor, TipoError = TipoErrorOperacion.Ninguno };

        public static ResultadoOperacion<T> NoEncontrado(string mensaje = "El recurso solicitado no fue encontrado.") =>
            new() { Exito = false, TipoError = TipoErrorOperacion.NoEncontrado, Mensaje = mensaje };

        public static ResultadoOperacion<T> Conflicto(string mensaje) =>
            new() { Exito = false, TipoError = TipoErrorOperacion.ReglaNegocio, Mensaje = mensaje };

        public static ResultadoOperacion<T> Invalido(string mensaje) =>
            new() { Exito = false, TipoError = TipoErrorOperacion.Invalido, Mensaje = mensaje };
    }
}
