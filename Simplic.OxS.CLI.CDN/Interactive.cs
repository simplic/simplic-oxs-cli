using Simplic.OxS.CLI.Core;
using Spectre.Console;

namespace Simplic.OxS.CLI.CDN
{
    public static class Interactive
    {
        public static async Task<IList<BlobMetadata>> RetrieveMetadata(CdnClient client, IList<Guid> blobIds)
        {
            return await AnsiConsole.Progress()
                .StartAsync(async context =>
                {
                    var progress = context.AddTask("Retrieving metadata", new ProgressTaskSettings { MaxValue = blobIds.Count });
                    var metadatas = new List<BlobMetadata>();

                    var i = 0;
                    foreach (var blobId in blobIds)
                    {
                        metadatas.Add(await client.GetMetadataAsync(blobId));
                        progress.Value = ++i;
                    }

                    progress.StopTask();
                    return metadatas;
                });
        }

        public static void ShowMetadata(IEnumerable<BlobMetadata> metadatas)
        {
            var table = new Table()
                    .NoBorder()
                    .AddColumns("Name", "Blob ID", "Size");
            foreach (var metadata in metadatas)
                table.AddRow(
                    Markup.FromInterpolated($"[yellow]{metadata.FileName}[/]"),
                    Markup.FromInterpolated($"[gray]{metadata.Id}[/]"),
                    Markup.FromInterpolated($"[blue]{FormatHelper.FormatByteSize(metadata.Size)}[/]")
                );
            AnsiConsole.Write(table);
        }
    }
}
