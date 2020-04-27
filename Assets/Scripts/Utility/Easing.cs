using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace TJ.Utility
{
    public static class Easing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Ease(float t)
        {
            return 0.5f - math.cos(math.PI * t) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseIn(float t)
        {
            return math.cos((1f - t) * math.PI / 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOut(float t)
        {
            return 1f - math.cos(t * math.PI / 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutCubic(float t)
        {
            return EaseOutPow(t, 3f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInCubic(float t)
        {
            return EaseInPow(t, 3f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuad(float t)
        {
            return EaseOutPow(t, 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuad(float t)
        {
            return EaseInPow(t, 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuint(float t)
        {
            return EaseOutPow(t, 5f);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuart(float t)
        {
            return EaseOutPow(t, 4f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuart(float t)
        {
            return EaseInPow(t, 4f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuint(float t)
        {
            return EaseInPow(t, 5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float EaseOutPow(float t, float pow)
        {
            return math.pow(t, pow);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float EaseInPow(float t, float pow)
        {
            return 1f - math.pow(1f - t, pow);
        }

        public static float EaseInBounce(float t)
        {
            var p = 2.75f;
            var s = math.pow(p, 2f);
            if (t < (1f / p))
                return s * t * t;
            if (t < (2f / p))
                return 1f + s * ((math.pow(t - 1.5f / p, 2f)) - math.pow(-.5f / p, 2f));
            if (t < (2.5f / p))
                return 1f + s * ((math.pow(t - 2.25f / p, 2f)) - math.pow(-.25f / p, 2f));
            return 1f + s * ((math.pow(t - 2.625f / p, 2f)) - math.pow(-.125f / p, 2f));
        }
        
        public static float EaseOutBounce(float t)
        {
            return 1f - EaseInBounce(1f - t);
        }
        
        public static float EaseInElastic(float t)
        {
            const float p = 0.3f;
            return 1f + math.pow(2f, -10f * t) * math.sin((t - p / 4f) * (2f * math.PI) / p);
        }

        public static float EaseOutElastic(float t)
        {
            return 1f - EaseInElastic(1 - t);
        }
        
        public static float EaseCubic(float t)
        {
            if (t < .5f)
            {
                return EaseOutCubic(t * 2f) / 2f;
            }
            return 1 - EaseOutCubic((1 - t) * 2f) / 2f;
        }
    }
}