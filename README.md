# GammaCopy

An `.SMDB` file, template file, sometimes called a layout, is like a `.ZIP` file but without the actual data.  Save folder and file structure to a template file.  Restore files and folder structure based on a given template and the existing available source material.  GammaCopy has a Highly optimized metadata caching system, interruptable, with deep archive traversal - for example it will find a file buried several layers deep in a `.RAR` within a `.ZIP` from an `.ISO` inside a `.7z`, and so on, and it will store its hash in an `SQLite` database so that if the data is ever needed GammaCopy has recorded the source material's existence and exactly how to retrieve it.

Template files allow one to share/archive file organization and identities by use of [checksums](https://en.wikipedia.org/wiki/Checksum).  Templates work well for preserving metadata such as file sums and file and folder names.  Depending on what data is available, they can be used to restore fully or partially a copy of the original file and folder layout.


Template Support: 
| Input | Output  |   |
|---|---|---|
| X | X | [SourceMaterial DataBase (or SMDB)](https://github.com/frederic-mahe/Hardware-Target-Game-Database)  |
| X |   | [Logiqx Data files](https://github.com/Logiqx/logiqx-dev)  |


License is Apache 2.0 because of [Crc32.cs](https://github.com/damieng/DamienGKit/blob/master/CSharp/DamienG.Library/Security/Cryptography/Crc32.cs)

***
## Disclaimer
**You are responsible for your own actions.** If you mess something up or loose data while using this software, it's your fault, and your fault only.

- *I am in no way affiliated with, nor is this project endorsed by [SmokeMonsterPacks](https://github.com/SmokeMonsterPacks) or SmokeMonster.*

***

## Command line

| verb  | description                                                                                                              |
|-------|--------------------------------------------------------------------------------------------------------------------------|
| parse | Create a template file.                                                                                                  |
| index | Store in a database the sums of all source material.  Metadata caching vastly improves build performance.                |
| build | Restore files and folder structure based on a given template and the existing available source material.                 |

### parse options

`-p, --parse-folder`                **Required**. Folders to be parsed.

`-w, --parsed-output-destination`    **Required**. Write parsed output to this file.

`-r, --global-prepend`

`-t, --prepend-last-folder`

### index options

`-s, --source`        **Required**. Source locations to index or refresh.

`-e, --onebyone`    Gather metadata sequentially.

`-x, --disable-archive-traversal`    Do not traverse archive files.

### build options

`-d, --database` **Required**. template files to use.

`-o, --output` Path to the output folder.

`-n, --containers` Write output to `<outputpath>/<templatefilename>/`

`-f, --coverage-hybrid-to-file` Gather entries not in output location or metadata cache. Output coverage information to file.

`-c, --coverage-hybrid-to-stdout` Gather entries not in output location or metadata cache. Output coverage summary to console.

`-u, --coverage-existant-to-file` Gather entries not in output location. Output coverage information to file.

`-v, --coverage-existant-to-stdout` Gather entries not in output location. Output coverage summary to console.

`-h, --coverage-meta-to-file` Gather entries not in metadata cache. Output coverage information to file.

`-b, --coverage-meta-to-stdout` Gather entries not in metadata cache. Output coverage summary to console.

`-j, --stdout-coverage-full` When outputting coverage to console, DO NOT omit the missing entry list.

`-k, --delete-extras` Delete extra files found in the output path but not in the template file.

`-l, --delete-empty-folders` Delete empty folders found in the output path.

`-g, --go` Go ahead with the build process and output the data to the output folder.

## Examples

#### use a template to build file and folder structures:
`>GammaCopy build -d "Y:\nes template.txt" -c -f -g -o "Y:\nes"`

#### update the metadata cache with all sums and locations:
`>GammaCopy index -s "Y:\pilesofarchives\"`

#### create a new template based on an organized file and folder structure:
`>GammaCopy parse -p "Y:\finalset" -w "Y:\final.txt" -r "final"`

## Getting Started

The first thing you will want to do is have GammaCopy index, or analyze all of your actual files, called the "source material".  The great thing about all this is that the source material need not be organized at all!  The organization comes from the templates.  GammaCopy will by default recursively delve as deep as it can within supported unencrypted filetypes, ISOs, RARs, 7z, even EXEs.  The analysis process duration depends on system resources, network conditions, and how extensive your source material is.  To begin the process, issue the following command:

```
C:\Users\User>E:\projects\GammaCopy\GammaCopy\bin\Debug\GammaCopy.exe index -s Z:\emulation
Using metadata cache: C:\Users\User\AppData\Roaming\GammaCopy\index.db
Index Options:
source(s): Z:\emulation
onebyone: False
disable-archive-traversal: False
Finding orphaned metadata cache entries, metadata cache size: 0.
metadata cache pruning took 00:00:00.0072307.
Refreshing metadata cache for: Z:\emulation
```

