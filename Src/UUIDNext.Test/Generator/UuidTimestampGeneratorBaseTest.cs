﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NFluent;
using UUIDNext.Generator;
using Xunit;

namespace UUIDNext.Test.Generator
{
    public abstract class UuidTimestampGeneratorBaseTest
    {
        protected abstract byte Version { get; }
        protected abstract UuidTimestampGeneratorBase GetNewGenerator();

        protected abstract (long timestamp, int sequence) DecodeUuid(Guid uuid);

        [Fact]
        public void DumbTest()
        {
            var generator = GetNewGenerator();
            ConcurrentBag<Guid> generatedUuids = new();
            Parallel.For(0, 100, _ => generatedUuids.Add(generator.New()));

            Check.That(generatedUuids).ContainsNoDuplicateItem();

            foreach (var uuid in generatedUuids)
            {
                UuidTestHelper.CheckVersionAndVariant(uuid, Version);
            }
        }

        [Fact]
        public void OrderTest()
        {
            var generator = GetNewGenerator();
            Span<Guid> guids = stackalloc Guid[100];
            for (int i = 0; i < 100; i++)
            {
                guids[i] = generator.New();
            }

            var comparer = new GuidComparer();
            for (int i = 1; i < guids.Length; i++)
            {
                Check.That(comparer.Compare(guids[i - 1], guids[i])).IsStrictlyLessThan(0);
            }
        }

        [Fact]
        public void TestSequenceOverflow()
        {
            var generator = GetNewGenerator();
            var date = DateTime.UtcNow.Date;
            int sequenceMaxValue = generator.GetSequenceMaxValue();

            var firstUuid = generator.New(date);
            var (firstTimestamp, firstSequenceNumber) = DecodeUuid(firstUuid);

            Check.That(firstSequenceNumber).IsStrictlyLessThan((sequenceMaxValue + 1) / 2);

            Parallel.For(0, sequenceMaxValue - firstSequenceNumber, _ => generator.New(date));

            var lastUuid = generator.New(date);
            var (lastTimestamp, lastSequenceNumber) = DecodeUuid(lastUuid);
            Check.That(lastSequenceNumber).IsStrictlyLessThan((sequenceMaxValue + 1) / 2);
            Check.That(lastTimestamp).IsEqualTo(firstTimestamp + 1);
        }

        [Fact]
        public void TestBackwardClock()
        {
            var date = DateTime.UtcNow.Date;
            var pastDate = date.AddSeconds(-5);

            var generator = GetNewGenerator();
            var guid1 = generator.New(date);

            var guid2 = generator.New(pastDate);
            Check.That(guid1.ToString()).IsBefore(guid2.ToString());

            var guid3 = generator.New(date);
            Check.That(guid2.ToString()).IsBefore(guid3.ToString());

            var guid4 = generator.New(pastDate);
            Check.That(guid3.ToString()).IsBefore(guid4.ToString());
        }
    }
}
