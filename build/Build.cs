using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Serilog;
using static Nuke.Common.Tools.Docker.DockerTasks;

class Build : NukeBuild
{
    [GitRepository] readonly GitRepository Repository;

    [Parameter] readonly string DockerImageName = "mssql-server-tmpfs";
    [Parameter] readonly string DockerOrganization = "michaelwolfenden";
    [Parameter] readonly string DockerRegistryPassword;
    [Parameter] readonly string DockerRegistryUsername;
    [Parameter] readonly string MsSqlDockerTag;

    AbsolutePath DockerFileTemplate => RootDirectory / "dockerfile-template";
    AbsolutePath DockerFile => RootDirectory / "dockerfile";

    Target CreateDockerFileFromTemplate => _ => _
        .Executes(() =>
        {
            var tag = GetMsSqlDockerTag();

            if (string.IsNullOrEmpty(tag))
                throw new Exception("Expected either a -ms-sql-docker-tag parameter to be passed or the git commit to have a tag");

            Log.Information("Creating dockerfile based off mcr.microsoft.com/mssql/server:{Tag}", tag);

            var dockerFileContents = File.ReadAllText(DockerFileTemplate)
                .Replace("{TAG}", tag);

            File.WriteAllText(DockerFile, dockerFileContents);
        });

    Target BuildDocker => _ => _
        .DependsOn(CreateDockerFileFromTemplate)
        .Executes(() =>
        {
            var imageName = $"{DockerImageName}:{GetMsSqlDockerTag()}";

            Log.Information("Building image {Image}", imageName);

            DockerBuild(c => c
                .SetFile(DockerFile)
                .SetTag(imageName)
                .SetPath(".")
                .SetPull(true)
                .SetProcessWorkingDirectory(RootDirectory));
        });

    Target PushDocker => _ => _
        .DependsOn(BuildDocker)
        .Requires(() => DockerRegistryUsername)
        .Requires(() => DockerRegistryPassword)
        .Executes(() =>
        {
            DockerLogin(x => x
                .SetUsername(DockerRegistryUsername)
                .SetPassword(DockerRegistryPassword)
                .DisableProcessLogOutput());

            DockerTag(c => c
                .SetSourceImage(GetSourceImageName())
                .SetTargetImage(GetTargetImageName()));

            DockerPush(c => c
                .SetName(GetTargetImageName()));
        });

    string GetMsSqlDockerTag() => MsSqlDockerTag ?? Repository.Tags.FirstOrDefault();

    string GetSourceImageName() =>
        $"{DockerImageName}:{GetMsSqlDockerTag()}";

    string GetTargetImageName() =>
        $"{DockerOrganization}/{DockerImageName}:{GetMsSqlDockerTag()}";

    public static int Main() =>
        Execute<Build>(x => x.BuildDocker);
}
