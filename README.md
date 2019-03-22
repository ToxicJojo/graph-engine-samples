# graph-engine-samples
A collection of sample applications for Microsoft GraphEngine

## CLI

This project has a small cli tool that can create GE projects with a Client, Server and Model.
It will create a folder for your project. That folder will contain folders for the Client, Server and Model.

The model is shared between the Client and Sever and will be automaticly copied to a tsl/ folder on build.

### Usage

From the repo root run:

```bash
./cli/ge-cli.sh
```

It will ask for a project name. The created folder will have that name.
It will also ask for a GraphEngine version. You can find your version by looking in the `/build` folder of the GraphEngine project. There will be a `GraphEngine.Core.X.X.XXXX.nupkg` file. The X.X.XXXX is the version you have. (This number is based on the date you build GE. If you rebuild the project your version number will change.)

## Samples

The samples can be executed by:

```bash
dotnet run
```

## Differences to 1.X

This project uses GE 2.X. The Samples in the GE repo and on their website are using version 1.X. This means there are certain changes. This is a small list of differences I found:

### CellID vs CellId

Version 1.X uses `cell.CellID`
Version 2.X uses `cell.CellId`

### Server vs Partition

Version 1.X uses Server to identifiy different running servers
Version 2.X uses Partition to identify different running servers.
