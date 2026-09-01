using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.ServingSuggestions.Domain;

namespace Nop.Plugin.Misc.ServingSuggestions.Data.Mapping.Builders;

/// <summary>
/// Represents a serving suggestion entity builder
/// </summary>
public class ServingSuggestionBuilder : NopEntityBuilder<ServingSuggestion>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ServingSuggestion.Title)).AsString(400).NotNullable()
            .WithColumn(nameof(ServingSuggestion.Description)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ServingSuggestion.PictureId)).AsInt32().ForeignKey<Picture>()
            .WithColumn(nameof(ServingSuggestion.ProductId)).AsInt32().ForeignKey<Product>();
    }
}
