using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.ServingSuggestions.Domain;

namespace Nop.Plugin.Misc.ServingSuggestions.Data.Mapping.Builders;

/// <summary>
/// Represents a serving suggestion step entity builder
/// </summary>
public class ServingSuggestionStepBuilder : NopEntityBuilder<ServingSuggestionStep>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ServingSuggestionStep.Text)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ServingSuggestionStep.ServingSuggestionId)).AsInt32().ForeignKey<ServingSuggestion>();
    }
}
