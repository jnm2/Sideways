﻿namespace Sideways.Tests.Chart
{
    using NUnit.Framework;

    public static class FloatRangeTests
    {
        [TestCase(1f, 2f, 100.0, 1f, 100.0)]
        [TestCase(1f, 2f, 100.0, 2f, 0.0)]
        public static void Y(float min, float max, double height, float value, double expected)
        {
            var range = new FloatRange(min, max);
            Assert.AreEqual(expected, range.Y(value, height));
        }

        [TestCase(1f, 2f, 100.0, 100.0, 1f)]
        [TestCase(1f, 2f, 100.0, 0.0, 2f)]
        public static void ValueFromY(float min, float max, double height, double value, float expected)
        {
            var range = new FloatRange(min, max);
            Assert.AreEqual(expected, range.ValueFromY(value, height));
        }
    }
}