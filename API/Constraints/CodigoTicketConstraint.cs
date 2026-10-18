using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace API.Constraints
{
    /// <summary>
    /// Restricción de ruta personalizada para validar el formato de código de ticket de la Variante 0: TK-AAAA-NNNN.
    /// Si el valor no cumple el patrón, la ruta no coincide y ASP.NET Core responde 404 antes de invocar el controlador.
    /// </summary>
    public class CodigoTicketConstraint : IRouteConstraint
    {
        private static readonly Regex _regex = new(@"^TK-\d{4}-\d{4}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool Match(
            HttpContext? httpContext,
            IRouter? route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (values.TryGetValue(routeKey, out var value) && value is not null)
            {
                var stringValue = Convert.ToString(value);
                return !string.IsNullOrEmpty(stringValue) && _regex.IsMatch(stringValue);
            }

            return false;
        }
    }
}
