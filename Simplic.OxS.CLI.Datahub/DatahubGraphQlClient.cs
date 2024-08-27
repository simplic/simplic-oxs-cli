using Spectre.Console;
using System.Net.Http.Json;

namespace Simplic.OxS.CLI.Datahub
{
    internal class DatahubGraphQlClient(HttpClient httpClient)
    {
        public async Task<IEnumerable<Guid>> GetUncomitted(Guid definition)
        {
            var query = @$"
query {{
  queue(
    where: {{
      definition: {{ id: {{ eq: ""{definition}"" }} }}
      state: {{ eq: ""enqueued"" }}
    }}
  ) {{
    items {{
      id
    }}
  }}
}}";
            var request = JsonContent.Create(new Request { Query = query });
            var response = await httpClient.PostAsync("datahub-api/v1/graphql", request);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<Response<Uncomitted>>();
            return data?.Data.Queue.Items.Select(x => x.Id) ?? [];
        }

        internal class Request
        {
            public required string Query { get; set; }
        }

        internal class Response<T>
        {
            public required T Data { get; set; }
        }

        internal class Uncomitted
        {
            public required Queue Queue { get; set; }
        }

        internal class Queue
        {
            public required IEnumerable<QueueItem> Items { get; set; }
        }

        internal class QueueItem
        {
            public required Guid Id { get; set; }
        }
    }
}