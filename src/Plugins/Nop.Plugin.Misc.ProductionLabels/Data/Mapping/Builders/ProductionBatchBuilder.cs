using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.ProductionLabels.Domain;

namespace Nop.Plugin.Misc.ProductionLabels.Data.Mapping.Builders;

/// <summary>
/// Represents a production batch entity builder
/// </summary>
public class ProductionBatchBuilder : NopEntityBuilder<ProductionBatch>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductionBatch.ProductId)).AsInt32().ForeignKey<Product>()
            .WithColumn(nameof(ProductionBatch.BatchCode)).AsString(50).NotNullable();
    }
}
