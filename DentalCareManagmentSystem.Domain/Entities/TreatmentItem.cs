
using DentalCareManagmentSystem.Domain.Interfaces;

namespace DentalCareManagmentSystem.Domain.Entities;

public class TreatmentItem : IVisitable
{
    public Guid Id { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public Guid PriceListItemId { get; set; }

    public string? NameSnapshot { get; set; }
    public decimal PriceSnapshot { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => PriceSnapshot * Quantity;

    // مهم جداً: الـ Navigation Property اللي ناقصة عندك
    public virtual TreatmentPlan? TreatmentPlan { get; set; }

    public virtual PriceListItem? PriceListItem { get; set; }

    public void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }
}