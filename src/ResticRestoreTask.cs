using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace LudusaviRestic
{
    public class RestoreGameTask : BaseResticTask
    {
        private Game game;

        public RestoreGameTask(Game game, SemaphoreSlim semaphore, BackupContext context) : base(semaphore, context)
        {
            this.game = game;
        }

        protected override void RunTask()
        {
            Restore(this.semaphore, this.context, this.game);
        }

        protected static void Restore(SemaphoreSlim semaphore, BackupContext context, Game game)
        {
            try
            {
                semaphore.Wait();
                QuerySnapshots(game, context);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static DateTime? ParseDate(string dateString)
        {
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) == true)
            {
                return date;
            }
            else
            {
                logger.Error($"Unable to parse date: {dateString}");
                return null;
            }
        }

        private static List<GenericItemOption> MockSearch(string a)
        {
            logger.Debug($"Mock search called {a}");
            return new List<GenericItemOption>();
        }

        private static List<GenericItemOption> SnapshotsSearch(BackupContext context, string tag)
        {
            CommandResult process;
            JArray resticSnapshots;

            string args = $"{ConstructTag(tag)} --json";
            process = ResticCommand.Snapshots(context, args);

            resticSnapshots = JArray.Parse(process.StdOut);
            List<GenericItemOption> snapshots = new List<GenericItemOption>();

            foreach (JObject snapshot in resticSnapshots)
            {
                ResticSnapshot s = new ResticSnapshot((string)snapshot["short_id"], (string)snapshot["hostname"], ParseDate((string)snapshot["time"]));
                snapshots.Add(s);
                logger.Debug($"{s.ToString()}");
            }

            return snapshots;
        }


        // Restic isn't able to restore windows backups to same location
        // https://github.com/restic/restic/issues/2092
        private static void RestoreSnapshot(BackupContext context, ResticSnapshot snapshot, string restoreArgs)
        {
            try
            {
                logger.Debug($"{restoreArgs} {snapshot.ID}");
                CommandResult process = ResticCommand.Restore(context, $"{restoreArgs} {snapshot.ID}");
                logger.Debug(process.StdOut);
                logger.Debug(process.StdErr);
            }
            catch (Exception e)
            {
                logger.Debug(e, "Encountered error restoring data");
            }
        }

        private static void QuerySnapshots(Game game, BackupContext context)
        {
            try
            {
                ResticSnapshot selectedSnapshot = (ResticSnapshot)context.api.Dialogs.ChooseItemWithSearch(null, (a) =>
                {
                    return SnapshotsSearch(context, a);
                }, game.Name);

                if (selectedSnapshot == null)
                {
                    logger.Debug("Restore cancelled");
                    return;
                }
                
                logger.Debug($"Selected snapshot {selectedSnapshot.ToString()}");

                string args = context.api.Dialogs.SelectString("Restore arguments", "Restore arguments", "--target /").SelectedString;

                RestoreSnapshot(context, selectedSnapshot, args);
            }
            catch (Exception e)
            {
                logger.Debug(e, "Encountered error querying snapshots");
                return;
            }
        }
    }
}