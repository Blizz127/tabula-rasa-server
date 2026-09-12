using System;
using System.Collections.Generic;

namespace Rasa.Data
{
    public readonly struct WeaponActionTiming
    {
        public int WindupMilliseconds { get; }
        public int? WindupAnimationFamilyId { get; }
        public int RecoveryMilliseconds { get; }
        public int? RecoveryAnimationFamilyId { get; }
        public int MaxRange { get; }
        public int ReuseMilliseconds { get; }
        public bool Preload { get; }
        public bool StartReuseTimerOnPerform { get; }

        public WeaponActionTiming(int windupMilliseconds, int? windupAnimationFamilyId,
            int recoveryMilliseconds, int? recoveryAnimationFamilyId, int maxRange,
            int reuseMilliseconds, bool preload, bool startReuseTimerOnPerform)
        {
            WindupMilliseconds = windupMilliseconds;
            WindupAnimationFamilyId = windupAnimationFamilyId;
            RecoveryMilliseconds = recoveryMilliseconds;
            RecoveryAnimationFamilyId = recoveryAnimationFamilyId;
            MaxRange = maxRange;
            ReuseMilliseconds = reuseMilliseconds;
            Preload = preload;
            StartReuseTimerOnPerform = startReuseTimerOnPerform;
        }
    }

    public static class WeaponActionData
    {
        // All original 1.16.5.0 actionArguments rows for stow129, draw130, reload134.
        // Reload windup is overridden by the weapon's server-supplied reload time.
        // See docs/weapon-actions-client-evidence.md for hashes and raw store offsets.
        private static readonly Dictionary<(ActionId, uint), WeaponActionTiming> Timings = new()
        {
            { (ActionId.WeaponStow, 1), new WeaponActionTiming(0, null, 1500, 498, 0, 0, false, true) },
            { (ActionId.WeaponStow, 2), new WeaponActionTiming(0, null, 1132, 500, 0, 0, false, true) },
            { (ActionId.WeaponStow, 3), new WeaponActionTiming(0, null, 1500, 514, 0, 0, false, true) },
            { (ActionId.WeaponStow, 4), new WeaponActionTiming(0, null, 1500, 567, 0, 0, false, true) },
            { (ActionId.WeaponStow, 5), new WeaponActionTiming(0, null, 1500, 568, 0, 0, false, true) },
            { (ActionId.WeaponStow, 6), new WeaponActionTiming(0, null, 1999, 610, 0, 0, false, true) },
            { (ActionId.WeaponStow, 7), new WeaponActionTiming(0, null, 1132, 632, 0, 0, false, true) },
            { (ActionId.WeaponStow, 8), new WeaponActionTiming(0, null, 1000, 749, 0, 0, false, true) },
            { (ActionId.WeaponStow, 9), new WeaponActionTiming(0, null, 1666, 757, 1, 0, false, true) },
            { (ActionId.WeaponStow, 14), new WeaponActionTiming(0, null, 2000, 779, 0, 0, false, true) },
            { (ActionId.WeaponStow, 15), new WeaponActionTiming(0, null, 2000, 610, 0, 0, false, true) },
            { (ActionId.WeaponStow, 16), new WeaponActionTiming(0, null, 2166, 765, 0, 0, false, true) },
            { (ActionId.WeaponStow, 17), new WeaponActionTiming(0, null, 1000, 1131, 0, 0, false, true) },
            { (ActionId.WeaponStow, 18), new WeaponActionTiming(0, null, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponStow, 19), new WeaponActionTiming(0, null, 1500, 514, 0, 0, false, true) },
            { (ActionId.WeaponStow, 20), new WeaponActionTiming(0, null, 1500, 567, 0, 0, false, true) },
            { (ActionId.WeaponStow, 21), new WeaponActionTiming(0, null, 1500, 567, 0, 0, false, true) },
            { (ActionId.WeaponStow, 22), new WeaponActionTiming(0, null, 1000, 632, 0, 0, false, true) },
            { (ActionId.WeaponStow, 23), new WeaponActionTiming(0, null, 2000, 632, 0, 0, false, true) },
            { (ActionId.WeaponStow, 24), new WeaponActionTiming(0, null, 1500, 1479, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 1), new WeaponActionTiming(0, null, 1500, 499, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 2), new WeaponActionTiming(0, null, 1000, 501, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 3), new WeaponActionTiming(0, null, 900, 513, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 4), new WeaponActionTiming(0, null, 1500, 566, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 5), new WeaponActionTiming(0, null, 1500, 569, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 6), new WeaponActionTiming(0, null, 800, 609, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 7), new WeaponActionTiming(0, null, 1000, 631, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 8), new WeaponActionTiming(0, null, 666, 748, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 9), new WeaponActionTiming(0, null, 1333, 753, 1, 2000, false, true) },
            { (ActionId.WeaponDraw, 10), new WeaponActionTiming(0, null, 1333, 764, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 14), new WeaponActionTiming(0, null, 1500, 778, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 15), new WeaponActionTiming(0, null, 800, 609, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 16), new WeaponActionTiming(0, null, 800, 1130, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 17), new WeaponActionTiming(0, null, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 18), new WeaponActionTiming(0, null, 1500, 513, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 19), new WeaponActionTiming(0, null, 3000, 566, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 20), new WeaponActionTiming(0, null, 3000, 566, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 21), new WeaponActionTiming(0, null, 1000, 631, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 22), new WeaponActionTiming(0, null, 1000, 632, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 23), new WeaponActionTiming(0, null, 1000, 631, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 24), new WeaponActionTiming(0, null, 2000, 631, 0, 0, false, true) },
            { (ActionId.WeaponDraw, 25), new WeaponActionTiming(0, null, 1000, 1483, 0, 0, false, true) },
            { (ActionId.WeaponReload, 1), new WeaponActionTiming(1500, 557, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 2), new WeaponActionTiming(1333, 558, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 3), new WeaponActionTiming(2166, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 4), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 5), new WeaponActionTiming(2000, 562, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 6), new WeaponActionTiming(2333, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 9), new WeaponActionTiming(2166, 767, 0, null, 1, 0, false, true) },
            { (ActionId.WeaponReload, 10), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 11), new WeaponActionTiming(2166, 583, 0, null, 1, 0, false, true) },
            { (ActionId.WeaponReload, 12), new WeaponActionTiming(2000, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 13), new WeaponActionTiming(2000, 654, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 14), new WeaponActionTiming(1500, 557, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 15), new WeaponActionTiming(1500, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 16), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 17), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 18), new WeaponActionTiming(500, 1026, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 19), new WeaponActionTiming(500, 1027, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 20), new WeaponActionTiming(2000, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 21), new WeaponActionTiming(2000, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 22), new WeaponActionTiming(2000, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 23), new WeaponActionTiming(2000, 654, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 24), new WeaponActionTiming(2000, 654, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 25), new WeaponActionTiming(2000, 654, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 26), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 27), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 28), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 29), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 30), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 31), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 32), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 33), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 34), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 35), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 36), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 37), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 38), new WeaponActionTiming(2333, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 39), new WeaponActionTiming(2333, 654, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 40), new WeaponActionTiming(2333, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 41), new WeaponActionTiming(2333, 654, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 42), new WeaponActionTiming(2333, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 43), new WeaponActionTiming(2333, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 44), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 45), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 46), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 47), new WeaponActionTiming(1500, 557, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 48), new WeaponActionTiming(1500, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 49), new WeaponActionTiming(1500, 557, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 50), new WeaponActionTiming(1500, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 51), new WeaponActionTiming(1500, 557, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 52), new WeaponActionTiming(1500, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 53), new WeaponActionTiming(1500, 557, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 54), new WeaponActionTiming(1500, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 55), new WeaponActionTiming(2000, 562, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 56), new WeaponActionTiming(2000, 562, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 57), new WeaponActionTiming(2000, 562, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 58), new WeaponActionTiming(2000, 562, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 59), new WeaponActionTiming(1333, 558, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 60), new WeaponActionTiming(1333, 634, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 61), new WeaponActionTiming(1333, 558, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 62), new WeaponActionTiming(1333, 634, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 63), new WeaponActionTiming(1333, 558, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 64), new WeaponActionTiming(1333, 634, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 65), new WeaponActionTiming(1333, 558, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 66), new WeaponActionTiming(1333, 634, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 67), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 68), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 69), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 70), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 71), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 72), new WeaponActionTiming(3000, 653, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 73), new WeaponActionTiming(2166, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 74), new WeaponActionTiming(2166, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 75), new WeaponActionTiming(2166, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 76), new WeaponActionTiming(2166, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 77), new WeaponActionTiming(2166, 556, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 78), new WeaponActionTiming(1966, 729, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 79), new WeaponActionTiming(1666, 729, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 80), new WeaponActionTiming(1666, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 81), new WeaponActionTiming(1666, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 82), new WeaponActionTiming(1666, 729, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 83), new WeaponActionTiming(1666, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 84), new WeaponActionTiming(1666, 729, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 85), new WeaponActionTiming(1666, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 86), new WeaponActionTiming(1666, 729, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 87), new WeaponActionTiming(1666, 627, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 88), new WeaponActionTiming(2164, 767, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 89), new WeaponActionTiming(2166, 767, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 90), new WeaponActionTiming(2166, 767, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 91), new WeaponActionTiming(3000, 559, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 92), new WeaponActionTiming(0, null, 3996, 653, 0, 0, false, true) },
            { (ActionId.WeaponReload, 93), new WeaponActionTiming(1500, 1481, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 94), new WeaponActionTiming(3000, 633, 0, null, 0, 0, false, true) },
            { (ActionId.WeaponReload, 95), new WeaponActionTiming(2000, 556, 0, null, 0, 0, false, true) },
        };

        public static int Count => Timings.Count;

        public static bool TryGet(ActionId actionId, uint argumentId, out WeaponActionTiming timing)
            => Timings.TryGetValue((actionId, argumentId), out timing);

        public static WeaponActionTiming Get(ActionId actionId, uint argumentId)
            => Timings.TryGetValue((actionId, argumentId), out var timing)
                ? timing : throw new ArgumentOutOfRangeException(nameof(argumentId),
                    $"No original weapon action timing for {(int)actionId}/{argumentId}.");
    }
}
