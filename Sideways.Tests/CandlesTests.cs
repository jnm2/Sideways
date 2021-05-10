﻿namespace Sideways.Tests
{
    using System;
    using System.Collections.Immutable;

    using NUnit.Framework;

    public static class CandlesTests
    {
        [Test]
        public static void Descending()
        {
            var c1 = new Candle(
                new DateTimeOffset(2021, 04, 3, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c2 = new Candle(
                new DateTimeOffset(2021, 04, 2, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c3 = new Candle(
                new DateTimeOffset(2021, 04, 1, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);
            var candles = new Candles(ImmutableArray.Create(c1, c2, c3));
            CollectionAssert.AreEqual(new[] { c1, c2, c3 }, candles);
        }

        [Test]
        public static void Previous()
        {
            var c1 = new Candle(
                new DateTimeOffset(2021, 04, 3, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c2 = new Candle(
                new DateTimeOffset(2021, 04, 2, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c3 = new Candle(
                new DateTimeOffset(2021, 04, 1, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);
            var candles = new Candles(ImmutableArray.Create(c1, c2, c3));
            Assert.AreEqual(c3, candles.Previous(c2.Time, CandleInterval.None));
            Assert.AreEqual(c2, candles.Previous(c1.Time, CandleInterval.None));
            Assert.AreEqual(null, candles.Previous(c3.Time, CandleInterval.None));
        }

        [Test]
        public static void Next()
        {
            var c1 = new Candle(
                new DateTimeOffset(2021, 04, 3, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c2 = new Candle(
                new DateTimeOffset(2021, 04, 2, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c3 = new Candle(
                new DateTimeOffset(2021, 04, 1, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);
            var candles = new Candles(ImmutableArray.Create(c1, c2, c3));
            Assert.AreEqual(c2, candles.Next(c3.Time, CandleInterval.None));
            Assert.AreEqual(c1, candles.Next(c2.Time, CandleInterval.None));
            Assert.AreEqual(null, candles.Next(c1.Time, CandleInterval.None));
        }

        [TestCase(3, 2, 1)]
        [TestCase(30, 2, 1)]
        [TestCase(30, 29, 1)]
        public static void IndexOfFirst(int d1, int d2, int d3)
        {
            var c1 = new Candle(
                new DateTimeOffset(2021, 04, d1, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c2 = new Candle(
                new DateTimeOffset(2021, 04, d2, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);

            var c3 = new Candle(
                new DateTimeOffset(2021, 04, d3, 00, 00, 00, 0, TimeSpan.Zero),
                default,
                default,
                default,
                default,
                default);
            var candles = new Candles(ImmutableArray.Create(c1, c2, c3));
            Assert.AreEqual(0, candles.IndexOf(c1.Time));
            Assert.AreEqual(1, candles.IndexOf(c2.Time));
            Assert.AreEqual(2, candles.IndexOf(c3.Time));

        }
    }
}