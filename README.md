# msql-server-tmpfs

## Motivation

I recently came across [Fast unit tests with databases, part 3 – Implementation of our solution](https://www.fusonic.net/de/blog/fusonic-test-with-databases-part-3) which mentioned how they sped up their integration tests by running the database server in docker with its data directory mounted to an in memory file system.

Unfortunately the docker containers provided by microsoft don't natively [allow the data directory to be mounted on tmpfs](https://github.com/microsoft/mssql-docker/issues/110).

There is a work around [here](https://github.com/t-oster/mssql-docker-zfs/) which involves

> Masking the O_DIRECT flag of the open function, which the mssql-server uses and which causes the mssql container to not run on tmps

This requires building a new image based of the microsoft provided one with this hack included.

By tagging this repository with a version corresponding to one of the offical [Microsoft SQL Server images](https://hub.docker.com/_/microsoft-mssql-server) (for example `2022-latest`), a workflow is kicked off that

1. Builds a new image using that Microsoft SQL Server image but with the hack applied (see [dockerfile-template](dockerfile-template))

2. Pushes the new image to [my docker hub repository](https://hub.docker.com/repository/docker/michaelwolfenden/mssql-server-tmpfs) with that tag ready for use. For example as image `michaelwolfenden/mssql-server-tmpfs:2022-latest`

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
