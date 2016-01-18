using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AutoMapperMapper = AutoMapper.Mapper;

namespace FluentProjections.AutoMapper.Tests
{
    public class FluentEventDenormalizerTests
    {
        private class TestEvent
        {
            public short ValueInt16 { get; set; }
            public int ValueInt32 { get; set; }
            public long ValueInt64 { get; set; }
        }

        private class TestProjection
        {
            public short ValueInt16 { get; set; }
            public int ValueInt32 { get; set; }
            public long ValueInt64 { get; set; }
        }

        private class TestStore : IFluentProjectionStore
        {
            public TestStore(TestProjection readProjection)
            {
                ReadProjection = readProjection;
            }

            public IEnumerable<FluentProjectionFilterValue> ReadFilterValues { get; private set; }
            public TestProjection ReadProjection { get; }
            public TestProjection UpdateProjection { get; private set; }
            public List<TestProjection> InsertProjections { get; private set; }
            public IEnumerable<FluentProjectionFilterValue> RemoveFilterValues { get; private set; }

            public IEnumerable<TProjection> Read<TProjection>(IEnumerable<FluentProjectionFilterValue> values)
                where TProjection : class
            {
                ReadFilterValues = values;
                return new[] {ReadProjection}.OfType<TProjection>();
            }

            public void Update<TProjection>(TProjection projection) where TProjection : class
            {
                UpdateProjection = projection as TestProjection;
            }

            public void Insert<TProjection>(TProjection projection) where TProjection : class
            {
                InsertProjections = InsertProjections ?? new List<TestProjection>();
                InsertProjections.Add(projection as TestProjection);
            }

            public void Remove<TProjection>(IEnumerable<FluentProjectionFilterValue> values) where TProjection : class
            {
                RemoveFilterValues = values;
            }
        }

        [TestFixture]
        public class When_event_add_new_projection_and_auto_map_properties
        {
            private class TestDenormalizer : FluentEventDenormalizer<TestProjection>
            {
                private readonly IFluentProjectionStore _store;

                static TestDenormalizer()
                {
                    AutoMapperMapper.CreateMap<TestEvent, TestProjection>();
                }

                public TestDenormalizer(IFluentProjectionStore store)
                {
                    _store = store;

                    On<TestEvent>(x => x.AddNew().AutoMap());
                }

                public void Handle(TestEvent @event)
                {
                    Handle(@event, _store);
                }
            }

            private TestStore _targetStore;

            [TestFixtureSetUp]
            public void Init()
            {
                _targetStore = new TestStore(null);

                var @event = new TestEvent
                {
                    ValueInt32 = 777
                };

                new TestDenormalizer(_targetStore).Handle(@event);
            }

            [Test]
            public void Should_add_new_projection()
            {
                Assert.AreEqual(1, _targetStore.InsertProjections.Count);
            }

            [Test]
            public void Should_map_values()
            {
                Assert.AreEqual(777, _targetStore.InsertProjections.Single().ValueInt32);
            }
        }
    }
}