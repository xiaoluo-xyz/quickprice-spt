using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using QuickPrice.Utils;
using UnityEngine.UI;

internal static class Program
{
    private static int passed;
    private const ArmorRegion All = ArmorRegion.Front | ArmorRegion.Back | ArmorRegion.Left | ArmorRegion.Right;
    private static readonly double[] Thresholds = { 1, 10, 20, 30, 40, 50, 60 };

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
            Console.WriteLine($"Passed {passed} armor protection checks against the installed game DLL.");
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
        var config = (BackendConfigSettingsClass)FormatterServices.GetUninitializedObject(typeof(BackendConfigSettingsClass));
        config.Armor = new BackendConfigSettingsClass.GClass1732
        {
            ArmorClass = Thresholds.Select(value => new BackendConfigSettingsClass.GClass1732.GClass1733
                { Resistance = (int)value }).ToArray()
        };
        Singleton<BackendConfigSettingsClass>.Create(config);

        Check("front/back IV, sides VI", 4, 4, All,
            Part(ArmorRegion.Front, 40), Part(ArmorRegion.Back, 40),
            Part(ArmorRegion.Left, 60), Part(ArmorRegion.Right, 60));
        Check("front carries more weight than back", 5, 5, ArmorRegion.Front | ArmorRegion.Back,
            Part(ArmorRegion.Front, 60), Part(ArmorRegion.Back, 40));
        Check("back cannot outweigh front", 4, 4, ArmorRegion.Front | ArmorRegion.Back,
            Part(ArmorRegion.Front, 40), Part(ArmorRegion.Back, 60));
        Check("missing supported front remains in denominator", 1, 1, All,
            Part(ArmorRegion.Back, 40), Part(ArmorRegion.Left, 20), Part(ArmorRegion.Right, 20));
        Check("unsupported sides do not dilute front/back", 4, 4, ArmorRegion.Front | ArmorRegion.Back,
            Part(ArmorRegion.Front, 40), Part(ArmorRegion.Back, 40));
        Check("soft armor covers missing front", 3, 3, All,
            Part(All, 20), Part(ArmorRegion.Back, 60));
        Check("soft armor is not added to plates", 4, 4, All,
            Part(All, 20), Part(All, 40));
        Check("soft armor still protects behind destroyed plate", 2, 4, All,
            Part(All, 20), new ArmorProtectionPart(All, 0, 40));
        Check("overlapping colliders do not increase weight", 4, 4, All,
            Part(ArmorRegion.Front | ArmorRegion.Back, 40),
            Part(ArmorRegion.Left | ArmorRegion.Right, 60), Part(ArmorRegion.Left, 60));
        Check("empty armor", 0, 0, All);
        Check("head protection has no torso penalty", 4, 4, ArmorRegion.None, Part(ArmorRegion.None, 40));

        foreach (int level in Enumerable.Range(1, 6))
            Check($"full durability class {level}", level, level, ArmorRegion.None, GamePart(level, 100));
        Check("VI at half durability", 5, 6, ArmorRegion.None, GamePart(6, 50));
        Check("VI at quarter durability", 4, 6, ArmorRegion.None, GamePart(6, 25));
        Check("VI at ten percent durability", 2, 6, ArmorRegion.None, GamePart(6, 10));
        Check("destroyed VI retains original badge", 0, 6, ArmorRegion.None,
            new ArmorProtectionPart(ArmorRegion.None, 0, 60));
        Check("durability changes on the same item", 4, 6, ArmorRegion.None, GamePart(6, 25));
        Check("replacement plate changes original badge", 3, 4, ArmorRegion.None, GamePart(4, 50));
        var custom = ArmorProtectionCalculation.Calculate(new[] { Part(ArmorRegion.None, 40) },
            ArmorRegion.None, new double[] { 1, 8, 16, 24, 32, 40, 48 });
        Equal("runtime resistance thresholds", 5, custom.CurrentClass);

        Equal("both plate families front/back", ArmorRegion.Front | ArmorRegion.Back,
            ArmorHelper.GetRegions(EArmorPlateCollider.Plate_Korund_chest | EArmorPlateCollider.Plate_6B13_back,
                Array.Empty<EBodyPartColliderType>()));
        Equal("multiple left colliders form one side", ArmorRegion.Left,
            ArmorHelper.GetRegions(EArmorPlateCollider.Plate_Granit_SSAPI_side_left_high |
                EArmorPlateCollider.Plate_Granit_SSAPI_side_left_low | EArmorPlateCollider.Plate_Korund_side_left_high,
                Array.Empty<EBodyPartColliderType>()));
        Equal("body armor direction data", All, ArmorHelper.GetRegions(0, new[]
        {
            EBodyPartColliderType.RibcageUp, EBodyPartColliderType.SpineTop,
            EBodyPartColliderType.LeftSideChestDown, EBodyPartColliderType.RightSideChestUp
        }));
        Equal("head/arms are not torso directions", ArmorRegion.None, ArmorHelper.GetRegions(0, new[]
            { EBodyPartColliderType.ParietalHead, EBodyPartColliderType.Eyes, EBodyPartColliderType.LeftUpperArm }));
        Equal("native badge field", typeof(Image), typeof(ItemViewStats)
            .GetField("_armorClassIcon", BindingFlags.NonPublic | BindingFlags.Instance).FieldType);
        Equal("native badge refresh method", typeof(void), typeof(GridItemView)
            .GetMethod(nameof(GridItemView.UpdateInfo)).ReturnType);
        Equal("native cached background field", typeof(UnityEngine.Color), typeof(ItemView)
            .GetField("OriginalBackgroundColor", BindingFlags.NonPublic | BindingFlags.Instance).FieldType);
        Equal("native animation refresh method", typeof(void), typeof(EFT.UI.ItemViewAnimation)
            .GetMethod(nameof(EFT.UI.ItemViewAnimation.Init)).ReturnType);

        var plate = new ArmorPlateItemClass("000000000000000000000001", new ArmorPlateTemplateClass
        {
            armorClass = 6, Durability = 100, MaxDurability = 100, Slots = Array.Empty<Slot>()
        });
        Equal("real plate current rating", (int?)6, ArmorHelper.GetArmorClass(plate));
        Equal("real plate original rating", (int?)6, ArmorHelper.GetOriginalArmorClass(plate));
        plate.Repairable.Durability = 25;
        plate.Repairable.MaxDurability = 25;
        Equal("repair ceiling does not reset effective durability", (int?)4, ArmorHelper.GetArmorClass(plate));
        Equal("damaged real plate original rating", (int?)6, ArmorHelper.GetOriginalArmorClass(plate));
        plate.Repairable.Durability = 0;
        Equal("real destroyed plate is zero", (int?)0, ArmorHelper.GetArmorClass(plate));
        Equal("real destroyed plate retains original rating", (int?)6, ArmorHelper.GetOriginalArmorClass(plate));

        var carrier = new ArmorItemClass("000000000000000000000010", new ArmorTemplateClass
        {
            Slots = new Slot[]
            {
                MakeSlot("front", EArmorPlateCollider.Plate_Granit_SAPI_chest),
                MakeSlot("back", EArmorPlateCollider.Plate_Granit_SAPI_back),
                MakeSlot("left", EArmorPlateCollider.Plate_Granit_SSAPI_side_left_high),
                MakeSlot("right", EArmorPlateCollider.Plate_Granit_SSAPI_side_right_high)
            }
        });
        carrier.Slots[0].ChangeContainedItemDirectly(MakePlate(4, "000000000000000000000011"));
        carrier.Slots[1].ChangeContainedItemDirectly(MakePlate(4, "000000000000000000000012"));
        carrier.Slots[2].ChangeContainedItemDirectly(MakePlate(6, "000000000000000000000013"));
        carrier.Slots[3].ChangeContainedItemDirectly(MakePlate(6, "000000000000000000000014"));
        Equal("real carrier weighted current rating", (int?)4, ArmorHelper.GetArmorClass(carrier));
        Equal("real carrier weighted original badge", (int?)4, ArmorHelper.GetOriginalArmorClass(carrier));
        ((ArmorPlateItemClass)carrier.Slots[0].ContainedItem).Repairable.Durability = 20;
        Equal("real carrier front durability changes color", (int?)3, ArmorHelper.GetArmorClass(carrier));
        Equal("real carrier damage does not change original badge", (int?)4, ArmorHelper.GetOriginalArmorClass(carrier));
        carrier.Slots[0].ChangeContainedItemDirectly(null);
        Equal("real carrier missing front", (int?)2, ArmorHelper.GetArmorClass(carrier));
        carrier.Slots[0].ChangeContainedItemDirectly(MakePlate(6, "000000000000000000000015"));
        Equal("real carrier replacement changes badge", (int?)5, ArmorHelper.GetOriginalArmorClass(carrier));
    }

    private static ArmorProtectionPart Part(ArmorRegion regions, double resistance) =>
        new ArmorProtectionPart(regions, resistance, resistance);

    private static Slot MakeSlot(string id, EArmorPlateCollider region) => new GClass3125(id,
        new[] { new ItemFilter { armorPlateColliders = new[] { region } } }, false, EParentMergeType.Merge);

    private static ArmorPlateItemClass MakePlate(int level, string id)
    {
        var template = new ArmorPlateTemplateClass
        {
            armorClass = level, Durability = 100, MaxDurability = 100, Slots = Array.Empty<Slot>()
        };
        var plate = new ArmorPlateItemClass(id, template);
        // Populate coverage after construction; fixtures do not initialize the game's localization UI.
        template.armorPlateColliders = Enum.GetValues(typeof(EArmorPlateCollider)).Cast<EArmorPlateCollider>().ToArray();
        return plate;
    }

    private static ArmorProtectionPart GamePart(int level, float durability)
    {
        double resistance = GClass659.RealResistance(durability, 100, level, 0).RealResistance;
        return new ArmorProtectionPart(ArmorRegion.None, Math.Min(resistance, Thresholds[level]), Thresholds[level]);
    }

    private static void Check(string name, int current, int original, ArmorRegion supported,
        params ArmorProtectionPart[] parts)
    {
        var result = ArmorProtectionCalculation.Calculate(parts, supported, Thresholds);
        if (result.CurrentClass != current || result.OriginalClass != original)
            throw new Exception($"{name}: expected {current}/{original}, got {result.CurrentClass}/{result.OriginalClass}");
        passed++;
    }

    private static void Equal<T>(string name, T expected, T actual)
    {
        if (!Equals(expected, actual))
            throw new Exception($"{name}: expected {expected}, got {actual}");
        passed++;
    }
}
