using Nop.Core;

namespace Nop.Plugin.Misc.ProductionLabels.Domain;

/// <summary>
/// Represents one production run of a product: batch code, production date, best-before date, quantity.
/// Rows are immutable once created (a mistake is corrected by creating a new row, not editing the old
/// one) and are locked against deletion once a label has been generated from them - see
/// <see cref="Services.IProductionBatchService.DeleteProductionBatchAsync"/>.
/// </summary>
public class ProductionBatch : BaseEntity
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the system-generated batch code (format: yyyyMMdd-NNN)
    /// </summary>
    public string BatchCode { get; set; }

    /// <summary>
    /// Gets or sets the production date (UTC)
    /// </summary>
    public DateTime ProductionDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the best-before date (UTC)
    /// </summary>
    public DateTime BestBeforeDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the quantity produced
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the date and time (UTC) a label was last generated for this batch, or null if no
    /// label has ever been generated - once set, the batch can no longer be deleted
    /// </summary>
    public DateTime? LabelGeneratedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of instance creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}
