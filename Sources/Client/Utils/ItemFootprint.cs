using EFT.InventoryLogic;

namespace QuickPrice.Utils
{
    internal static class ItemFootprint
    {
        public static int GetSlots(Item item)
        {
            var size = item.CalculateCellSize();
            return size.X * size.Y;
        }
    }
}
