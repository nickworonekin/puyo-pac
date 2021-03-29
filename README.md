# PuyoPac

PuyoPac is a command-line app for creating and extracting PAC archives used in Puyo Puyo Tetris 2.

## Requirements

* [.NET 5.0+ Runtime](https://dotnet.microsoft.com/download)

## Download

The latest release can be downloaded from the [Releases](https://github.com/nickworonekin/puyo-pac/releases) page.

## Usage
### Create a new PAC
```
puyopac create [options] <output> <inputs>...
```

#### Options

`-d, --dependency <path>`

Add a dependency on a PAC archive. This option can be repeated multiple times for multiple dependencies. The path of the dependent PAC is relative to the raw folder, uses backslashes for folder seperators, and does not include the .pac file extension (e.g. "ui\\ui_resident").

By default, if no dependencies are provided, they will be automatically added based on the output name. This behavior can be disabled with the `--no-auto-dependencies` option.

Aliases may be used to reference common dependencies:

| Alias | Dependency
| - | - |
| ui_cutin_cmn | ui\ui_cutin_cmn
| ui_system | ui\ui_system
| ui_adv_area_common | ui\adventure\ui_adv_area_common
| ui_resident | ui\ui_resident

In addition, some dependencies may have a dependency on additional PAC archives. These additional dependencies will be automatically added when you add the respective dependency:

| Dependency | Additional Dependencies |
| - | - |
| ui\ui_cutin_cmn | ui\ui_resident<br>ui\ui_system
| ui\ui_system | ui\ui_resident
| ui\adventure\ui_adv_area_common | ui\ui_resident

`-c, --compression <format>`

Set the compression format to use when compressing embedded PACs. Can be one of the following values:
* none
* deflate
* lz4

If set to none, embedded PACs will not be compressed. If not set, defaults to lz4. 

When using Deflate compression, embedded PACs are compressed only when they exceed 100KB.

PAC archives created for use with the Nintendo Switch version of the game should use Deflate compression. PAC archives created for use with the PC version of the game should use LZ4 compression.

`--no-splits`

Do not split embedded PACs. By default, embedded PACs are split into multiple files when they exceed 30MB.

`--no-auto-dependencies`

Do not automatically add dependencies based on the output name.

`-?, -h, --help`

Show help and usage information for the create command.

#### Arguments

`output`

The name of the PAC archive to create.

`inputs`

The files, or folders containing files, to add. If a folder is specified, all files in that folder will be added. File and folder names can contain wildcard characters. As an example, all of the following are accepted by this argument:

* **uiTENPEX_cutin_2p_arl_nml_01.dds** (Adds this file to the archive)
* ***.dds** (Adds all files with the dds file extension in the current folder to the archive)
* **arle** (Adds all files in the arle folder to the archive)
* **arle/*.swif** (Adds all files with the swif file extension in the arle folder to the archive)

### Extract a PAC
```
puyopac extract [options] <input>
```

#### Options

`-o, --output <output>`

The name of the folder to extract the PAC archive to. If omitted, the files will be extracted to the current folder.

`-?, -h, --help`

Show help and usage information for the extract command.

#### Arguments

`input`

The name of the PAC archive to extract.

## Batch files

This app comes with two batch files, create.cmd and extract.cmd, to easily create and extract PAC archives.

### create.cmd
To use this batch file, drag the files and/or folders containing the files you want to add to the PAC archive. The batch file will will ask you to name the PAC archive when creating it.

### extract.cmd
To use this batch file, drag the PAC archive you want to extract. The batch file will ask you for the name of the folder to extract the files to. If left blank, the files will be extracted to the current folder.

## License
PuyoPac is licensed under the [MIT license](LICENSE.md).