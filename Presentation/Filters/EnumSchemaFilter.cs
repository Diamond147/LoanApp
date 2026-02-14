using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Filters
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // This line ensures we find the enum even if it's "LoanType?" (nullable)
            var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;

            if (type.IsEnum)
            {
                schema.Enum.Clear();
                schema.Type = "string";
                schema.Format = null;
                schema.Nullable = false;

                foreach (var name in Enum.GetNames(type))
                {
                    schema.Enum.Add(new OpenApiString(name));
                }
            }
        }
    }
}