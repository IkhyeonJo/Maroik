using System.Text.RegularExpressions;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// <c>Backup.sh</c> runs once a day at 04:00 server time (owner's decision, 2026-11-02; it ran hourly before). Every run copies the
/// whole upload tree, File Manager included, and xz-compresses it: with a large drive one run can outlast an hour, and the next
/// hourly run would remove the staging folder under it (the script has no lock). Once a day leaves it the whole night, at the
/// quietest hour; the price is that a lost server loses up to a day of data.
/// </summary>
public class BackupScheduleArchitectureTests
{
    [Fact]
    public void Backup_RunsOnceADay_At4AM()
    {
        string[] backupLines =
        [
            .. File.ReadAllLines(Path.Combine(RepositoryPaths.Root, "Maroik.Crontab", "InitCron"))
                .Where(line => line.Contains("Maroik.Crontab/Backup.sh", StringComparison.Ordinal)),
        ];

        string line = Assert.Single(backupLines);
        Assert.Matches(new Regex(@"^0 4 \* \* \* /bin/bash /home/ubuntu/Maroik/Maroik\.Crontab/Backup\.sh$"), line);
    }
}
