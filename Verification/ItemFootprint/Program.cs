using System;
using System.IO;
using System.Reflection;
using EFT.InventoryLogic;
using QuickPrice.Utils;

internal static class Program
{
    private static int passed;

    private static int Main(string[] args)
    {
        string managed = Path.Combine(args[0], "EscapeFromTarkov_Data", "Managed");
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string path = Path.Combine(managed, new AssemblyName(request.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try
        {
            Run();
            Console.WriteLine($"Passed {passed} item footprint checks against the installed game DLL.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run()
    {
        Equal("weapon uses the compound footprint calculation", typeof(CompoundItem),
            typeof(Weapon).GetMethod(nameof(Weapon.CalculateCellSize)).DeclaringType);

        var item = new Item("000000000000000000000020", new ItemTemplate { Width = 2, Height = 3 });
        Equal("ordinary item dimensions", 6, ItemFootprint.GetSlots(item));

        var rifle = new CompoundItem("000000000000000000000021", new CompoundItemTemplateClass
        {
            Width = 2, Height = 1, MergesWithChildren = true,
            Slots = new[]
            {
                new Slot("mod_stock_000", Array.Empty<ItemFilter>(), false, EParentMergeType.Merge),
                new Slot("mod_magazine", Array.Empty<ItemFilter>(), false, EParentMergeType.Merge)
            }
        });
        Equal("bare receiver", 2, ItemFootprint.GetSlots(rifle));
        var stock = new CompoundItem("000000000000000000000022", new CompoundItemTemplateClass
        {
            Width = 1, Height = 1, MergesWithChildren = true,
            Slots = new[] { new Slot("mod_stock_extension", Array.Empty<ItemFilter>(), false, EParentMergeType.Merge) }
        });
        // Populate extensions after construction; fixtures need no localization singleton.
        stock.Template.ExtraSizeRight = 2;
        rifle.Slots[0].ChangeContainedItemDirectly(stock);
        Equal("stock adds width", 4, ItemFootprint.GetSlots(rifle));
        var magazine = new Item("000000000000000000000023", new ItemTemplate { Width = 1, Height = 1 });
        magazine.Template.ExtraSizeDown = 1;
        rifle.Slots[1].ChangeContainedItemDirectly(magazine);
        Equal("9A-91-style assembled footprint", 8, ItemFootprint.GetSlots(rifle));
        Equal("206.5k across eight cells", 25812.5, 206500.0 / ItemFootprint.GetSlots(rifle));
        var horizontal = rifle.CalculateRotatedSize(ItemRotation.Horizontal);
        var vertical = rifle.CalculateRotatedSize(ItemRotation.Vertical);
        Equal("horizontal footprint", 8, horizontal.X * horizontal.Y);
        Equal("rotation preserves cell count", 8, vertical.X * vertical.Y);

        var foldable = new FoldableComponent(rifle, new WeaponTemplate
            { FoldedSlot = "mod_stock_000", SizeReduceRight = 0 });
        rifle.Components.Add(foldable);
        foldable.Folded = true;
        Equal("folding removes the stock extension", 4, ItemFootprint.GetSlots(rifle));
        foldable.Folded = false;
        Equal("unfolding restores eight cells", 8, ItemFootprint.GetSlots(rifle));
        var extension = new Item("000000000000000000000024", new ItemTemplate { Width = 1, Height = 1 });
        extension.Template.ExtraSizeRight = 5;
        stock.Slots[0].ChangeContainedItemDirectly(extension);
        Equal("nested attachment expands footprint", 14, ItemFootprint.GetSlots(rifle));
        foldable.Folded = true;
        Equal("folded branch excludes nested attachments", 4, ItemFootprint.GetSlots(rifle));
        rifle.Slots[1].ChangeContainedItemDirectly(null);
        Equal("removing the magazine shrinks height", 2, ItemFootprint.GetSlots(rifle));
    }

    private static void Equal<T>(string name, T expected, T actual)
    {
        if (!Equals(expected, actual))
            throw new Exception($"{name}: expected {expected}, got {actual}");
        passed++;
    }
}
