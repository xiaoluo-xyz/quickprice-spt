using System;
using System.Collections.Generic;

namespace QuickPrice.Utils
{
    [Flags]
    internal enum ArmorRegion
    {
        None = 0,
        Front = 1,
        Back = 2,
        Left = 4,
        Right = 8
    }

    internal readonly struct ArmorProtectionPart
    {
        public ArmorProtectionPart(ArmorRegion regions, double currentResistance, double originalResistance)
        {
            Regions = regions;
            CurrentResistance = currentResistance;
            OriginalResistance = originalResistance;
        }

        public ArmorRegion Regions { get; }
        public double CurrentResistance { get; }
        public double OriginalResistance { get; }
    }

    internal readonly struct ArmorProtection
    {
        public ArmorProtection(int currentClass, int originalClass)
        {
            CurrentClass = currentClass;
            OriginalClass = originalClass;
        }

        public int CurrentClass { get; }
        public int OriginalClass { get; }
    }

    internal static class ArmorProtectionCalculation
    {
        private static readonly ArmorRegion[] Regions =
            { ArmorRegion.Front, ArmorRegion.Back, ArmorRegion.Left, ArmorRegion.Right };
        private static readonly double[] Weights = { 5, 3, 1, 1 };

        public static ArmorProtection Calculate(IEnumerable<ArmorProtectionPart> parts,
            ArmorRegion supportedRegions, IReadOnlyList<double> thresholds)
        {
            var current = new double[Regions.Length];
            var original = new double[Regions.Length];
            double singleCurrent = 0;
            double singleOriginal = 0;

            foreach (var part in parts)
            {
                supportedRegions |= part.Regions;
                singleCurrent = Math.Max(singleCurrent, part.CurrentResistance);
                singleOriginal = Math.Max(singleOriginal, part.OriginalResistance);
                for (int i = 0; i < Regions.Length; i++)
                {
                    if ((part.Regions & Regions[i]) == 0)
                        continue;

                    // Layers are not additive: retain the strongest layer in each direction.
                    current[i] = Math.Max(current[i], part.CurrentResistance);
                    original[i] = Math.Max(original[i], part.OriginalResistance);
                }
            }

            double currentResistance = 0;
            double originalResistance = 0;
            double totalWeight = 0;
            for (int i = 0; i < Regions.Length; i++)
            {
                if ((supportedRegions & Regions[i]) == 0)
                    continue;

                currentResistance += current[i] * Weights[i];
                originalResistance += original[i] * Weights[i];
                totalWeight += Weights[i];
            }

            if (totalWeight > 0)
            {
                currentResistance /= totalWeight;
                originalResistance /= totalWeight;
            }
            else
            {
                // Loose plates and head protection do not use torso direction weights.
                currentResistance = singleCurrent;
                originalResistance = singleOriginal;
            }

            return new ArmorProtection(GetClass(currentResistance, thresholds),
                GetClass(originalResistance, thresholds));
        }

        private static int GetClass(double resistance, IReadOnlyList<double> thresholds)
        {
            for (int level = thresholds.Count - 1; level > 0; level--)
            {
                if (resistance >= thresholds[level])
                    return level;
            }

            return 0;
        }
    }
}
