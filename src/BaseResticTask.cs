using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LudusaviRestic
{
    public abstract class BaseResticTask
    {
        protected static readonly ILogger logger = LogManager.GetLogger();
        protected SemaphoreSlim semaphore;
        protected BackupContext context;

        public BaseResticTask(SemaphoreSlim semaphore, BackupContext context)
        {
            this.semaphore = semaphore;
            this.context = context;
        }

        public void Run()
        {
            Task.Run(() => this.RunTask());
        }

        protected abstract void RunTask();

        protected static string ConstructTags(string game, IList<string> extraTags)
        {
            string tags = ConstructTag(game);

            foreach (string tag in extraTags)
            {
                tags += ConstructTag(tag);
            }

            return tags;
        }

        protected static string ConstructTag(string tag)
        {
            return $" --tag \"{tag}\"";
        }

        protected static string ConstructTags(Game game, IList<string> extraTags)
        {
            return ConstructTags(game.Name, extraTags);
        }
        protected static string ConstructTags(Game game)
        {
            return ConstructTags(game.Name, new List<string>());
        }

    }
}