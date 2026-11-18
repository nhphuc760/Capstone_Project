using System;

[Serializable]
public abstract class ItemComponent
{
    /// <summary>Kiểm tra điều kiện dùng item (cooldown, đủ mana, ...).</summary>
    public virtual bool CanUse(ItemContext ctx) => true;

    /// <summary>Khi người chơi dùng item (chuột phải / phím tắt / consume).</summary>
    public virtual void OnUse(ItemContext ctx) { }

    /// <summary>Khi item được trang bị (Equipment). Thêm buff chỉ số ở đây.</summary>
    public virtual void OnEquip(ItemContext ctx) { }

    /// <summary>Khi item bị gỡ. Phải gỡ mọi thứ đã thêm trong OnEquip (dùng ctx.SourceKey).</summary>
    public virtual void OnUnequip(ItemContext ctx) { }

    /// <summary>Khi item vào inventory (nhặt). Dùng cho passive-in-bag hoặc quest tracking.</summary>
    public virtual void OnAcquired(ItemContext ctx) { }

    /// <summary>Khi item rời inventory (bỏ, dùng hết).</summary>
    public virtual void OnLost(ItemContext ctx) { }
    public virtual void Validate(ItemSO owner) { }
}
