namespace Presentation.Filters
{
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class SwaggerClearQueryParametersFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null) return;

            foreach (var parameter in operation.Parameters)
            {
                // Target query parameters of type string
                if (parameter.In == ParameterLocation.Query && parameter.Schema?.Type == "string")
                {
                    // Clear the default "string" value so unused inputs remain null/omitted in the HTTP request
                    parameter.Schema.Default = null;
                    parameter.Schema.Example = null;
                }
            }
        }
    }
}
