using System.Linq;
using NodaTime;
using NUnit.Framework;
using Raven.Client.Documents;

/*
 * Test code from Raven.Client.NodaTime.Tests.NodaZonedDateTimeTests, reduced to single relevant test
 */

namespace ReplicateZonedDateTimeQueryProblem
{
	public class Tests : MyRavenTestDriver
    {
        [Test]
        public void Can_Use_NodaTime_ZonedDateTime_In_Document()
        {
            var instant = SystemClock.Instance.GetCurrentInstant();
            var zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            Can_Use_NodaTime_ZonedDateTime_In_Dynamic_Index(new ZonedDateTime(instant, zone));
        }

        private void Can_Use_NodaTime_ZonedDateTime_In_Dynamic_Index(ZonedDateTime zdt)
        {
            using (var documentStore = NewDocumentStore())
            {
                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Foo { Id = "foos/1", ZonedDateTime = zdt });
                    session.Store(new Foo { Id = "foos/2", ZonedDateTime = zdt - Duration.FromMinutes(1) });
                    session.Store(new Foo { Id = "foos/3", ZonedDateTime = zdt - Duration.FromMinutes(2) });
                    session.SaveChanges();
                }

                //WaitForUserToContinueTheTest(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    // .ToInstant() is required for dynamic query.  See comments in the static index for an alternative.

                    var q1 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.ZonedDateTime.ToInstant() == zdt.ToInstant());
                    System.Diagnostics.Debug.WriteLine(q1);
                    var results1 = q1.ToList();
                    //WaitForUserToContinueTheTest(documentStore);
                    Assert.AreEqual(1, results1.Count);

                    var q2 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.ZonedDateTime.ToInstant() < zdt.ToInstant())
                                    .OrderBy(x => x.ZonedDateTime.ToInstant());
                    var results2 = q2.ToList();
                    Assert.AreEqual(2, results2.Count);
                    Assert.True(ZonedDateTime.Comparer.Local.Compare(results2[0].ZonedDateTime, results2[1].ZonedDateTime) < 0);

                    var q3 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.ZonedDateTime.ToInstant() <= zdt.ToInstant())
                                    .OrderBy(x => x.ZonedDateTime.ToInstant());
                    var results3 = q3.ToList();
                    Assert.AreEqual(3, results3.Count);
                    Assert.True(ZonedDateTime.Comparer.Local.Compare(results3[0].ZonedDateTime, results3[1].ZonedDateTime) < 0);
                    Assert.True(ZonedDateTime.Comparer.Local.Compare(results3[1].ZonedDateTime, results3[2].ZonedDateTime) < 0);
                }
            }
        }

        public class Foo
        {
            public string Id { get; set; } = default!;
            public ZonedDateTime ZonedDateTime { get; set; }
        }
    }
}
