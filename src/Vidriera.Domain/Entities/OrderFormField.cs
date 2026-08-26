namespace Vidriera.Domain.Entities;

public class OrderFormField
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Label { get; set; } = null!;
    public virtual string FieldType { get; set; } = null!;
    public virtual bool IsRequired { get; set; }
    public virtual int SortOrder { get; set; }
}
