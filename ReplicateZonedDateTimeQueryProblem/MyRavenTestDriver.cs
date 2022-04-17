using Raven.Client.Documents;
using Raven.Client.NodaTime;
using Raven.TestDriver;

namespace ReplicateZonedDateTimeQueryProblem
{
    public class MyRavenTestDriver : RavenTestDriver
    {
        protected override void PreInitialize(IDocumentStore documentStore)
        {
            documentStore.ConfigureForNodaTime();

            base.PreInitialize(documentStore);
        }

        protected IDocumentStore NewDocumentStore() => GetDocumentStore();
    }
}
